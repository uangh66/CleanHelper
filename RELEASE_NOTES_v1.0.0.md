# CleanHelper v1.0.0 Release Notes

## 发布说明

这是 CleanHelper 的首个公开发布版本。`CleanHelper.exe` 为单文件独立可执行文件，无需安装 .NET 运行时，下载即可运行。

## 下载

| 文件 | 大小 | 说明 |
|------|------|------|
| `CleanHelper.exe` | ~155 MB | 单文件、自包含、免安装 |

> 📦 发布包从 GitHub Releases 页面下载，不在 Git 仓库中。

## 系统要求

| 项目 | 要求 |
|------|------|
| 操作系统 | Windows 10 / 11 (x64) |
| .NET 运行时 | 不需要（自包含发布） |
| 磁盘空间 | ~155 MB（exe 自身） |

## 功能概览

### 🧹 临时文件清理
- 一键扫描 `%TEMP%` 目录
- 按修改时间自动风险分级（>7天低风险，≤7天中风险）
- 跳过 ReparsePoint，路径安全校验
- 区分"成功处理大小"与"真实释放空间"
- 导出 UTF-8 清理报告

### 📊 空间体检
- 扫描下载、桌面、文档、图片、视频、音乐 6 个用户目录
- 筛选阈值：50/100/500/1000 MB
- 按目录分类汇总 + 建议说明
- 仅读取元数据，不删除文件

### 💬 聊天缓存分析
- 统计微信、QQ、企业微信、钉钉缓存目录占用
- 12 个候选目录自动探测
- 仅统计大小和文件数，不读取聊天内容
- 按软件汇总排行

### 📝 报告系统
- 三种报告：清理报告 / 空间体检报告 / 聊天缓存报告
- UTF-8 纯文本格式，仅含文件元数据

## 安全承诺

- 🔌 不联网，零网络请求
- 📤 不上传任何数据
- 🚫 不常驻，关闭即退出
- ⚙️ 不安装 Windows 服务
- 📁 不碰系统目录和注册表
- 👁️ 不窥探隐私

## 技术栈

| 类别 | 技术 |
|------|------|
| 语言 | C# 12 |
| 框架 | .NET 8.0 |
| UI | WPF |
| 架构 | MVVM |
| 发布方式 | 单文件自包含 (PublishSingleFile + SelfContained) |

## 构建方式

```bash
git clone https://github.com/[your-username]/CleanHelper.git
cd CleanHelper
dotnet run
```

发布单文件：
```bash
dotnet publish -c Release -o publish
# 输出: publish/CleanHelper.exe
```

## 开发声明

本项目由 **黄锐 (Huang Rui)** 独立开发并开源。开发过程中使用了 Claude、DeepSeek 等 AI 工具辅助需求拆解、代码生成、调试与文档整理。所有 AI 生成代码均经过人工审查与测试验证。
