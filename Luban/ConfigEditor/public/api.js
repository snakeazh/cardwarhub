async function request(url, options) {
  const res = await fetch(url, options);
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(data.error || `HTTP ${res.status}`);
  return data;
}

export function getSchema() {
  return request('/api/schema');
}

export function getTableData(name) {
  return request(`/api/table/${encodeURIComponent(name)}/data`);
}

export function saveTableData(name, fields, rows) {
  return request(`/api/table/${encodeURIComponent(name)}/data`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ fields, rows }),
  });
}

export function reloadSchema() {
  return request('/api/reload', { method: 'POST' });
}

// 单例表加字段：后端同步改 Defines XML + 在 xlsx 末尾插行
export function addTableField(name, payload) {
  return request(`/api/table/${encodeURIComponent(name)}/field`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
}

// 新建枚举：只改 Defines XML
export function createEnum(payload) {
  return request('/api/enum', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
}

// 新建表：Defines XML 加 bean+table，并在 Datas 下建数据 xlsx
export function createTable(payload) {
  return request('/api/table', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
}
