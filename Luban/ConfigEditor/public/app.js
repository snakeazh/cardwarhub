import { state, setStatus } from './store.js';
import * as api from './api.js';
import { renderSidebar } from './views/tableList.js';
import { renderOverview } from './views/overview.js';
import { renderTableEditor } from './views/tableEditor.js';

const main = document.getElementById('main');
const searchInput = document.getElementById('global-search');
const overviewBtn = document.getElementById('nav-overview');
const reloadBtn = document.getElementById('nav-reload');

function openTable(name) {
  state.currentTable = name;
  overviewBtn.classList.remove('active');
  renderSidebar(openTable, searchInput.value);
  renderTableEditor(main, name, openTable);
}

function showOverview() {
  state.currentTable = null;
  overviewBtn.classList.add('active');
  renderSidebar(openTable, searchInput.value);
  renderOverview(main, openTable, searchInput.value);
}

overviewBtn.onclick = showOverview;
reloadBtn.onclick = async () => {
  try {
    await api.reloadSchema();
    state.schema = await api.getSchema();
    setStatus('已重新扫描配置', 'ok');
    if (state.currentTable) openTable(state.currentTable);
    else showOverview();
  } catch (e) {
    setStatus('重新加载失败：' + e.message, 'err');
  }
};

let searchTimer = null;
searchInput.oninput = () => {
  clearTimeout(searchTimer);
  searchTimer = setTimeout(() => {
    if (state.currentTable) renderSidebar(openTable, searchInput.value);
    else showOverview();
  }, 200);
};

(async function boot() {
  try {
    state.schema = await api.getSchema();
  } catch (e) {
    main.innerHTML = '';
    const tip = document.createElement('div');
    tip.className = 'empty-tip';
    tip.textContent = '无法连接后端服务：' + e.message;
    main.appendChild(tip);
    return;
  }
  showOverview();
})();
