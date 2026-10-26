// 解析 Defines/*.xml，产出完整 schema：
// 每个文件的 module / enums / beans / tables，附带前置 <!-- 中文注释 -->，
// 以及由 type 中的 #ref= 推导出的表引用关系。
const fs = require('fs');
const path = require('path');
const { XMLParser } = require('fast-xml-parser');
const { parseType } = require('./typeParser');

const parser = new XMLParser({
  preserveOrder: true,
  ignoreAttributes: false,
  attributeNamePrefix: '@_',
  commentPropName: '#comment',
  parseTagValue: false,
  parseAttributeValue: false,
  trimValues: true,
});

// 从 preserveOrder 的子节点数组中按序提取元素，并把前面的注释附到元素上。
// 注意：XML 注释没有行号信息，统一采用「注释归属于下一个元素」的规则。
function extractWithComments(children) {
  const items = [];
  let pendingComments = [];
  for (const node of children || []) {
    if (node['#comment'] !== undefined) {
      const texts = node['#comment']
        .map(c => (c['#text'] || '').trim())
        .filter(Boolean);
      pendingComments.push(...texts);
      continue;
    }
    if (node['#text'] !== undefined) continue; // 纯文本空白
    const tag = Object.keys(node).find(k => k !== ':@');
    if (!tag) continue;
    items.push({
      tag,
      attrs: node[':@'] || {},
      children: node[tag] || [],
      comment: pendingComments.join(' '),
    });
    pendingComments = [];
  }
  return items;
}

const attr = (attrs, name) => {
  const v = attrs['@_' + name];
  return v === undefined || v === null ? '' : String(v);
};

function parseEnum(item) {
  const vars = extractWithComments(item.children)
    .filter(c => c.tag === 'var')
    .map(c => ({
      name: attr(c.attrs, 'name'),
      alias: attr(c.attrs, 'alias'),
      value: attr(c.attrs, 'value'),
      comment: c.comment,
    }));
  return { name: attr(item.attrs, 'name'), comment: item.comment, items: vars };
}

function parseBean(item) {
  const fields = extractWithComments(item.children)
    .filter(c => c.tag === 'var')
    .map(c => ({
      name: attr(c.attrs, 'name'),
      type: attr(c.attrs, 'type'),
      group: attr(c.attrs, 'group'),
      comment: c.comment,
    }));
  return {
    name: attr(item.attrs, 'name'),
    sep: attr(item.attrs, 'sep'),
    comment: item.comment,
    fields,
  };
}

function parseTable(item) {
  return {
    name: attr(item.attrs, 'name'),
    value: attr(item.attrs, 'value'),   // 对应的 bean 名
    input: attr(item.attrs, 'input'),   // 相对 Datas/ 的 xlsx 路径
    mode: attr(item.attrs, 'mode') || 'map',
    group: attr(item.attrs, 'group'),
    index: attr(item.attrs, 'index'),
    comment: item.comment,
  };
}

/**
 * 解析整个 Defines 目录。
 * @param {string} definesDir Defines/ 的绝对路径
 */
function parseDefines(definesDir) {
  const files = fs.readdirSync(definesDir).filter(f => f.endsWith('.xml')).sort();
  const modules = [];
  const enums = {};   // name -> enum
  const beans = {};   // name -> bean
  const tables = {};  // name -> table

  for (const file of files) {
    const xml = fs.readFileSync(path.join(definesDir, file), 'utf8');
    let doc;
    try {
      doc = parser.parse(xml);
    } catch (e) {
      console.error(`[schema] 解析失败 ${file}: ${e.message}`);
      continue;
    }
    const root = doc.find(n => n.module !== undefined);
    if (!root) continue;

    const mod = {
      file,
      name: (root[':@'] && root[':@']['@_name']) || '',
      enums: [],
      beans: [],
      tables: [],
    };

    for (const item of extractWithComments(root.module)) {
      if (item.tag === 'enum') {
        const e = parseEnum(item);
        e.file = file;
        mod.enums.push(e);
        enums[e.name] = e;
      } else if (item.tag === 'bean') {
        const b = parseBean(item);
        b.file = file;
        mod.beans.push(b);
        beans[b.name] = b;
      } else if (item.tag === 'table') {
        const t = parseTable(item);
        t.file = file;
        mod.tables.push(t);
        tables[t.name] = t;
      }
    }
    modules.push(mod);
  }

  // 类型注册表 + 引用关系（字段 -> 目标表）
  const registry = {
    enums: new Set(Object.keys(enums)),
    beans: new Map(Object.entries(beans)),
  };
  const refs = []; // { table, field, target }
  for (const t of Object.values(tables)) {
    const bean = beans[t.value];
    if (!bean) continue;
    for (const f of bean.fields) {
      const pt = parseType(f.type, registry);
      collectRefs(pt, (target) => refs.push({ table: t.name, field: f.name, target }));
      f.parsedType = pt;
    }
  }

  return { modules, enums, beans, tables, refs, registry };
}

function collectRefs(parsedType, cb) {
  if (!parsedType) return;
  if (parsedType.kind === 'ref') cb(parsedType.ref);
  if (parsedType.kind === 'list') collectRefs(parsedType.element, cb);
}

module.exports = { parseDefines };
