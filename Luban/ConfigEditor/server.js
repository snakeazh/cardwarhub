// Luban 配置工具本地服务：静态托管 public/ + REST API。
// 用法：node server.js [port]   （默认 5678）
const http = require('http');
const fs = require('fs');
const path = require('path');
const { parseDefines } = require('./src/schemaParser');
const { parseType } = require('./src/typeParser');
const { readTable, writeTable, appendColumnField, createTableXlsx } = require('./src/excelIO');
const { addBeanField, addEnum, addBeanAndTable } = require('./src/definesWriter');

const ROOT = path.resolve(__dirname, '..');          // Luban 根目录
const DEFINES_DIR = path.join(ROOT, 'Defines');
const DATAS_DIR = path.join(ROOT, 'Datas');
const BACKUP_DIR = path.join(DATAS_DIR, '.backup');
const PUBLIC_DIR = path.join(__dirname, 'public');

let schema = null;

async function loadSchema() {
  schema = parseDefines(DEFINES_DIR);
  // 常量表（mode=one）顺带读出当前值，供总览页直接展示
  for (const t of Object.values(schema.tables)) {
    if (t.mode !== 'one') continue;
    try {
      const { columnMode, fields, rows } = await readTable(tableFilePath(t));
      if (columnMode && rows.length) {
        const bean = schema.beans[t.value];
        const known = new Set(bean ? bean.fields.map(f => f.name) : []);
        const values = {};
        fields.forEach((f, i) => {
          if (known.has(f.name)) values[f.name] = rows[0][i]; // 过滤掉说明性表头行
        });
        t.values = values;
      }
    } catch (e) {
      console.warn(`[schema] 常量表 ${t.name} 读取失败: ${e.message}`);
    }
  }
  const t = Object.keys(schema.tables).length;
  const b = Object.keys(schema.beans).length;
  const e = Object.keys(schema.enums).length;
  console.log(`[schema] 已加载 ${schema.modules.length} 个定义文件：${t} 表 / ${b} bean / ${e} 枚举`);
}

// 去掉不能 JSON 序列化的 registry；附带 Datas 子目录列表（建表时选目录用）
function schemaJson() {
  const dataDirs = fs.readdirSync(DATAS_DIR, { withFileTypes: true })
    .filter(d => d.isDirectory() && !d.name.startsWith('.'))
    .map(d => d.name)
    .sort();
  return {
    modules: schema.modules,
    enums: schema.enums,
    beans: schema.beans,
    tables: schema.tables,
    refs: schema.refs,
    dataDirs,
  };
}

// Defines 下已存在的 xml 文件（只允许直接子文件，防路径穿越）
function definesFilePath(file) {
  const name = String(file || '').trim();
  if (!/^[A-Za-z0-9_-]+\.xml$/.test(name)) throw new Error(`非法的定义文件名: ${name}`);
  const p = path.join(DEFINES_DIR, name);
  if (!fs.existsSync(p)) throw new Error(`定义文件不存在: ${name}`);
  return p;
}

function tableFilePath(table) {
  // input 形如 "GamePlay/BuffConfig.xlsx"，相对 Datas/
  const p = path.normalize(path.join(DATAS_DIR, table.input));
  if (!p.startsWith(DATAS_DIR)) throw new Error('非法的 input 路径');
  return p;
}

// 列信息：xlsx 实际字段 + bean 中的类型/注释
function buildColumns(table, xlsxFields) {
  const bean = schema.beans[table.value];
  const beanFields = bean ? bean.fields : [];
  return xlsxFields.map(xf => {
    const bf = beanFields.find(f => f.name === xf.name);
    const pt = bf ? bf.parsedType : parseType('', schema.registry);
    return {
      field: xf.name,
      title: xf.title,
      type: bf ? bf.type : '',
      comment: bf ? bf.comment : '',
      kind: pt.kind,
      parsedType: pt,
    };
  });
}

const MIME = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.png': 'image/png',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon',
};

