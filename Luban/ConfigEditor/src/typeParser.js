// 解析 Luban 类型串，产出前端渲染编辑器所需的结构化信息。
// 支持：基础类型(bool/int/float/string/text/datetime/float3...)、
//       type#ref=TbName 表引用、(list#sep=|),T 容器、具名 enum / bean。

const NUMBER_TYPES = new Set(['byte', 'short', 'int', 'long', 'float', 'double', 'fshort', 'fint', 'flong']);
const STRING_TYPES = new Set(['string', 'datetime']);
const SIMPLE_TYPES = new Set([...NUMBER_TYPES, ...STRING_TYPES, 'bool', 'text',
  'vector2', 'vector3', 'vector4', 'float2', 'float3', 'float4']);

// (list#sep=|),int  ->  { container:'list', sep:'|', element:'int' }
function parseContainer(raw) {
  const m = raw.match(/^\((list|array|set)(?:#sep=(.*))?\),(.*)$/);
  if (!m) return null;
  return { container: m[1], sep: m[2] || '', element: m[3].trim() };
}

// int#ref=TbItemConfig  ->  { base:'int', ref:'TbItemConfig' }
function parseRef(raw) {
  const m = raw.match(/^([^#]+)#ref=(.+)$/);
  if (!m) return null;
  return { base: m[1].trim(), ref: m[2].trim() };
}

/**
 * @param {string} raw 类型原串
 * @param {object} registry { enums:Set<string>, beans:Map<string,bean> }
 */
function parseType(raw, registry) {
  raw = (raw || '').trim();

  const container = parseContainer(raw);
  if (container) {
    return {
      raw,
      kind: 'list',
      container: container.container,
      sep: container.sep,
      element: parseType(container.element, registry),
    };
  }

  const ref = parseRef(raw);
  if (ref) {
    return { raw, kind: 'ref', base: ref.base, ref: ref.ref };
  }

  if (registry.enums.has(raw)) {
    return { raw, kind: 'enum', enumName: raw };
  }
  if (registry.beans.has(raw)) {
    const bean = registry.beans.get(raw);
    return { raw, kind: 'bean', beanName: raw, sep: bean ? bean.sep : '' };
  }
  if (raw === 'bool') return { raw, kind: 'bool' };
  if (raw === 'text') return { raw, kind: 'text' };
  if (NUMBER_TYPES.has(raw)) return { raw, kind: 'number', base: raw };
  if (STRING_TYPES.has(raw)) return { raw, kind: 'string', base: raw };
  if (SIMPLE_TYPES.has(raw)) return { raw, kind: 'string', base: raw };

  return { raw, kind: 'other' };
}

module.exports = { parseType, NUMBER_TYPES };
