# AGENTS.md

本文件面向 AI 编码代理，介绍本仓库的结构、工作流与约定。假定读者对本项目一无所知。

## 项目概览

这是一个 **Luban 游戏配置仓库**（不是独立的应用程序源码）。核心内容是：

- 用 **Luban**（.NET 配置导出工具，二进制位于 `Tools/Luban/`）把策划填写的 **Excel 配置数据** + **XML 结构定义** 导出为：
  - **客户端**：C# 代码 + JSON 数据（目标工程是 Unity 项目 `../FusionContinent`，代码生成器 `cs-simple-json`）；
  - **服务端**：Go 代码 + JSON 数据（目标工程是 `../Server`，Go module `ruoyi-go/app/service/luban`，代码生成器 `go-json`）。
- 附带一个自研的本地网页工具 **ConfigEditor**（`ConfigEditor/`，Node.js），供策划浏览配置结构并直接编辑 xlsx 数据。

本仓库本身**没有自动化测试、没有 CI、没有打包发布流程**——产出物通过 `gen.bat` 直接写入相邻的客户端/服务端工程目录。

## 目录结构

```
Defines/          # Luban schema：*.xml，声明 module/bean/enum/table，按业务域分文件（Buff.xml、Equip.xml 等）
                  #   Builtin.xml 是内建类型（int2/float2/float3 等映射到 Unity.Mathematics）
Datas/            # 配置数据 xlsx，按子目录分组：Common/、GamePlay/（绝大部分表）、Localization/、RedDot/
  __tables__.xlsx # 在 Excel 里定义的 table（luban.conf 中以 schemaFiles 引入）
  __beans__.xlsx  # 在 Excel 里定义的 bean
  __enums__.xlsx  # 在 Excel 里定义的 enum
  .backup/        # ConfigEditor 写文件前的自动备份目录
Templates/        # 自定义 Scriban 模板（cs-bin、go-bin、go-json 三套），通过 --customTemplateDir 生效
Tools/Luban/      # Luban 工具本体（Luban.dll 及各语言插件 dll）
ConfigEditor/     # 本地配置编辑工具（Node.js，见下）
luban.conf        # Luban 主配置：groups、schemaFiles、dataDir、targets
gen.bat           # 一键导出客户端 + 服务端配置
configEditorRun.bat  # 启动 ConfigEditor 网页工具
```

注意：`gen.bat` 的输出路径指向**仓库外**的兄弟目录（`../FusionContinent`、`../Config`、`../Server`），导出后还用 `xcopy` 把 JSON 拷进 Unity 资源目录和服务端数据目录。

## 构建 / 导出 / 运行命令

- **导出配置**：在项目根目录运行 `gen.bat`（Windows）。需要本机装有 .NET 运行时（`dotnet Tools/Luban/Luban.dll ...`）。它依次导出 client（`-c cs-simple-json -d json`）和 server（`-c go-json -d json`），时区固定 `China Standard Time`，多语言文本源为 `Datas/Common/TextInfo.xlsx`。
- **启动编辑工具**：双击根目录 `configEditorRun.bat`（首次运行自动 `npm install`，需要 Node.js），然后浏览器访问 `http://localhost:5678`。也可手动：`cd ConfigEditor && npm install && node server.js [端口]`（默认 5678）。
- **没有测试命令**。验证配置改动的方式就是跑 `gen.bat` 看 Luban 是否报错（Luban 会做类型、引用、校验器检查）。

## Luban 配置约定（改 Defines / Datas 时必须遵守）

- **groups**：`luban.conf` 定义了 `c`（客户端）、`s`（服务端）、`e` 三个组，字段可用 `group="c"` / `group="s"` 限定导出目标；targets 为 `server`（s）、`client`（c）、`all`（c+s,e）。
- **table 的 mode**：`map`（键值表，第一个字段是主键，默认）、`list`（列表）、`one`（单例常量表，如 TbCommonConst，xlsx 用 `##column` 列模式：每行一个字段「字段/类型/值/说明」）。普通表用 `##var` + `##` 表头的行模式。
- **table 与数据的对应**：`<table name="TbXxxConfig" value="XxxConfig" input="Common/XxxConfig.xlsx"/>`——表名 `Tb` 前缀，input 相对 `Datas/`。
- **枚举**：XML 中写「程序名 + 中文别名」（`<var name="Epic" alias="史诗"/>`），**Excel 数据里填中文别名**，导出时 Luban 负责映射。ConfigEditor 的下拉框也遵守这一习惯。
- **类型串**：支持 `text`、`string`、`int` 等基础类型；列表用 `(list#sep=|),int` 这类构造器语法；复合 bean 在单元格内用其 `sep` 分隔（如 `RewardAndNum` 的 `100_5`）；跨表引用用 `#ref`（如 `int#ref=TbItemConfig`）。
- **Defines XML**：含中文注释，文本级格式（缩进、CRLF）需保留；根节点是 `<module name="">`。

## ConfigEditor 技术结构（改工具代码时参考）

纯 Node.js（仅用 `exceljs`、`fast-xml-parser` 两个依赖），无构建步骤：

```
ConfigEditor/
  server.js            # Node 内置 http：静态托管 public/ + REST API
                       #   GET /api/schema、POST /api/reload、/api/table/:name/data、
                       #   POST /api/table/:name/field（单例表加字段，同步改 XML+xlsx）、
                       #   POST /api/table（新建表）、POST /api/enum（新建枚举）
  src/schemaParser.js  # 解析 Defines/*.xml（含中文注释、#ref 引用关系）
  src/typeParser.js    # 解析 Luban 类型串（枚举/引用/(list#sep=..)/bean 复合结构）
  src/definesWriter.js # Defines XML 文本级插入（保注释、缩进与 CRLF；写前备份）
  src/excelIO.js       # xlsx 读写（##var/## 行模式 + ##column 列模式；新建表 xlsx）
  public/              # 无构建前端（原生 ES Module）：overview / tableList / tableEditor / typeInput
```

重要行为约定：

- **所有写操作前自动备份**到 `Datas/.backup/`（XML 和 xlsx 都是）；建表时若 xlsx 创建失败会回滚 XML 改动。
- 保存只改数据区，保留 `##var`/`##` 表头、样式和其他 sheet。
- 工具**只改 xlsx/XML，不会自动跑 `gen.bat`**——改完要生效仍需手动导出。
- 不支持：新建 Defines 文件、删除/重命名表和枚举（需手改 XML）。
- 多人同时改同一张表是后保存覆盖先保存（本地单机工具，无协作锁）。

## 测试与验证

- 仓库无任何自动化测试与 lint 配置。
- 改 Defines/Datas 后：运行 `gen.bat` 确认 Luban 导出无报错。
- 改 ConfigEditor 后：`node server.js` 启动后手动在浏览器验证（总览页、表编辑、保存、新建表/枚举流程），并确认 `Datas/.backup/` 备份正常生成。

## 安全与操作注意事项

- ConfigEditor 是无鉴权的**本地单机** HTTP 服务，不要把它暴露到局域网/公网。
- 改动 xlsx 与 Defines XML 前依赖 `Datas/.backup/` 备份机制回滚，不要删除该目录。
- `gen.bat` 会写入并覆盖仓库外兄弟工程（`../FusionContinent`、`../Server`、`../Config`）里的生成文件，属正常行为；不要手工编辑这些生成产物，应改本仓库的源数据后重新导出。
- xlsx 是二进制文件，无法用文本 diff 审查，修改时务必通过 ConfigEditor 或 Excel 谨慎操作。
