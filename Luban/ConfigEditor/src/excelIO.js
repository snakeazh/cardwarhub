// xlsx 读写：遵循 Luban 的 Excel 约定。
// 行模式：首列以 ## 开头的行是元信息行（##var=字段名行，##=中文标题行），第一个非 ## 行起为数据。
// 列模式（mode=one 常用）：A1=##column，第 1 行其余单元格标记各列用途（##=注释列，空=值列），
//   从第 2 行起每行一个字段：A 列=字段名。对外统一转置成「一行数据」的形式。
// 回写时只覆写数据区，保留所有 ## 头行、样式及其他 sheet；写入前自动备份。
const fs = require('fs');
const path = require('path');
const ExcelJS = require('exceljs');

function cellToString(value) {
  if (value === null || value === undefined) return '';
  if (typeof value === 'boolean') return value ? 'true' : 'false';
  if (typeof value === 'number') return String(value);
  if (value instanceof Date) {
    const p = n => String(n).padStart(2, '0');
    return `${value.getFullYear()}-${p(value.getMonth() + 1)}-${p(value.getDate())} ` +
      `${p(value.getHours())}:${p(value.getMinutes())}:${p(value.getSeconds())}`;
  }
  if (typeof value === 'object') {
    if (value.richText) return value.richText.map(t => t.text).join('');
    if (value.text !== undefined) return cellToString(value.text);      // hyperlink
    if (value.result !== undefined) return cellToString(value.result);  // 公式
    if (value.error) return '';
    return String(value);
  }
  return String(value);
}

// 识别表头。返回：
//  行模式 { columnMode:false, fields:[{name,title,col}], dataStartRow }
//  列模式 { columnMode:true,  fields:[{name,title,row}], valueCol }
function parseHeader(ws) {
  const a1 = cellToString(ws.getRow(1).getCell(1).value).trim();

  // 列模式标记：##column 或 ##var#column
  if (a1 === '##column' || a1 === '##var#column') {
    const markerRow = ws.getRow(1);
    let titleCol = 0;
    let valueCol = 0;
    markerRow.eachCell({ includeEmpty: false }, (cell, colNumber) => {
      if (colNumber === 1) return;
      if (cellToString(cell.value).trim().startsWith('##')) {
        if (!titleCol) titleCol = colNumber;
      } else if (!valueCol) {
        valueCol = colNumber;
      }
    });
    if (!titleCol) titleCol = 2;
    if (!valueCol) valueCol = titleCol === 2 ? 3 : 2;

    const fields = [];
    for (let r = 2; r <= ws.rowCount; r++) {
      const name = cellToString(ws.getRow(r).getCell(1).value).trim();
      if (!name || name.startsWith('##')) continue;
      fields.push({ name, title: cellToString(ws.getRow(r).getCell(titleCol).value).trim(), row: r });
    }
    return { columnMode: true, fields, titleCol, valueCol };
  }

  let fieldsRow = null;
  let titlesRow = null;
  let dataStartRow = null;
  let lastMetaRow = 0;
  const maxScan = Math.min(ws.rowCount, 20);
  for (let r = 1; r <= maxScan; r++) {
    const marker = cellToString(ws.getRow(r).getCell(1).value).trim();
    if (marker === '##var') { fieldsRow = r; lastMetaRow = r; }
    else if (marker === '##') { titlesRow = r; lastMetaRow = r; }
    else if (marker.startsWith('##')) { lastMetaRow = r; } // ##type 等，跳过
    else { dataStartRow = r; break; }
  }
  if (!fieldsRow) {
    throw new Error('未找到 ##var 字段名行，不是标准 Luban 表');
  }
  if (!dataStartRow) dataStartRow = lastMetaRow + 1; // 空表（只有表头）：数据从表头之后开始

  const fRow = ws.getRow(fieldsRow);
  const tRow = titlesRow ? ws.getRow(titlesRow) : null;
  const fields = [];
  // eachCell(includeEmpty:false) 只遍历有值的单元格，避免整行样式把列数撑到 16384
  fRow.eachCell({ includeEmpty: false }, (cell, colNumber) => {
    if (colNumber === 1) return; // 首列是 ##var 标记
    const name = cellToString(cell.value).trim();
    if (!name) return;
    fields.push({
      name,
      title: tRow ? cellToString(tRow.getCell(colNumber).value).trim() : '',
      col: colNumber,
    });
  });
  fields.sort((a, b) => a.col - b.col);
  return { columnMode: false, fields, dataStartRow };
}

