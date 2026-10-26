import { state, getBean, tableComment, el, setStatus } from '../store.js';
import * as api from '../api.js';
import { buildTypeDatalist, buildTypeInput } from './typeInput.js';

// 数据结构总览：分「常量表 / 常规配置 / Defines（枚举+结构）」三类，类内按英文名排序
export function renderOverview(main, onOpenTable, filter = '') {
  main.innerHTML = '';
  const kw = filter.trim().toLowerCase();

  renderCreateArea(main, onOpenTable, filter);

  const allTables = Object.values(state.schema.tables);
  let constTables = allTables.filter(t => t.mode === 'one');
  let regularTables = allTables.filter(t => t.mode !== 'one');
  let enums = Object.values(state.schema.enums);
  let beans = Object.values(state.schema.beans)
    .filter(b => !allTables.some(t => t.value === b.name)); // 表对应的 bean 已在表卡片里展示
  if (kw) {
    constTables = constTables.filter(t => matchTable(t, kw));
    regularTables = regularTables.filter(t => matchTable(t, kw));
    enums = enums.filter(e => matchEnum(e, kw));
    beans = beans.filter(b => matchBean(b, kw));
  }
  const byName = (a, b) => a.name.localeCompare(b.name);

  appendTableSection(main, '常量表（单例）', constTables.sort(byName), onOpenTable, renderConstCard);
  appendTableSection(main, '常规配置', regularTables.sort(byName), onOpenTable, renderTableCard);
  appendDefinesSection(main, enums.sort(byName), beans.sort(byName), onOpenTable);

  if (!main.querySelector('.module-block')) {
    main.appendChild(el('div', 'empty-tip', '没有匹配的内容'));
  }
}

function appendTableSection(main, title, tables, onOpenTable, renderCard) {
  if (!tables.length) return;
  const block = el('div', 'module-block');
  block.appendChild(el('h2', null, `${title}（${tables.length}）`));
  const grid = el('div', 'card-grid');
  for (const t of tables) grid.appendChild(renderCard(t, onOpenTable));
  block.appendChild(grid);
  main.appendChild(block);
}

// 常量表（单例）卡片：直接展示当前值，字段 / 类型 / 值 / 说明
function renderConstCard(t, onOpenTable) {
  const card = el('div', 'card');
  const h3 = el('h3');
  const link = el('span', 'tlink', t.name);
  link.onclick = () => onOpenTable(t.name);
  link.title = '点击打开数据编辑';
  h3.appendChild(link);
  const comment = tableComment(t);
  if (comment) h3.appendChild(document.createTextNode(` ${comment}`));
  card.appendChild(h3);
  card.appendChild(el('div', 'meta', `${t.input} · 单例`));

  const bean = getBean(t.value);
  if (bean) {
    const ft = el('table', 'field-table');
    const head = el('tr');
    for (const h of ['字段', '类型', '值', '说明']) head.appendChild(el('th', null, h));
    ft.appendChild(head);
    for (const f of bean.fields) {
      const tr = el('tr');
      tr.appendChild(el('td', null, f.name));
      const typeTd = el('td', 'type');
      appendType(typeTd, f, onOpenTable);
      tr.appendChild(typeTd);
      const value = t.values ? (t.values[f.name] ?? '') : '（读取失败）';
      const vTd = el('td', 'const-value', value);
      if (value !== '') vTd.title = value;
      tr.appendChild(vTd);
      tr.appendChild(el('td', null, f.comment || ''));
      ft.appendChild(tr);
    }
    card.appendChild(ft);
  }
  return card;
}

function appendDefinesSection(main, enums, beans, onOpenTable) {
  if (!enums.length && !beans.length) return;
  const block = el('div', 'module-block');
  block.appendChild(el('h2', null, `Defines（${enums.length} 枚举 · ${beans.length} 结构）`));

  if (beans.length) {
    const grid = el('div', 'card-grid');
    for (const b of beans) grid.appendChild(renderBeanCard(b, onOpenTable));
    block.appendChild(grid);
  }
  for (const e of enums) block.appendChild(renderEnumBlock(e));
  main.appendChild(block);
}

