# Quick Copy Reference v1.0.6 · 快速复制引用

## 简体中文

项目正式更名为 **Quick Copy Reference / 快速复制引用**，仓库为 `logicalwang/quick-copy-reference`。用键盘快捷键、鼠标侧键或脚本，从当前聚焦的受支持 Windows 应用复制带出处、选中文字和位置的引用链接。Stream Deck 是可选入口。

- 中文完整包：`QuickCopyReference-portable-1.0.6-zh-CN.zip`。
- 英文完整包：`QuickCopyReference-portable-1.0.6-en.zip`。
- 完整解压后运行 `QuickCopyReference.exe`，首次自定义快捷键和语言。
- 更新：退出旧托盘程序，将 `QuickCopyReference-update-1.0.6.zip` 中四个 EXE 覆盖到原目录，保留 `reference-settings.json`，再运行主程序。
- 保留 `VickyReference.exe` 兼容入口、原有本地配置／状态目录和启动链接；旧鼠标映射、脚本可继续使用。
- VS Code 桥接更新为 **Quick Copy Reference Bridge**；随包提供 `vicky-reference-0.1.1.vsix`，扩展 ID 和内部命令不变。

中英文首页重新介绍用途、复制示例、快速开始、应用支持范围和各种入口。窗口、托盘提示及下载包采用新名称。文件读取行为沿用 v1.0.5，PowerPoint 放映／演讲者视图和远程 URI 仍未适配。需要 Windows 10／11 与 .NET Framework 4.x。

## English

The project is now **Quick Copy Reference**, hosted at `logicalwang/quick-copy-reference`. Use a keyboard shortcut, mouse side button, or script to copy source links with available selected text and locations from the focused supported Windows app. Stream Deck is an optional entry point.

- English full package: `QuickCopyReference-portable-1.0.6-en.zip`.
- Simplified Chinese full package: `QuickCopyReference-portable-1.0.6-zh-CN.zip`.
- Extract the whole folder and run `QuickCopyReference.exe` to choose your shortcut and language.
- Upgrade: exit the tray app, replace the four EXEs from `QuickCopyReference-update-1.0.6.zip`, keep `reference-settings.json`, and restart the main app.
- `VickyReference.exe`, existing configuration/state directories, and startup links remain compatible with old mouse mappings and scripts.
- The VS Code bridge is now **Quick Copy Reference Bridge**, bundled as `vicky-reference-0.1.1.vsix`. Extension ID and internal commands are preserved.

Both READMEs now lead with the purpose, examples, quick start, source-app support, and trigger options. Dialogs, tray notifications, and downloads use the new name. Capture behavior remains as in v1.0.5; PowerPoint slide shows/presenter view and remote URIs remain unsupported. Windows 10/11 with .NET Framework 4.x is required.

## Verification, privacy, and license / 检查、隐私与许可

Published only from GitHub Actions artifacts after Windows build, bilingual UI/shortcut tests, reference-format tests, adapter tests, package checks, and hash verification pass. Hosted CI does not run real Office/browser apps. / 仅在 GitHub Actions Windows 构建、双语界面与快捷键测试、引用格式与来源匹配测试、打包及校验通过后发布；云端不运行真实 Office／浏览器。

Fresh first-launch settings and an explicit file allowlist exclude private documents, personal settings, logs, clipboard content, runtime state, and debug symbols. SHA256 hashes are attached. / 首次启动配置和明确文件清单排除私人文档、个人配置、日志、剪贴板、运行状态及调试符号，附 SHA256 校验值。

Source available under PolyForm Noncommercial 1.0.0; commercial use needs separate permission. / 沿用非商业源码许可，商业使用需另行许可。
