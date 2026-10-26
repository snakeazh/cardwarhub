import { state, tableComment, el } from '../store.js';

// 左侧表导航：按 Datas 子目录分组，可过滤
export function renderSidebar(onOpenTable, filter = '') {
  const bar = document.getElementById('sidebar');
  bar.innerHTML = '';
  const kw = filter.trim().toLowerCase();

  const groups = {};
  for (const t of Object.values(state.schema.tables)) {
    const dir = (t.input.includes('/') ? t.input.split('/')[0] : '根目录');
    (groups[dir] = groups[dir] || []).push(t);
  }

  for (const dir of Object.keys(groups).sort()) {
    let tables = groups[dir].sort((a, b) => a.name.localeCompare(b.name));
    if (kw) {
      tables = tables.filter(t => matchTable(t, kw));
    }
    if (!tables.length) continue;

    const g = el('div', 'side-group');
    g.appendChild(el('div', 'group-name', `${dir} (${tables.length})`));
    for (const t of tables) {
      const item = el('button', 'side-item' + (state.currentTable === t.name ? ' active' : ''));
      const comment = tableComment(t);
      item.appendChild(document.createTextNode(t.name.replace(/^Tb/, '')));
      if (comment) {
        const c = el('span', 'comment', ` ${comment}`);
        item.appendChild(c);
      }
      item.title = `${t.name}\n${t.input}${comment ? '\n' + comment : ''}`;
      item.onclick = () => onOpenTable(t.name);
      g.appendChild(item);
    }
    bar.appendChild(g);
  }
  if (!bar.children.length) {
    bar.appendChild(el('div', 'empty-tip', '没有匹配的表'));
  }
}

function matchTable(t, kw) {
  if (t.name.toLowerCase().includes(kw)) return true;
  if (tableComment(t).toLowerCase().includes(kw)) return true;
  const bean = state.schema.beans[t.value];
  if (bean) {
    for (const f of bean.fields) {
      if (f.name.toLowerCase().includes(kw) || (f.comment || '').toLowerCase().includes(kw)) return true;
    }
  }
  return false;
}