// bean 结构卡片（非表对应的复合结构）
function renderBeanCard(b, onOpenTable) {
  const card = el('div', 'card');
  const h3 = el('h3');
  h3.appendChild(document.createTextNode(b.name));
  if (b.comment) h3.appendChild(document.createTextNode(` ${b.comment}`));
  card.appendChild(h3);
  card.appendChild(el('div', 'meta', `结构 · ${b.file}`));

  const ft = el('table', 'field-table');
  const head = el('tr');
  for (const h of ['字段', '类型', '说明']) head.appendChild(el('th', null, h));
  ft.appendChild(head);
  for (const f of b.fields) {
    const tr = el('tr');
    tr.appendChild(el('td', null, f.name));
    const typeTd = el('td', 'type');
    appendType(typeTd, f, onOpenTable);
    tr.appendChild(typeTd);
    tr.appendChild(el('td', null, f.comment || ''));
    ft.appendChild(tr);
  }
  card.appendChild(ft);
  return card;
}

function matchEnum(e, kw) {
  return e.name.toLowerCase().includes(kw) ||
    (e.comment || '').toLowerCase().includes(kw) ||
    e.items.some(i => i.name.toLowerCase().includes(kw) || (i.alias || '').includes(kw));
}

function matchBean(b, kw) {
  if (b.name.toLowerCase().includes(kw)) return true;
  if ((b.comment || '').toLowerCase().includes(kw)) return true;
  return b.fields.some(f =>
    f.name.toLowerCase().includes(kw) || (f.comment || '').toLowerCase().includes(kw));
}

function matchTable(t, kw) {
  if (t.name.toLowerCase().includes(kw)) return true;
  if (tableComment(t).toLowerCase().includes(kw)) return true;
  const bean = getBean(t.value);
  if (bean) {
    for (const f of bean.fields) {
      if (f.name.toLowerCase().includes(kw) || (f.comment || '').toLowerCase().includes(kw)) return true;
    }
  }
  return false;
}

function renderTableCard(t, onOpenTable) {
  const card = el('div', 'card');
  const h3 = el('h3');
  const link = el('span', 'tlink', t.name);
  link.onclick = () => onOpenTable(t.name);
  link.title = '点击打开数据编辑';
  h3.appendChild(link);
  const comment = tableComment(t);
  if (comment) h3.appendChild(document.createTextNode(` ${comment}`));
  card.appendChild(h3);

  const modeLabel = { map: '键值表', list: '列表', one: '单例' }[t.mode] || t.mode;
  card.appendChild(el('div', 'meta', `${t.input} · ${modeLabel}${t.group ? ' · 分组 ' + t.group : ''}`));

  const bean = getBean(t.value);
  if (bean) {
    const ft = el('table', 'field-table');
    const head = el('tr');
    for (const h of ['字段', '类型', '说明']) head.appendChild(el('th', null, h));
    ft.appendChild(head);
    for (const f of bean.fields) {
      const tr = el('tr');
      tr.appendChild(el('td', null, f.name));
      const typeTd = el('td', 'type');
      appendType(typeTd, f, onOpenTable);
      tr.appendChild(typeTd);
      tr.appendChild(el('td', null, f.comment || ''));
      ft.appendChild(tr);
    }
    card.appendChild(ft);
  }
  return card;
}

// 类型串渲染：枚举 → 可展开标签；ref → 跳转链接
function appendType(td, field, onOpenTable) {
  const pt = field.parsedType || { kind: 'other', raw: field.type };
  if (pt.kind === 'ref') {
    td.appendChild(document.createTextNode(pt.base + ' '));
    const a = el('span', 'ref-link', '→ ' + pt.ref);
    a.title = '引用 ' + pt.ref + '，点击打开';
    a.onclick = () => onOpenTable(pt.ref);
    td.appendChild(a);
    return;
  }
  if (pt.kind === 'enum') {
    const e = state.schema.enums[pt.enumName];
    if (e) {
      const preview = e.items.slice(0, 4).map(i => i.alias || i.name).join('/');
      const span = el('span', null, `${pt.raw}（${preview}${e.items.length > 4 ? '…' : ''}）`);
      span.title = e.items.map(i => `${i.alias || i.name} = ${i.name}`).join('\n');
      td.appendChild(span);
      return;
    }
  }
  if (pt.kind === 'list') {
    td.appendChild(el('span', null, pt.raw));
    td.title = `列表，分隔符「${pt.sep || '默认'}」`;
    return;
  }
  td.appendChild(el('span', null, field.type));
}