/**
 * 读取一张表。
 * @returns {Promise<{columnMode, fields, rows}>}
 *   rows 为字符串二维数组，对齐 fields 顺序；整行为空的行被跳过。列模式恒为单行。
 */
async function readTable(filePath) {
  const wb = new ExcelJS.Workbook();
  await wb.xlsx.readFile(filePath);
  const ws = wb.worksheets[0];
  if (!ws) throw new Error('xlsx 中没有工作表');
  const header = parseHeader(ws);

  if (header.columnMode) {
    const values = header.fields.map(f => cellToString(ws.getRow(f.row).getCell(header.valueCol).value));
    return { columnMode: true, fields: header.fields, rows: [values] };
  }

  const rows = [];
  for (let r = header.dataStartRow; r <= ws.rowCount; r++) {
    const row = ws.getRow(r);
    const values = header.fields.map(f => cellToString(row.getCell(f.col).value));
    if (values.every(v => v === '')) continue;
    rows.push(values);
  }
  return { columnMode: false, fields: header.fields, rows };
}

function toCellValue(str, kind) {
  if (str === '' || str === null || str === undefined) return null;
  if (kind === 'number') {
    const n = Number(str);
    if (!Number.isNaN(n)) return n;
  }
  if (kind === 'bool') {
    const s = String(str).toLowerCase();
    if (s === 'true' || s === 'false') return s === 'true';
  }
  return str;
}

/**
 * 回写一张表。
 * @param {string}   filePath   xlsx 绝对路径
 * @param {Array}    fieldNames 参与写入的字段名（顺序与 rows 的列一致；文件中其它字段不动）
 * @param {Array}    rows       字符串二维数组；列模式忽略，仅行模式使用
 * @param {Array}    kinds      每列 kind（'number'/'bool'/...），数字写回为数值单元格
 * @param {string}   backupDir  备份目录
 */
async function writeTable(filePath, fieldNames, rows, kinds, backupDir) {
  // 备份原文件
  fs.mkdirSync(backupDir, { recursive: true });
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  const backupPath = path.join(backupDir, `${path.basename(filePath, '.xlsx')}.${ts}.xlsx`);
  fs.copyFileSync(filePath, backupPath);

  const wb = new ExcelJS.Workbook();
  await wb.xlsx.readFile(filePath);
  const ws = wb.worksheets[0];
  const header = parseHeader(ws);

  if (header.columnMode) {
    // rows[0] 即各字段的新值
    const values = rows[0] || [];
    fieldNames.forEach((name, j) => {
      const f = header.fields.find(x => x.name === name);
      if (!f) throw new Error(`列模式表中找不到字段: ${name}`);
      ws.getRow(f.row).getCell(header.valueCol).value = toCellValue(values[j], kinds[j]);
    });
  } else {
    const colOf = {};
    for (const f of header.fields) colOf[f.name] = f.col;
    for (const name of fieldNames) {
      if (colOf[name] === undefined) throw new Error(`表中找不到字段: ${name}`);
    }
    const managedCols = header.fields.map(f => f.col);
    const oldMaxRow = ws.rowCount;

    for (let i = 0; i < rows.length; i++) {
      const row = ws.getRow(header.dataStartRow + i);
      for (let j = 0; j < fieldNames.length; j++) {
        row.getCell(colOf[fieldNames[j]]).value = toCellValue(rows[i][j], kinds[j]);
      }
    }
    // 清掉多余的旧数据行（只清本表管理的列）
    for (let r = header.dataStartRow + rows.length; r <= oldMaxRow; r++) {
      const row = ws.getRow(r);
      for (const c of managedCols) row.getCell(c).value = null;
    }
  }

  await wb.xlsx.writeFile(filePath);
  return { backupPath };
}

