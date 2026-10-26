import { state, el } from '../store.js';

// 类型候选 datalist：基础类型 + 枚举名 + bean 名（同一页面只允许一个同 id 实例）
export function buildTypeDatalist(id) {
  const dl = el('datalist');
  dl.id = id;
  for (const t of ['bool', 'int', 'long', 'float', 'double', 'string', 'text']) {
    dl.appendChild(el('option')).value = t;
  }
  for (const n of Object.keys(state.schema.enums)) dl.appendChild(el('option')).value = n;
  for (const n of Object.keys(state.schema.beans)) dl.appendChild(el('option')).value = n;
  return dl;
}

// 列表分隔符候选：| 是项目绝对主流（Defines 里 180+ 处），其余个别使用
const SEPS = ['|', ',', ':', ';', '_'];

/**
 * 类型构造器：[普通/列表] + 元素类型（datalist）+ [分隔符（列表时显示）]
 * 列表自动拼成 Luban 语法 (list#sep=X),T，策划不用手写。
 * @param {string} datalistId buildTypeDatalist 生成的 datalist id
 * @returns {{root:HTMLElement, input:HTMLElement, getType:()=>string}}
 */
export function buildTypeInput(datalistId) {
  const root = el('span', 'type-input');

  const modeSel = el('select');
  modeSel.title = '普通 = 单个值；列表 = 多个值用分隔符连起来（如 1|2|3）';
  for (const [v, label] of [['plain', '普通'], ['list', '列表']]) {
    const opt = el('option', null, label);
    opt.value = v;
    modeSel.appendChild(opt);
  }
  modeSel.value = 'plain';

  const base = el('input');
  base.placeholder = '类型';
  base.setAttribute('list', datalistId);

  const sepSel = el('select');
  sepSel.title = '列表元素之间的分隔符（数据里填写时用的符号）';
  for (const s of SEPS) {
    const opt = el('option', null, s);
    opt.value = s;
    sepSel.appendChild(opt);
  }
  sepSel.value = SEPS[0];
  sepSel.style.display = 'none';
  modeSel.onchange = () => { sepSel.style.display = modeSel.value === 'list' ? '' : 'none'; };

  root.appendChild(modeSel);
  root.appendChild(base);
  root.appendChild(sepSel);

  return {
    root,
    input: base,
    getType() {
      const t = base.value.trim();
      if (!t) return '';
      return modeSel.value === 'list' ? `(list#sep=${sepSel.value}),${t}` : t;
    },
  };
}