function renderEnumBlock(e) {
  const div = el('div', 'enum-block');
  const head = el('div');
  head.appendChild(el('span', 'enum-name', '枚举 ' + e.name));
  if (e.comment) head.appendChild(el('span', 'enum-comment', e.comment));
  if (e.file) head.appendChild(el('span', 'enum-comment', e.file));
  div.appendChild(head);
  const items = el('div');
  for (const i of e.items) {
    const label = i.alias ? `${i.alias} (${i.name})` : i.name;
    const tag = el('span', 'enum-tag', label);
    if (i.comment) tag.title = i.comment;
    items.appendChild(tag);
  }
  div.appendChild(items);
  return div;
}

/* ---------------- 新建表 / 新建枚举 ---------------- */

function renderCreateArea(main, onOpenTable, filter) {
  const bar = el('div', 'editor-actions');
  const tableBtn = el('button', 'btn', '＋ 新建表');
  const enumBtn = el('button', 'btn', '＋ 新建枚举');
  bar.appendChild(tableBtn);
  bar.appendChild(enumBtn);
  main.appendChild(bar);

  const tableForm = buildCreateTableForm(main, onOpenTable);
  const enumForm = buildCreateEnumForm(main, onOpenTable, filter);
  tableBtn.onclick = () => { enumForm.classList.remove('open'); tableForm.classList.toggle('open'); };
  enumBtn.onclick = () => { tableForm.classList.remove('open'); enumForm.classList.toggle('open'); };
  main.appendChild(tableForm);
  main.appendChild(enumForm);
}

// 归属定义文件下拉
function buildFileSelect() {
  const sel = el('select');
  for (const mod of state.schema.modules) {
    const opt = el('option', null, `${mod.name || mod.file.replace(/\.xml$/, '')}（${mod.file}）`);
    opt.value = mod.file;
    sel.appendChild(opt);
  }
  return sel;
}

// 可增删的小行编辑器：cols = [{placeholder, list?} | {build:()=>({root, getValue})}]
function buildRowEditor(cols, addLabel) {
  const root = el('div', 'mini-rows');
  const list = el('div');
  root.appendChild(list);
  const addBtn = el('button', 'btn', addLabel);
  root.appendChild(addBtn);

  function addRow() {
    const row = el('div', 'mini-row');
    const getters = [];
    for (const c of cols) {
      if (c.build) {
        const w = c.build();
        row.appendChild(w.root);
        getters.push(w.getValue);
      } else {
        const input = el('input');
        input.placeholder = c.placeholder;
        if (c.list) input.setAttribute('list', c.list);
        row.appendChild(input);
        getters.push(() => input.value.trim());
      }
    }
    row._getters = getters;
    const del = el('button', 'btn row-del', '✕');
    del.title = '删除此行';
    del.onclick = () => list.removeChild(row);
    row.appendChild(del);
    list.appendChild(row);
    return row;
  }
  addBtn.onclick = () => addRow();
  addRow();

  return {
    root,
    addRow,
    getRows: () => [...list.children]
      .map(row => row._getters.map(g => g()))
      .filter(r => r.some(v => v !== '')),
  };
}

function formRow(...nodes) {
  const row = el('div', 'form-row');
  for (const n of nodes) row.appendChild(n);
  return row;
}

