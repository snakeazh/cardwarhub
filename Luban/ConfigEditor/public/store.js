// 全局状态与小工具
export const state = {
  schema: null,          // /api/schema 的完整结果
  currentTable: null,    // 当前打开的表名（null = 总览页）
};

export function getEnum(name) {
  return state.schema.enums[name] || null;
}

export function getTable(name) {
  return state.schema.tables[name] || null;
}

export function getBean(name) {
  return state.schema.beans[name] || null;
}

// 表的中文说明：优先 table 注释，其次对应 bean 注释
export function tableComment(table) {
  if (table.comment) return table.comment;
  const bean = getBean(table.value);
  return bean ? bean.comment : '';
}

// 枚举单元格的存储值：有 alias 用 alias（与现有 Excel 一致），否则用 name
export function enumStorageValue(item) {
  return item.alias || item.name;
}

export function el(tag, className, text) {
  const e = document.createElement(tag);
  if (className) e.className = className;
  if (text !== undefined && text !== null) e.textContent = text;
  return e;
}

export function setStatus(msg, kind) {
  const s = document.getElementById('status-msg');
  s.textContent = msg || '';
  s.className = kind || '';
  if (msg) setTimeout(() => { if (s.textContent === msg) { s.textContent = ''; s.className = ''; } }, 6000);
}