function sendJson(res, code, obj) {
  const body = JSON.stringify(obj);
  res.writeHead(code, { 'Content-Type': 'application/json; charset=utf-8' });
  res.end(body);
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    req.on('data', c => chunks.push(c));
    req.on('end', () => {
      try { resolve(chunks.length ? JSON.parse(Buffer.concat(chunks).toString('utf8')) : {}); }
      catch (e) { reject(e); }
    });
    req.on('error', reject);
  });
}

async function handleApi(req, res, url) {
  const parts = url.pathname.split('/').filter(Boolean); // ['api', ...]

  if (req.method === 'GET' && url.pathname === '/api/schema') {
    return sendJson(res, 200, schemaJson());
  }

  if (req.method === 'POST' && url.pathname === '/api/reload') {
    await loadSchema();
    return sendJson(res, 200, { ok: true });
  }

  // /api/table/:name/data
  if (parts[0] === 'api' && parts[1] === 'table' && parts[3] === 'data') {
    const name = decodeURIComponent(parts[2]);
    const table = schema.tables[name];
    if (!table) return sendJson(res, 404, { error: `表不存在: ${name}` });
    const filePath = tableFilePath(table);

    if (req.method === 'GET') {
      if (!fs.existsSync(filePath)) {
        return sendJson(res, 404, { error: `数据文件不存在: ${table.input}` });
      }
      const { columnMode, fields, rows } = await readTable(filePath);
      let columns = buildColumns(table, fields);
      let outRows = rows;
      if (columnMode) {
        // 列模式只暴露 bean 里定义了的字段（过滤掉「程序中的字段名称」之类的说明行）
        const keep = [];
        columns = columns.filter((c, i) => {
          const bf = (schema.beans[table.value] ? schema.beans[table.value].fields : []);
          const ok = bf.some(f => f.name === c.field);
          if (ok) keep.push(i);
          return ok;
        });
        outRows = rows.map(r => keep.map(i => r[i]));
      }
      return sendJson(res, 200, {
        table: { name: table.name, value: table.value, input: table.input, mode: table.mode, comment: table.comment },
        columnMode,
        columns,
        rows: outRows,
      });
    }

    if (req.method === 'PUT') {
      const body = await readBody(req);
      if (!Array.isArray(body.rows)) return sendJson(res, 400, { error: 'body.rows 必须是二维数组' });
      if (!Array.isArray(body.fields)) return sendJson(res, 400, { error: 'body.fields 必须是字段名数组' });
      const { fields } = await readTable(filePath);
      const columns = buildColumns(table, fields);
      const kindOf = {};
      for (const c of columns) kindOf[c.field] = c.kind;
      const kinds = body.fields.map(f => kindOf[f] || 'string');
      for (const row of body.rows) {
        if (!Array.isArray(row) || row.length !== body.fields.length) {
          return sendJson(res, 400, { error: `每行必须是 ${body.fields.length} 列的数组` });
        }
      }
      const { backupPath } = await writeTable(filePath, body.fields, body.rows, kinds, BACKUP_DIR);
      return sendJson(res, 200, { ok: true, backup: path.relative(ROOT, backupPath), rowCount: body.rows.length });
    }
  }

  // /api/table/:name/field  —— 单例表加字段：同步改 Defines XML + xlsx 末尾插行
  if (parts[0] === 'api' && parts[1] === 'table' && parts[3] === 'field' && req.method === 'POST') {
    const name = decodeURIComponent(parts[2]);
    const table = schema.tables[name];
    if (!table) return sendJson(res, 404, { error: `表不存在: ${name}` });

    const body = await readBody(req);
    const fieldName = String(body.field || '').trim();
    const type = String(body.type || '').trim();
    const title = String(body.title || '').trim();
    const value = body.value === undefined || body.value === null ? '' : String(body.value);

    if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(fieldName)) {
      return sendJson(res, 400, { error: '字段名必须是合法的英文标识符（字母/数字/下划线，不以数字开头）' });
    }
    if (!type) return sendJson(res, 400, { error: '类型不能为空' });
    const pt = parseType(type, schema.registry);
    if (pt.kind === 'other') {
      return sendJson(res, 400, { error: `无法识别的类型: ${type}（支持基础类型、枚举名、bean 名、(list#sep=..) 容器）` });
    }

    const bean = schema.beans[table.value];
    if (!bean) return sendJson(res, 400, { error: `找不到表对应的 bean: ${table.value}` });
    if (bean.fields.some(f => f.name === fieldName)) {
      return sendJson(res, 400, { error: `字段 "${fieldName}" 已存在于 bean` });
    }

    // 预检 xlsx：必须列模式且字段未存在（先检后写，避免改完 XML 才发现 xlsx 不行）
    const filePath = tableFilePath(table);
    const { columnMode, fields } = await readTable(filePath);
    if (!columnMode) return sendJson(res, 400, { error: '仅单例表（列模式）支持在编辑器里加字段' });
    if (fields.some(f => f.name === fieldName)) {
      return sendJson(res, 400, { error: `字段 "${fieldName}" 已存在于 xlsx` });
    }

    const backups = [];
    const xmlPath = path.join(DEFINES_DIR, bean.file);
    const r1 = addBeanField(xmlPath, bean.name, { name: fieldName, type, comment: title }, BACKUP_DIR);
    backups.push(path.relative(ROOT, r1.backupPath));
    const r2 = await appendColumnField(filePath, fieldName, title, value, pt.kind, BACKUP_DIR);
    backups.push(path.relative(ROOT, r2.backupPath));

    await loadSchema(); // 刷新缓存，新字段立即可见
    return sendJson(res, 200, { ok: true, backups });
  }

  // /api/enum  —— 新建枚举：只改 Defines XML
  if (req.method === 'POST' && url.pathname === '/api/enum') {
    const body = await readBody(req);
    const name = String(body.name || '').trim();
    const comment = String(body.comment || '').trim();
    const items = Array.isArray(body.items) ? body.items : [];

    let xmlPath;
    try { xmlPath = definesFilePath(body.file); } catch (e) { return sendJson(res, 400, { error: e.message }); }
    if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(name)) {
      return sendJson(res, 400, { error: '枚举名必须是合法的英文标识符' });
    }
    if (schema.enums[name] || schema.beans[name] || schema.tables[name]) {
      return sendJson(res, 400, { error: `名称 "${name}" 已被现有枚举/bean/表占用` });
    }
    if (!items.length) return sendJson(res, 400, { error: '枚举至少要有 1 个取值' });
    const seen = new Set();
    for (const it of items) {
      const n = String(it.name || '').trim();
      if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(n)) {
        return sendJson(res, 400, { error: `取值名 "${n}" 不是合法的英文标识符` });
      }
      if (seen.has(n)) return sendJson(res, 400, { error: `取值名 "${n}" 重复` });
      seen.add(n);
    }

    const r = addEnum(xmlPath, {
      name, comment,
      items: items.map(it => ({ name: String(it.name).trim(), alias: String(it.alias || '').trim() })),
    }, BACKUP_DIR);
    await loadSchema();
    return sendJson(res, 200, { ok: true, backup: path.relative(ROOT, r.backupPath) });
  }

  // /api/table  —— 新建表：Defines XML 加 bean+table，Datas 下建数据 xlsx
  if (req.method === 'POST' && url.pathname === '/api/table') {
    const body = await readBody(req);
    const tableName = String(body.tableName || '').trim();
    const beanName = String(body.beanName || '').trim();
    const comment = String(body.comment || '').trim();
    const mode = String(body.mode || 'map').trim();
    const dir = String(body.dir || '').trim().replace(/[/\\]+$/, '');
    const fields = Array.isArray(body.fields) ? body.fields : [];

    let xmlPath;
    try { xmlPath = definesFilePath(body.file); } catch (e) { return sendJson(res, 400, { error: e.message }); }
    for (const [label, n] of [['表名', tableName], ['bean 名', beanName]]) {
      if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(n)) {
        return sendJson(res, 400, { error: `${label}必须是合法的英文标识符` });
      }
    }
    if (schema.tables[tableName]) return sendJson(res, 400, { error: `表 "${tableName}" 已存在` });
    if (schema.beans[beanName] || schema.enums[beanName]) {
      return sendJson(res, 400, { error: `bean 名 "${beanName}" 已被现有 bean/枚举占用` });
    }
    if (!['map', 'list', 'one'].includes(mode)) {
      return sendJson(res, 400, { error: 'mode 只能是 map / list / one' });
    }
    const dataDir = path.normalize(path.join(DATAS_DIR, dir));
    if (!dir || !dataDir.startsWith(DATAS_DIR) || !fs.existsSync(dataDir) || !fs.statSync(dataDir).isDirectory()) {
      return sendJson(res, 400, { error: `数据目录不存在: ${dir}` });
    }
    if (!fields.length) return sendJson(res, 400, { error: '至少要定义 1 个字段' });
    const seen = new Set();
    for (const f of fields) {
      const n = String(f.name || '').trim();
      const t = String(f.type || '').trim();
      if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(n)) {
        return sendJson(res, 400, { error: `字段名 "${n}" 不是合法的英文标识符` });
      }
      if (seen.has(n)) return sendJson(res, 400, { error: `字段名 "${n}" 重复` });
      seen.add(n);
      if (parseType(t, schema.registry).kind === 'other') {
        return sendJson(res, 400, { error: `无法识别的类型: ${t}` });
      }
    }

    const input = `${dir}/${beanName}.xlsx`;
    const xlsxPath = path.join(DATAS_DIR, input);
    if (fs.existsSync(xlsxPath)) {
      return sendJson(res, 400, { error: `数据文件已存在: ${input}` });
    }

    // 先改 XML 再建 xlsx；xlsx 失败则回滚 XML
    const normFields = fields.map(f => ({
      name: String(f.name).trim(), type: String(f.type).trim(), comment: String(f.title || '').trim(),
    }));
    const r = addBeanAndTable(xmlPath,
      { name: beanName, comment, fields: normFields },
      { name: tableName, value: beanName, input, mode },
      BACKUP_DIR);
    try {
      await createTableXlsx(xlsxPath, mode, normFields.map(f => ({ name: f.name, title: f.comment })));
    } catch (e) {
      fs.copyFileSync(r.backupPath, xmlPath); // 回滚 XML
      return sendJson(res, 500, { error: `创建 xlsx 失败，已回滚 XML 改动: ${e.message}` });
    }

    await loadSchema();
    return sendJson(res, 200, { ok: true, table: tableName, backup: path.relative(ROOT, r.backupPath) });
  }

  return sendJson(res, 404, { error: 'not found' });
}

function serveStatic(req, res, url) {
  let rel = decodeURIComponent(url.pathname);
  if (rel === '/') rel = '/index.html';
  const filePath = path.normalize(path.join(PUBLIC_DIR, rel));
  if (!filePath.startsWith(PUBLIC_DIR)) {
    res.writeHead(403); return res.end('forbidden');
  }
  fs.readFile(filePath, (err, data) => {
    if (err) { res.writeHead(404); return res.end('not found'); }
    res.writeHead(200, { 'Content-Type': MIME[path.extname(filePath)] || 'application/octet-stream' });
    res.end(data);
  });
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, 'http://localhost');
  try {
    if (url.pathname.startsWith('/api/')) {
      await handleApi(req, res, url);
    } else {
      serveStatic(req, res, url);
    }
  } catch (e) {
    console.error(e);
    sendJson(res, 500, { error: e.message });
  }
});

const port = Number(process.argv[2]) || 5678;
loadSchema().then(() => {
  server.listen(port, () => {
    console.log(`Luban 配置工具已启动: http://localhost:${port}`);
  });
});
