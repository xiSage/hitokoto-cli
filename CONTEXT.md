# CONTEXT — hitokoto-cli 领域词汇

本文件的术语用于给接缝命名、写测试名、评审时引用领域概念。改代码前先读它；发现缺术语时补进来（懒创建）。

## 领域术语

### 一言 (Hitokoto)
从 `https://hitokoto.cn` 获取的一句话。CLI 的核心产出，支持三种输出形态：`text`、`json`、`full`。

### 获取 (Fetch)
「拿一句一言并输出」这条执行流。入口是默认命令（无子命令时的 `hitokoto`），经过「解析 CLI 覆盖 → 合并配置 → 请求 API → 渲染输出」。

### 配置 (Config)
配置的领域：`config` 子命令分支 + 配置文件 + 合并规则。唯一拥有它的是 `ConfigModule`（配置模块）——命令与默认命令只跨这一条接缝。

### 配置键 (ConfigKey)
配置文件中一个可配置的键（`endpoint`、`categories`、`min_length`、`max_length`、`output_format`、`timeout_seconds`、`show_source`、`show_link`）。每个键的全部知识（解析、格式化、合并策略、默认值、读写清除）集中在一行 `ConfigKeyInfo` 里——新增键 = 一行 + 它的 `AppConfig` 属性 + CLI 选项。

### 合并优先级 (Merge Precedence)
有效参数的取值规则：**CLI 覆盖 > 配置文件 > 内置回退**。两种键类：
- **API 面向参数**（`categories`/`min_length`/`max_length`）：全部未设置时为 `null`，即从请求中省略该参数，交给 API 选择。
- **客户端面向参数**（`endpoint`/`output_format`/`timeout_seconds`/`show_source`/`show_link`）：有内置回退值。`show_source`/`show_link` 的合并回退是 `true`，但文件默认保持 `null`（`config list` 显示「未设置」）。

### CLI 覆盖 (CliOverrides)
命令行上给出的获取选项（与 `FetchSettings` 一一对应，但不依赖 Spectre 类型），由 `DefaultCommand` 映射后交给 `ConfigModule.Resolve`。