function buildCreateTableForm(main, onOpenTable) {
  const form = el('div', 'add-field-form');
  form.appendChild(el('div', 'form-tip',
    '在选中的 Defines 文件里追加 bean + table 声明，并在 Datas 下新建同名 xlsx（只含表头）；键值表（map）的第一个字段即主键。'));

  const fileSel = buildFileSelect();
  const tableIn = el('input');
  tableIn.placeholder = '表名（如 TbXxxConfig）';
  const beanIn = el('input');
  beanIn.placeholder = 'bean 名（默认表名去 Tb 前缀）';
  let beanTouched = false;
  beanIn.oninput = () => { beanTouched = true; };
  tableIn.oninput = () => {
    if (!beanTouched) beanIn.value = tableIn.value.trim().replace(/^Tb/, '');
  };
  const commentIn = el('input');
  commentIn.placeholder = '中文说明（可选）';
  form.appendChild(formRow(fileSel, tableIn, beanIn, commentIn));

  const modeSel = el('select');
  for (const [v, label] of [['map', '键值表（map）'], ['list', '列表（list）'], ['one', '单例（one）']]) {
    const opt = el('option', null, label);
    opt.value = v;
    modeSel.appendChild(opt);
  }
  const dirSel = el('select');
  for (const d of state.schema.dataDirs || []) {
    const opt = el('option', null, `Datas/${d}/`);
    opt.value = d;
    dirSel.appendChild(opt);
  }
  form.appendChild(formRow(modeSel, dirSel));

  const typeListId = 'create-type-options';
  const fields = buildRowEditor([
    { placeholder: '字段名（英文）' },
    { build: () => { const w = buildTypeInput(typeListId); return { root: w.root, getValue: w.getType }; } },
    { placeholder: '中文说明（可选）' },
  ], '＋ 加字段');
  fields.addRow(); // 默认两行：主键 + 一个业务字段
  form.appendChild(fields.root);
  form.appendChild(buildTypeDatalist(typeListId));

  const okBtn = el('button', 'btn primary', '创建表');
  const cancelBtn = el('button', 'btn', '取消');
  form.appendChild(formRow(okBtn, cancelBtn));

  cancelBtn.onclick = () => form.classList.remove('open');
  okBtn.onclick = async () => {
    const rows = fields.getRows();
    const payload = {
      file: fileSel.value,
      tableName: tableIn.value.trim(),
      beanName: beanIn.value.trim(),
      comment: commentIn.value.trim(),
      mode: modeSel.value,
      dir: dirSel.value,
      fields: rows.map(r => ({ name: r[0], type: r[1], title: r[2] })),
    };
    if (!payload.tableName || !payload.beanName) {
      setStatus('表名和 bean 名必填', 'err');
      return;
    }
    if (!payload.fields.length) {
      setStatus('至少填写 1 个字段（字段名 + 类型）', 'err');
      return;
    }
    okBtn.disabled = true;
    try {
      const res = await api.createTable(payload);
      state.schema = await api.getSchema(); // 后端已重扫 Defines，前端同步缓存
      setStatus(`已创建表 ${res.table}，可开始录入数据`, 'ok');
      onOpenTable(res.table);
    } catch (e) {
      setStatus('创建表失败：' + e.message, 'err');
      okBtn.disabled = false;
    }
  };
  return form;
}

function buildCreateEnumForm(main, onOpenTable, filter) {
  const form = el('div', 'add-field-form');
  form.appendChild(el('div', 'form-tip',
    '在选中的 Defines 文件里追加 <enum> 定义；取值写「程序名 + 中文别名」，别名即 Excel 里填的中文。'));

  const fileSel = buildFileSelect();
  const nameIn = el('input');
  nameIn.placeholder = '枚举名（英文，如 CardType）';
  const commentIn = el('input');
  commentIn.placeholder = '中文说明（可选）';
  form.appendChild(formRow(fileSel, nameIn, commentIn));

  const items = buildRowEditor([
    { placeholder: '程序名（如 Warrior）' },
    { placeholder: '中文别名（如 战士）' },
  ], '＋ 加取值');
  form.appendChild(items.root);

  const okBtn = el('button', 'btn primary', '创建枚举');
  const cancelBtn = el('button', 'btn', '取消');
  form.appendChild(formRow(okBtn, cancelBtn));

  cancelBtn.onclick = () => form.classList.remove('open');
  okBtn.onclick = async () => {
    const rows = items.getRows();
    const payload = {
      file: fileSel.value,
      name: nameIn.value.trim(),
      comment: commentIn.value.trim(),
      items: rows.map(r => ({ name: r[0], alias: r[1] })),
    };
    if (!payload.name) {
      setStatus('枚举名必填', 'err');
      return;
    }
    if (!payload.items.length) {
      setStatus('至少填写 1 个取值', 'err');
      return;
    }
    okBtn.disabled = true;
    try {
      await api.createEnum(payload);
      state.schema = await api.getSchema();
      setStatus(`已创建枚举 ${payload.name}`, 'ok');
      renderOverview(main, onOpenTable, filter);
    } catch (e) {
      setStatus('创建枚举失败：' + e.message, 'err');
      okBtn.disabled = false;
    }
  };
  return form;
}
