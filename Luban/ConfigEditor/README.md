# Luban 配置工具（ConfigEditor）

给策划用的本地网页工具：**浏览项目数据结构** + **直接编辑配置数据**。

## 启动

双击项目根目录（`Luban/`）下的 **`configEditorRun.bat`**：

- 首次运行会自动安装依赖（需要本机已装 [Node.js](https://nodejs.org/)）；
- 自动打开浏览器 `http://localhost:5678`；
- 关闭黑色命令行窗口即停止工具。

## 功能

### 数据结构总览（新人入口）

首页分三类展示，类内按英文名排序：

- **常量表（单例）**：`mode="one"` 的表，如 TbCommonConst——卡片直接展示**当前值**（字段 / 类型 / 值 / 说明），不用进编辑器就能看到实际配置；
- **常规配置**：键值表（map）/ 列表（list）；
- **Defines**：枚举（中文别名 + 程序名）和不被表直接使用的 bean 复合结构，各标注来源定义文件。

表卡片含中文说明、数据文件位置、字段列表（字段名 / 类型 / 说明）；**表间引用**（如 `int → TbItemConfig`）点击直接跳到被引用的表；顶部搜索框可按表名 / 注释 / 字段名过滤。

### 新建表 / 新建枚举

总览页顶部两个按钮，表单提交后自动联动落盘：

- **新建表**：在选中的 Defines 文件里追加 `bean + table` 声明，并在 `Datas/选中目录/` 下新建同名 xlsx（只含 `##var`/`##` 表头；单例表用 `##column` 列模式）——键值表（map）的第一个字段即主键；
- **新建枚举**：在选中的 Defines 文件里追加 `<enum>`，取值写「程序名 + 中文别名」（别名即 Excel 里填的中文）；
- XML 写前自动备份到 `Datas/.backup/`；建表时若 xlsx 创建失败会自动回滚 XML 改动；
- 不支持：新建 Defines 文件、删除/重命名表和枚举（仍需手改）。

### 数据编辑

点左侧导航（按 `Datas/` 子目录分组）或总览页的表名进入编辑：

- 枚举列是**下拉框**（显示中文，写回 Excel 的也是中文，与现有习惯一致）；
- bool 列下拉选 true/false；数字列保存前做格式校验；
- 列表 / 复合结构列悬停可查看分隔符说明（如 `1|2|3`、`100_5`）；
- 支持**添加行 / 删除行**，黄色 = 已修改未保存，红色 = 校验不通过；
- 点「保存」整表写回原 xlsx，**写前自动备份**到 `Datas/.backup/`，改坏了可以找回。

### 修改了 Defines 或直接用 Excel 改了数据后

点顶栏「重新加载」重新扫描即可，不用重启工具。

## 注意事项

- 保存只会改动数据区，`##var`/`##` 表头、样式、其他 sheet 都会保留；
- 本工具只改 xlsx，不会自动跑 `gen.bat`——改完配置要生效仍需照常导出；
- 单例表（`mode="one"`，如 TbCommonConst）按「每行一个字段（字段/类型/值/说明）」展示，可改值、可加字段：
  「＋ 添加字段」会**同步**把 `<var>` 追加到 `Defines/` 对应 bean、并在 xlsx 末尾插入一行（带默认值），
  两处写前都会自动备份到 `Datas/.backup/`；普通行模式表暂不支持在编辑器里加字段；
- 字段类型用「类型构造器」填写：`普通` 直接选基础类型/枚举/bean；`列表` 选元素类型 + 分隔符，
  自动拼成 Luban 语法（如 `(list#sep=|),int`），不用手写；
- 多人同时改同一张表仍是后保存覆盖先保存（本地单机工具，无协作锁）。

## 技术结构（给程序看）

```
ConfigEditor/
  server.js            # Node 内置 http：静态托管 + REST API（/api/schema、/api/table/:name/data、
                       #   POST /api/table/:name/field 加字段、POST /api/table 建表、POST /api/enum 建枚举）
  src/schemaParser.js  # 解析 Defines/*.xml（fast-xml-parser，含中文注释、#ref 引用关系）
  src/typeParser.js    # 解析 Luban 类型串（枚举/引用/(list#sep=..)/bean 复合结构）
  src/definesWriter.js # Defines XML 文本级插入（加字段/建表/建枚举；保注释、缩进与 CRLF；写前备份）
  src/excelIO.js       # xlsx 读写（exceljs；##var/## 行模式 + ##column 列模式；写前备份；新建表 xlsx）
  public/              # 无构建前端（原生 ES Module）：总览（含新建表/枚举）/ 表导航 / 表格编辑器
```
