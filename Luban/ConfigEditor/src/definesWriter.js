// Defines XML 写入：文本级插入，避免整棵重新序列化导致注释/格式丢失。
const fs = require('fs');
const path = require('path');

function escapeXml(s) {
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function escapeRegExp(s) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// 备份原文件并写回新内容
function backupAndWrite(filePath, newText, backupDir) {
  fs.mkdirSync(backupDir, { recursive: true });
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  const backupPath = path.join(backupDir, `${path.basename(filePath, '.xml')}.${ts}.xml`);
  fs.copyFileSync(filePath, backupPath);
  fs.writeFileSync(filePath, newText, 'utf8');
  return { backupPath };
}

// 把片段插到 closeTag（如 </bean>、</module>）所在行的行首之前，保住结束标签自己的换行和缩进。
// 注意 CRLF 文件里 '\n' 前面还有一个 '\r'，要一并让出来，否则会出现 \r\r\n
function insertBeforeClosingTag(text, closeTag, insert) {
  const endIdx = text.indexOf(closeTag);
  if (endIdx < 0) throw new Error(`缺少 ${closeTag} 结束标签`);
  let lineStart = text.lastIndexOf('\n', endIdx);
  if (lineStart > 0 && text[lineStart - 1] === '\r') lineStart--;
  return text.slice(0, lineStart) + insert + text.slice(lineStart);
}

// 换行符跟随文件原有风格
function eolOf(text) {
  return text.includes('\r\n') ? '\r\n' : '\n';
}

// module 顶层元素（enum/bean/table）的缩进，对齐文件内第一个同类元素
function moduleIndent(text) {
  const im = text.match(/\n([ \t]*)<(?:enum|bean|table)\s/);
  return im ? im[1] : '\t';
}

/**
 * 向指定 bean 追加一个 <var> 字段（插入到 </bean> 之前，缩进对齐同 bean 内现有 var）。
 * @param {string} filePath  Defines XML 的绝对路径
 * @param {string} beanName
 * @param {{name:string, type:string, comment?:string}} field
 * @param {string} backupDir 修改前备份目录
 * @returns {{backupPath:string}}
 */
function addBeanField(filePath, beanName, field, backupDir) {
  const text = fs.readFileSync(filePath, 'utf8');

  const beanRe = new RegExp(`<bean\\s+name="${escapeRegExp(beanName)}"[^>]*>`);
  const m = beanRe.exec(text);
  if (!m) throw new Error(`在 ${path.basename(filePath)} 中找不到 bean "${beanName}"`);
  if (m[0].endsWith('/>')) throw new Error(`bean "${beanName}" 是空 bean（自闭合），暂不支持自动加字段`);

  const endIdx = text.indexOf('</bean>', m.index + m[0].length);
  if (endIdx < 0) throw new Error(`bean "${beanName}" 缺少 </bean> 结束标签`);

  const body = text.slice(m.index, endIdx);
  if (new RegExp(`<var\\s+name="${escapeRegExp(field.name)}"`).test(body)) {
    throw new Error(`字段 "${field.name}" 已存在于 bean "${beanName}"`);
  }

  // 缩进对齐同 bean 内第一个 <var>
  const im = body.match(/\n([ \t]*)<var/);
  const indent = im ? im[1] : '\t\t';
  const eol = eolOf(text);

  let insert = '';
  if (field.comment) insert += `${eol}${indent}<!-- ${escapeXml(field.comment)} -->`;
  insert += `${eol}${indent}<var name="${escapeXml(field.name)}" type="${escapeXml(field.type)}" />`;

  const newText = text.slice(0, m.index) + insertBeforeClosingTag(
    text.slice(m.index), '</bean>', insert);

  return backupAndWrite(filePath, newText, backupDir);
}

/**
 * 新建枚举：插入到 </module> 之前。
 * @param {string} filePath
 * @param {{name:string, comment?:string, items:Array<{name:string, alias?:string}>}} enumDef
 * @param {string} backupDir
 * @returns {{backupPath:string}}
 */
function addEnum(filePath, enumDef, backupDir) {
  const text = fs.readFileSync(filePath, 'utf8');
  if (new RegExp(`<enum\\s+name="${escapeRegExp(enumDef.name)}"`).test(text)) {
    throw new Error(`枚举 "${enumDef.name}" 已存在于 ${path.basename(filePath)}`);
  }

  const eol = eolOf(text);
  const indent = moduleIndent(text);
  const varIndent = indent + '\t';

  let insert = '';
  if (enumDef.comment) insert += `${eol}${indent}<!-- ${escapeXml(enumDef.comment)} -->`;
  insert += `${eol}${indent}<enum name="${escapeXml(enumDef.name)}">`;
  for (const it of enumDef.items) {
    const alias = it.alias ? ` alias="${escapeXml(it.alias)}"` : '';
    insert += `${eol}${varIndent}<var name="${escapeXml(it.name)}"${alias} />`;
  }
  insert += `${eol}${indent}</enum>`;

  const newText = insertBeforeClosingTag(text, '</module>', insert);
  return backupAndWrite(filePath, newText, backupDir);
}

/**
 * 新建表：插入 bean 定义 + table 声明到 </module> 之前。
 * @param {string} filePath
 * @param {{name:string, comment?:string, fields:Array<{name:string, type:string, comment?:string}>}} bean
 * @param {{name:string, value:string, input:string, mode?:string}} table  mode 为 map 时按项目惯例省略该属性
 * @param {string} backupDir
 * @returns {{backupPath:string}}
 */
function addBeanAndTable(filePath, bean, table, backupDir) {
  const text = fs.readFileSync(filePath, 'utf8');
  if (new RegExp(`<bean\\s+name="${escapeRegExp(bean.name)}"`).test(text)) {
    throw new Error(`bean "${bean.name}" 已存在于 ${path.basename(filePath)}`);
  }
  if (new RegExp(`<table\\s+name="${escapeRegExp(table.name)}"`).test(text)) {
    throw new Error(`表 "${table.name}" 已存在于 ${path.basename(filePath)}`);
  }

  const eol = eolOf(text);
  const indent = moduleIndent(text);
  const varIndent = indent + '\t';

  let insert = '';
  if (bean.comment) insert += `${eol}${indent}<!-- ${escapeXml(bean.comment)} -->`;
  insert += `${eol}${indent}<bean name="${escapeXml(bean.name)}">`;
  for (const f of bean.fields) {
    if (f.comment) insert += `${eol}${varIndent}<!-- ${escapeXml(f.comment)} -->`;
    insert += `${eol}${varIndent}<var name="${escapeXml(f.name)}" type="${escapeXml(f.type)}" />`;
  }
  insert += `${eol}${indent}</bean>`;
  const mode = table.mode && table.mode !== 'map' ? ` mode="${escapeXml(table.mode)}"` : '';
  insert += `${eol}${indent}<table name="${escapeXml(table.name)}" value="${escapeXml(table.value)}"` +
    ` input="${escapeXml(table.input)}"${mode}/>`;

  const newText = insertBeforeClosingTag(text, '</module>', insert);
  return backupAndWrite(filePath, newText, backupDir);
}

module.exports = { addBeanField, addEnum, addBeanAndTable };
