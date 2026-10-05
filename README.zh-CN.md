# Quick Copy Reference · 快速复制引用

**简体中文** | [English](README.en.md)

[![Windows 构建与测试](https://github.com/logicalwang/quick-copy-reference/actions/workflows/build.yml/badge.svg)](https://github.com/logicalwang/quick-copy-reference/actions/workflows/build.yml)

**按一下快捷键，把正在看的文件、网页和选中文字，复制成带出处的引用链接。**

适用于 **Windows 10／11**，通过全局快捷键、鼠标侧键或脚本调用，从当前聚焦的受支持应用取得来源。复制结果可粘贴到 Codex、Markdown 文档、笔记或其他接收文本／链接的软件中。提供简体中文和英文界面，运行不需要 Stream Deck。

```text
选中文字／文件 → 按快捷键或鼠标侧键 → 粘贴带出处的引用
```

## 30 秒开始使用

1. 从 [Releases](https://github.com/logicalwang/quick-copy-reference/releases) 下载中文或英文完整便携包，解压到长期使用的位置。
2. 运行 `QuickCopyReference.exe`，首次设置自己的快捷键，例如 **Ctrl+Alt+Shift+R**。
3. 在浏览器、Office、文件管理器或编辑器中，聚焦要引用的内容；需要关注点时选中文字，需要文件引用时选中文件。
4. 按快捷键，再到目标软件粘贴。鼠标侧键可映射为同一快捷键，也可直接调用 `CopyReference.exe`。

运行只需要 Windows 的 .NET Framework 4.x。可配置开机启动，默认按需读取，结果留在本机剪贴板。

## 复制出来是什么

来源、可读取的选中文字和位置组合为 **一个单行 Markdown 链接**，同时提供 HTML 链接。多选文件的引用也保持在同一行。

```text
[网页标题: 需要关注的这段文字](https://example.com/article)
[757–770 (line 757)](C:/Example/notes.tex:757)
[deck.pptx: 幻灯片 2: 所选文字](C:/Example/deck.pptx)
[notes.md](C:/Example/notes.md) [paper.pdf](C:/Example/paper.pdf)
```

## 能从哪些软件取得来源

全局快捷键可随时触发；自动读取来源目前支持下表中的应用，并不是每个应用都能公开文件路径和选区。复制出的引用可以粘贴到其他软件。

| 聚焦软件 | 引用内容 |
| --- | --- |
| 资源管理器 | 所选文件／文件夹，支持多选 |
| Chrome、Edge、Brave、Firefox | 网页地址及可读取的所选文字 |
| Edge 本地 PDF | PDF 路径及可读取的所选文字 |
| Word、Excel、PowerPoint | 已保存文件、可读取选区、单元格或幻灯片位置 |
| 记事本、MarkPad | 已保存文件及可读取选区 |
| VS Code | 本地已保存文件和选区行号，需桥接扩展 |
| Notepad++、Typora、MarkText、Cursor | 取决于标题或标签是否提供完整路径 |

便携版支持 PowerPoint 编辑及幻灯片浏览视图，放映／演讲者视图暂未适配。未保存文档、远程 URI、受保护视图、高权限应用及不兼容的软件版本可能无法读取；来源不确定时拒绝猜测。文件名和选中文字不翻译。只在触发时读取，不上传文档数据；可能短暂聚焦浏览器地址栏，或打开并取消记事本另存为，以识别来源。接收应用决定粘贴 HTML 还是 Markdown。

## 键盘、鼠标和脚本入口

| 入口 | 用法 |
| --- | --- |
| 键盘 | 运行 `QuickCopyReference.exe`，自定义全局快捷键 |
| 鼠标侧键 | 映射到同一快捷键，或运行 `CopyReference.exe` |
| 脚本／启动器 | 调用 `QuickCopyReference.exe --copy`，或运行 `CopyReference.exe` |
| Stream Deck | 可将按键映射到快捷键或运行 `CopyReference.exe` |

触发时保持来源应用聚焦；脚本需要在同一交互式 Windows 桌面运行。可在托盘菜单修改快捷键、切换语言或设置开机启动。程序会提示快捷键冲突，并禁止占用 Ctrl+C／Ctrl+X／Ctrl+V。

## 下载、语言与更新

从 [Releases](https://github.com/logicalwang/quick-copy-reference/releases) 下载：

- `QuickCopyReference-portable-1.0.6-zh-CN.zip`：默认简体中文。
- `QuickCopyReference-portable-1.0.6-en.zip`：默认英文。
- `QuickCopyReference-update-1.0.6.zip`：覆盖四个 EXE，保留原配置。

完整解压后运行 `QuickCopyReference.exe`，首次选择语言和自定义快捷键。两个下载包使用同一个双语程序，可在设置窗口或 **托盘菜单 → 语言** 立即切换。鼠标侧键可映射为组合键，或直接运行 `CopyReference.exe`。可选择开机启动、成功提示。需要 Windows 10／11 与 .NET Framework 4.x；运行便携版不需要 Stream Deck、Node.js 或 npm。

更新时先退出旧托盘程序，用更新包覆盖四个 EXE，保留 `reference-settings.json`，再启动。VS Code 需安装便携版对应的 `vicky-reference-0.1.1.vsix`。设置、语言、隐私、限制和构建详情见 [中文便携指南](portable/README.zh-CN.md)／[English guide](portable/README.en.md)。

旧名 `VickyReference.exe` 保留为兼容入口，已有鼠标映射、脚本和开机启动可继续使用。

## 构建与自动检查

在 Windows 安装 Node.js、npm、Python 3，并具备 .NET Framework 编译器后：

```powershell
npm ci
npm run build
npm run typecheck
npm test
npm run validate
npm run pack -- --force
& .\portable\source\build.ps1
node --test .\portable\tests\portable.test.mjs
& .\scripts\test-portable-adapters.ps1
& .\scripts\test-portable-dialog.ps1
python .\scripts\package-portable.py
python .\scripts\verify-portable-package.py
```

[GitHub Actions](https://github.com/logicalwang/quick-copy-reference/actions/workflows/build.yml) 在 main、PR、手动触发和版本标签上执行 Windows 检查。通过后保存构建产物；与源码版本匹配的标签会在所有检查通过后发布中英文便携包。CI 检查编译、引用格式、快捷键保护、双语设置窗口、旧配置兼容和打包；不安装 Office，也不验证真实 Office／浏览器读取，仍需桌面实测。

发布打包使用明确的文件清单与首次启动配置，排除私人文档、日志、剪贴板、运行状态和调试符号。下载校验值在 `SHA256SUMS.txt`。

## 许可

沿用 [PolyForm Noncommercial 1.0.0](LICENSE)，允许非商业使用、修改和分发，商业使用需另行许可。因含非商业限制，属于源码可见，并非 OSI 定义的开源。第三方依赖保留各自许可。

反馈请附软件名称／版本、触发方式和错误详情，避免提供敏感路径、正文和选区。

## 可选：原 Stream Deck 插件

需要 Windows 10／11、Stream Deck 7.1+。从 [v0.1.0](https://github.com/logicalwang/quick-copy-reference/releases/tag/v0.1.0) 下载 `dev.vicky.copyreference.streamDeckPlugin` 安装，再把复制引用动作拖到按键。

VS Code 使用 `powershell -ExecutionPolicy Bypass -File .\scripts\install-vscode-bridge.ps1` 安装原插件的 `vscode-reference` 桥接，然后完全重启 VS Code。它与便携桥接不同；两者都内部使用 Ctrl+Alt+Shift+F12，仅支持本地文件。