/**
 * 列模式（单例表）追加一个字段行：A 列=字段名，注释列=中文描述，值列=默认值。
 * 插在最后一个字段行之后；若该行非空则先插入空行。样式复制自上一字段行。
 * @returns {Promise<{backupPath:string}>}
 */
async function appendColumnField(filePath, fieldName, title, value, kind, backupDir) {
  fs.mkdirSync(backupDir, { recursive: true });
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  const backupPath = path.join(backupDir, `${path.basename(filePath, '.xlsx')}.${ts}.xlsx`);
  fs.copyFileSync(filePath, backupPath);

  const wb = new ExcelJS.Workbook();
  await wb.xlsx.readFile(filePath);
  const ws = wb.worksheets[0];
  const header = parseHeader(ws);
  if (!header.columnMode) throw new Error('仅单例表（列模式）支持添加字段');
  if (header.fields.some(f => f.name === fieldName)) {
    throw new Error(`字段 "${fieldName}" 已存在于 xlsx`);
  }

  const lastFieldRow = Math.max(...header.fields.map(f => f.row));
  let newRowIdx = lastFieldRow + 1;
  // 目标行已有内容则先插入空行
  const probe = ws.getRow(newRowIdx);
  const hasContent = [1, header.titleCol, header.valueCol]
    .some(c => cellToString(probe.getCell(c).value).trim() !== '');
  if (hasContent) {
    ws.spliceRows(newRowIdx, 0, []);
  }

  const prev = ws.getRow(lastFieldRow);
  const newRow = ws.getRow(newRowIdx);
  for (const c of [1, header.titleCol, header.valueCol]) {
    newRow.getCell(c).style = { ...prev.getCell(c).style };
  }
  newRow.getCell(1).value = fieldName;
  newRow.getCell(header.titleCol).value = title || null;
  newRow.getCell(header.valueCol).value = toCellValue(value, kind);

  await wb.xlsx.writeFile(filePath);
  return { backupPath };
}

/**
 * 新建一张表的数据 xlsx（只写表头，不写数据行）。
 * 行模式（map/list）：r1 A=##var 后跟字段名，r2 A=## 后跟中文标题。
 * 列模式（one）：r1 A=##column、B=##；r2 起每行一个字段（A=字段名，B=中文标题）。
 * @param {string} filePath 目标 xlsx 绝对路径（已存在则报错）
 * @param {string} mode     'map' | 'list' | 'one'
 * @param {Array<{name:string, title?:string}>} fields
 */
async function createTableXlsx(filePath, mode, fields) {
  if (fs.existsSync(filePath)) {
    throw new Error(`数据文件已存在: ${path.basename(filePath)}`);
  }
  fs.mkdirSync(path.dirname(filePath), { recursive: true });

  const wb = new ExcelJS.Workbook();
  const ws = wb.addWorksheet('Sheet1');

  if (mode === 'one') {
    ws.getRow(1).getCell(1).value = '##column';
    ws.getRow(1).getCell(2).value = '##';
    fields.forEach((f, i) => {
      const row = ws.getRow(2 + i);
      row.getCell(1).value = f.name;
      if (f.title) row.getCell(2).value = f.title;
    });
  } else {
    const varRow = ws.getRow(1);
    const titleRow = ws.getRow(2);
    varRow.getCell(1).value = '##var';
    titleRow.getCell(1).value = '##';
    fields.forEach((f, i) => {
      varRow.getCell(2 + i).value = f.name;
      if (f.title) titleRow.getCell(2 + i).value = f.title;
    });
  }

  await wb.xlsx.writeFile(filePath);
  return { filePath };
}

module.exports = { readTable, writeTable, appendColumnField, createTableXlsx, cellToString };
