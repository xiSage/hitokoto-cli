# hitokoto-cli

从 [hitokoto.cn](https://hitokoto.cn) 获取「一言」的命令行工具。

## 安装

### 下载预编译二进制

从 [Releases](https://github.com/xiSage/hitokoto-cli/releases) 下载对应平台的可执行文件。

### 从源码构建

需要 [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。

```bash
# Debug 构建
dotnet build hitokoto-cli/hitokoto-cli.csproj

# Release 构建（Native AOT，生成独立可执行文件）
dotnet publish hitokoto-cli/hitokoto-cli.csproj -c Release
```

产物位于 `hitokoto-cli/bin/Release/net10.0/publish/hitokoto.exe`。

## 使用

### 获取一言

```bash
hitokoto                  # 获取随机一言（默认 full 格式）
hitokoto --format text    # 纯文本输出
hitokoto --format json    # JSON 输出
hitokoto -c a -c b        # 限定分类：动画 + 漫画
hitokoto --min-length 10 --max-length 50   # 限制句子长度
hitokoto --raw json       # 直接透传 API 原始 JSON 响应
```

### 分类代码

| 代码 | 分类   |
|------|--------|
| a    | 动画   |
| b    | 漫画   |
| c    | 游戏   |
| d    | 文学   |
| e    | 原创   |
| f    | 来自网络 |
| g    | 其他   |
| h    | 影视   |
| i    | 诗词   |
| j    | 网易云 |
| k    | 哲学   |
| l    | 抖机灵 |

### 管理配置

```bash
hitokoto config list              # 列出所有配置
hitokoto config get endpoint      # 查看 API 端点
hitokoto config set categories a,b,c   # 设置默认分类
hitokoto config set output_format text # 设置默认输出格式
hitokoto config unset categories  # 恢复某项为默认值
hitokoto config reset             # 重置所有配置
hitokoto config path              # 显示配置文件路径
```

### 可配置项

| 键               | 类型         | 说明             |
|------------------|-------------|-----------------|
| `endpoint`       | 字符串       | API 端点 URL     |
| `categories`     | a-l 逗号分隔 | 默认分类过滤     |
| `min_length`     | 整数         | 最小句子长度     |
| `max_length`     | 整数         | 最大句子长度     |
| `output_format`  | text / json / full | 默认输出格式 |
| `timeout_seconds`| 整数         | 请求超时（秒）   |
| `show_source`    | true / false | 是否显示来源     |
| `show_link`      | true / false | 是否显示链接     |

### 参数优先级

CLI 参数 > 配置文件 > 内置默认值

使用 `--no-config` 可忽略配置文件，仅使用 CLI 参数和内置默认值。

### 退出码

| 码 | 含义           |
|----|---------------|
| 0  | 成功           |
| 1  | 一般错误       |
| 2  | 参数 / 输入错误 |
| 3  | I/O 权限错误   |

## 特性

- **多种输出格式**：`full`（带装饰面板）、`text`（纯文本）、`json`
- **Raw 模式**：直接透传 API 原始响应，适合管道处理
- **配置文件**：持久化偏好设置，配置文件位于 `%APPDATA%/hitokoto-cli/config.json`
- **stdout/stderr 分离**：错误和提示写入 stderr，确保管道使用不受干扰
- **Native AOT 编译**：发布产物为独立原生可执行文件，无需 .NET 运行时

## 依赖

- [Spectre.Console.Cli](https://github.com/spectreconsole/spectre.console) — 命令行界面框架
- [hitokoto.cn API](https://developer.hitokoto.cn/) — 一言数据接口

## 许可

MIT
