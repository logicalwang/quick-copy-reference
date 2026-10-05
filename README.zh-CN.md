# 复制引用 / Copy Reference

**简体中文** | [English](README.en.md)

[![Windows 构建与测试](https://github.com/logicalwang/stream-deck-copy-reference/actions/workflows/build.yml/badge.svg)](https://github.com/logicalwang/stream-deck-copy-reference/actions/workflows/build.yml)

Windows 工具：读取当前聚焦的文件、文档或网页，把来源及可用的选中文字、位置写成 **单行 Markdown 引用** 并复制。便携版同时提供 HTML 剪贴板链接，可粘贴给 Codex 或普通 Markdown 编辑器。

提供键盘／鼠标便携版和原 Stream Deck 插件。**v1.0.5 发布双语便携版**；未改动的 Stream Deck 插件仍在 [v0.1.0](https://github.com/logicalwang/stream-deck-copy-reference/releases/tag/v0.1.0)。

## 便携版：简体中文与英文

从 [Releases](https://github.com/logicalwang/stream-deck-copy-reference/releases) 下载：

- `VickyReference-portable-1.0.5-zh-CN.zip`：默认简体中文。
- `VickyReference-portable-1.0.5-en.zip`：默认英文。
- `VickyReference-update-1.0.5.zip`：覆盖三个 EXE，保留原配置。

完整解压后运行 `VickyReference.exe`，首次选择语言和自定义快捷键。两个下载包使用同一个双语程序，可在设置窗口或 **托盘菜单 → 语言** 立即切换。鼠标侧键可映射为组合键，或直接运行 `CopyReference.exe`。可选择开机启动、成功提示。需要 Windows 10／11 与 .NET Framework 4.x；运行便携版不需要 Stream Deck、Node.js 或 npm。

更新时先退出旧托盘程序，用更新包覆盖三个 EXE，保留 `reference-settings.json`，再启动。VS Code 需安装便携版对应的 `vicky-reference-0.1.0.vsix`。设置、语言、隐私、限制和构建详情见 [中文便携指南](portable/README.zh-CN.md)／[English guide](portable/README.en.md)。

```text
[757–770 (line 757)](C:/Example/notes.tex:757)
[deck.pptx: 幻灯片 2: 所选文字](C:/Example/deck.pptx)
[notes.md](C:/Example/notes.md) [paper.pdf](C:/Example/paper.pdf)
```

## 支持范围

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

## 原 Stream Deck 插件

需要 Windows 10／11、Stream Deck 7.1+。从 [v0.1.0](https://github.com/logicalwang/stream-deck-copy-reference/releases/tag/v0.1.0) 下载 `dev.vicky.copyreference.streamDeckPlugin` 安装，再把复制引用动作拖到按键。

VS Code 使用 `powershell -ExecutionPolicy Bypass -File .\scripts\install-vscode-bridge.ps1` 安装原插件的 `vscode-reference` 桥接，然后完全重启 VS Code。它与便携桥接不同；两者都内部使用 Ctrl+Alt+Shift+F12，仅支持本地文件。

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

[GitHub Actions](https://github.com/logicalwang/stream-deck-copy-reference/actions/workflows/build.yml) 在 main、PR、手动触发和版本标签上执行 Windows 检查。通过后保存构建产物；与源码版本匹配的标签会在所有检查通过后发布中英文便携包。CI 检查编译、引用格式、快捷键保护、双语设置窗口、旧配置兼容和打包；不安装 Office，也不验证真实 Office／浏览器读取，仍需桌面实测。

发布打包使用明确的文件清单与首次启动配置，排除私人文档、日志、剪贴板、运行状态和调试符号。下载校验值在 `SHA256SUMS.txt`。

## 许可

沿用 [PolyForm Noncommercial 1.0.0](LICENSE)，允许非商业使用、修改和分发，商业使用需另行许可。因含非商业限制，属于源码可见，并非 OSI 定义的开源。第三方依赖保留各自许可。

反馈请附软件名称／版本、触发方式和错误详情，避免提供敏感路径、正文和选区。
