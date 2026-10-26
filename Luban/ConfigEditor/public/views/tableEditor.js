import { state, getEnum, enumStorageValue, el, setStatus } from '../store.js';
import * as api from '../api.js';
import { buildTypeDatalist, buildTypeInput } from './typeInput.js';

// 数据表编辑器：类型感知网格 + 行增删 + 校验 + 整表保存
export async function renderTableEditor(main, tableName, onOpenTable) {
  main.innerHTML = '<div class="empty-tip">加载中…</div>';
  let data;
  try {
    data = await api.getTableData(tableName);
  } catch (e) {
    main.innerHTML = '';
    main.appendChild(el('div', 'empty-tip', '加载失败：' + e.message));
    return;
  }
  if (state.currentTable !== tableName) return; // 已切走

  const { table, columnMode, columns, rows } = data;
  const dirty = new Set(); // "r,c"

  main.innerHTML = '';

  // 头部
  const head = el('div', 'editor-head');
  head.appendChild(el('h2', null, table.name));
  if (table.comment) head.appendChild(el('span', 'meta', table.comment));
  head.appendChild(el('span', 'meta', `${table.input} · ${columnMode ? '单例' : rows.length + ' 行'}`));
  main.appendChild(head);

  // 操作区
  const actions = el('div', 'editor-actions');
  const saveBtn = el('button', 'btn primary', '保存');
  saveBtn.disabled = true;
  saveBtn.onclick = doSave;
  const addBtn = el('button', 'btn', '＋ 添加行');
  if (!columnMode) {
    addBtn.onclick = () => { rows.push(columns.map(() => '')); appendRow(rows.length - 1); markDirty(); };
    actions.appendChild(addBtn);
  }
  actions.appendChild(saveBtn);
  if (columnMode) {
    const addFieldBtn = el('button', 'btn', '＋ 添加字段');
    addFieldBtn.onclick = () => {
      fieldForm.classList.toggle('open');
      if (fieldForm.classList.contains('open')) fieldForm.querySelector('input').focus();
    };
    actions.appendChild(addFieldBtn);
  }
  const refreshBtn = el('button', 'btn', '放弃修改并刷新');
  refreshBtn.onclick = () => renderTableEditor(main, tableName, onOpenTable);
  actions.appendChild(refreshBtn);
  main.appendChild(actions);

  // 单例表加字段表单：同步改 Defines XML + xlsx 末尾插行
  const fieldForm = columnMode ? buildAddFieldForm() : null;
  if (fieldForm) main.appendChild(fieldForm);

  // 网格
  const wrap = el('div', 'grid-wrap');
  const grid = el('table', 'data-grid');
  const thead = el('thead');
  const htr = el('tr');
  if (columnMode) {
    // 常量表按行绘制：每行一个字段
    for (const h of ['字段', '类型', '值', '说明']) htr.appendChild(el('th', null, h));
  } else {
    htr.appendChild(el('th', null, '#'));
    columns.forEach((c) => {
      const th = el('th');
      th.appendChild(el('div', null, c.title || c.field));
      const sub = el('div', 'fname', `${c.field}: ${c.type || '?'}`);
      th.appendChild(sub);
      if (c.kind === 'ref' && state.schema.tables[c.parsedType.ref]) {
        const jump = el('span', 'ref-jump', '→ ' + c.parsedType.ref);
        jump.onclick = () => onOpenTable(c.parsedType.ref);
        th.appendChild(jump);
      }
      if (c.comment) th.title = c.comment;
      htr.appendChild(th);
    });
    htr.appendChild(el('th', null, ''));
  }
  thead.appendChild(htr);
  grid.appendChild(thead);
  const tbody = el('tbody');
  grid.appendChild(tbody);
  wrap.appendChild(grid);
  main.appendChild(wrap);

  const hint = el('div', 'column-hint',
    '黄色 = 已修改未保存 · 红色 = 校验不通过 · 保存前会自动备份原文件到 Datas/.backup/');
  main.appendChild(hint);

  const rowEls = [];
  if (columnMode) {
    columns.forEach((c, ci) => appendFieldRow(c, ci));
  } else {
    rows.forEach((_, r) => appendRow(r));
  }

  // 常量表（列模式）：一个字段一行
  function appendFieldRow(c, ci) {
    const tr = el('tr');
    const nameTd = el('td', 'fname-cell', c.field);
    if (c.title) nameTd.title = c.title;
    tr.appendChild(nameTd);
    tr.appendChild(el('td', 'type-cell', c.type || '?'));
    const td = el('td');
    td.appendChild(buildEditor(c, rows[0][ci], (val) => {
      rows[0][ci] = val;
      td.classList.add('dirty');
      td.classList.remove('invalid');
      dirty.add('0,' + ci);
      markDirty();
    }));
    tr.appendChild(td);
    tr.appendChild(el('td', 'comment-cell', c.comment || c.title || ''));
    tbody.appendChild(tr);
    rowEls[ci] = tr;
  }

  function appendRow(r) {
    const tr = el('tr');
    const idxTd = el('td', 'idx', String(r + 1));
    tr.appendChild(idxTd);

    columns.forEach((c, ci) => {
      const td = el('td');
      td.appendChild(buildEditor(c, rows[r][ci], (val) => {
        rows[r][ci] = val;
        td.classList.add('dirty');
        td.classList.remove('invalid');
        dirty.add(r + ',' + ci);
        markDirty();
      }));
      tr.appendChild(td);
    });

    if (!columnMode) {
      const ops = el('td', 'cell-ops');
      const del = el('button', null, '✕');
      del.title = '删除此行';
      del.onclick = () => {
        if (!confirm(`删除第 ${r + 1} 行？（保存后生效）`)) return;
        rows.splice(r, 1);
        rebuild();
        markDirty();
      };
      ops.appendChild(del);
      tr.appendChild(ops);
    }
    tbody.appendChild(tr);
    rowEls[r] = tr;
  }

  function rebuild() {
    tbody.innerHTML = '';
    rowEls.length = 0;
    dirty.clear();
    rows.forEach((_, r) => appendRow(r));
    rows.forEach((_, r) => { /* 重建后所有行视为待保存 */ });
    markDirty();
  }

  function markDirty() {
    saveBtn.disabled = false;
  }

  function validate() {
    let ok = true;
    tbody.querySelectorAll('td.invalid').forEach(td => td.classList.remove('invalid'));
    rows.forEach((row, r) => {
      columns.forEach((c, ci) => {
        const v = row[ci];
        let valid = true;
        if (c.kind === 'number' && v !== '' && Number.isNaN(Number(v))) valid = false;
        if (c.kind === 'enum' && v !== '') {
          const e = getEnum(c.parsedType.enumName);
          if (e && !e.items.some(i => i.name === v || i.alias === v)) valid = false;
        }
        if (!valid) {
          ok = false;
          // 列模式：rowEls 按字段索引，值在第 3 格；行模式：rowEls 按数据行，前面有 # 列
          const td = columnMode
            ? rowEls[ci] && rowEls[ci].cells[2]
            : rowEls[r] && rowEls[r].cells[ci + 1];
          if (td) td.classList.add('invalid');
        }
      });
    });
    return ok;
  }

  async function doSave() {
    if (!validate()) {
      setStatus('有单元格未通过校验（红色），请修正后再保存', 'err');
      return;
    }
    saveBtn.disabled = true;
    try {
      const res = await api.saveTableData(tableName, columns.map(c => c.field), rows);
      tbody.querySelectorAll('td.dirty').forEach(td => td.classList.remove('dirty'));
      dirty.clear();
      setStatus(`已保存 ${res.rowCount} 行，备份：${res.backup}`, 'ok');
    } catch (e) {
      setStatus('保存失败：' + e.message, 'err');
      saveBtn.disabled = false;
    }
  }

  // 加字段表单：字段名 / 类型 / 中文说明 / 默认值，提交后后端同步改 XML + xlsx
  function buildAddFieldForm() {
    const form = el('div', 'add-field-form');
    form.appendChild(el('div', 'form-tip',
      '新字段会追加到 bean 定义（Defines XML），并在 xlsx 末尾插入一行；写前自动备份到 Datas/.backup/'));

    const row = el('div', 'form-row');
    const nameIn = el('input');
    nameIn.placeholder = '字段名（英文，如 maxCount）';
    const typeListId = 'field-type-options';
    const typeW = buildTypeInput(typeListId);
    const titleIn = el('input');
    titleIn.placeholder = '中文说明（可选）';
    const valueIn = el('input');
    valueIn.placeholder = '默认值（可选）';
    row.appendChild(nameIn);
    row.appendChild(typeW.root);
    row.appendChild(titleIn);
    row.appendChild(valueIn);

    const okBtn = el('button', 'btn primary', '确定');
    const cancelBtn = el('button', 'btn', '取消');
    row.appendChild(okBtn);
    row.appendChild(cancelBtn);
    form.appendChild(row);
    form.appendChild(buildTypeDatalist(typeListId));

    cancelBtn.onclick = () => form.classList.remove('open');
    okBtn.onclick = async () => {
      const payload = {
        field: nameIn.value.trim(),
        type: typeW.getType(),
        title: titleIn.value.trim(),
        value: valueIn.value,
      };
      if (!payload.field || !payload.type) {
        setStatus('字段名和类型必填', 'err');
        return;
      }
      okBtn.disabled = true;
      try {
        await api.addTableField(tableName, payload);
        state.schema = await api.getSchema(); // 后端已重扫 Defines，前端同步缓存
        setStatus(`已添加字段 ${payload.field}：Defines XML 与 xlsx 均已更新`, 'ok');
        renderTableEditor(main, tableName, onOpenTable);
      } catch (e) {
        setStatus('添加字段失败：' + e.message, 'err');
        okBtn.disabled = false;
      }
    };
    return form;
  }
}

// 按类型生成单元格编辑器
function buildEditor(col, value, onChange) {
  const pt = col.parsedType || { kind: 'string' };

  if (col.kind === 'enum') {
    const e = getEnum(pt.enumName);
    const sel = el('select');
    const empty = el('option', null, '');
    empty.value = '';
    sel.appendChild(empty);
    let matched = value === '';
    if (e) {
      for (const item of e.items) {
        const opt = el('option', null, item.alias ? `${item.alias} (${item.name})` : item.name);
        opt.value = enumStorageValue(item);
        if (value === item.name || value === item.alias) { opt.selected = true; matched = true; }
        sel.appendChild(opt);
      }
    }
    if (!matched) { // 现有值不在枚举里：原样保留，避免静默丢数据
      const opt = el('option', null, `⚠ ${value}`);
      opt.value = value;
      opt.selected = true;
      sel.appendChild(opt);
    }
    sel.onchange = () => onChange(sel.value);
    return sel;
  }

  if (col.kind === 'bool') {
    const sel = el('select');
    for (const [label, v] of [['', ''], ['true', 'true'], ['false', 'false']]) {
      const opt = el('option', null, label);
      opt.value = v;
      if (String(value).toLowerCase() === v) opt.selected = true;
      sel.appendChild(opt);
    }
    sel.onchange = () => onChange(sel.value);
    return sel;
  }

  const input = el('input');
  input.value = value;
  if (col.kind === 'ref') {
    input.placeholder = `引用 ${pt.ref}`;
  } else if (col.kind === 'list') {
    input.placeholder = `列表，分隔符 ${pt.sep || '默认'}`;
    input.title = `${col.type}\n列表元素之间用「${pt.sep || '默认分隔符'}」分隔`;
  } else if (col.kind === 'bean') {
    input.title = `${col.type}\n复合结构，字段之间用「${pt.sep || '默认分隔符'}」分隔`;
  } else if (col.type) {
    input.title = col.type;
  }
  input.oninput = () => onChange(input.value);
  return input;
}
