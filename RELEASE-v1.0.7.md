# Quick Copy Reference v1.0.7 · 浏览器 PDF 选区修复 / Browser PDF selections

## 简体中文

修复浏览器中已选中文字却只复制 PDF 文件名／地址的问题：

- 空的外层文档节点不再阻止读取内嵌 PDF／iframe 的选区。
- 先取得正文选区，再读取地址；浏览器未公开选区时，尝试复制当前选区并恢复原剪贴板。
- 没有选区时不使用旧剪贴板文字；排除地址栏、查找框和密码字段，并检测其他程序的剪贴板更新。
- 所选文字与来源组合在同一个单行 Markdown／HTML 引用中。读取当前鼠标选区，不提取已保存的高亮批注或保留原文排版。

中英文 README 开头现在直接说明“做什么、怎么用、为什么用”，并提供 PDF 引用示例和下载入口。新增 Windows 剪贴板与模拟 PDF 控件测试，覆盖实际复制按键、Unicode／HTML／自定义格式恢复、无选区、复制失败及并发更新。

- 中文完整包：`QuickCopyReference-portable-1.0.7-zh-CN.zip`。
- 英文完整包：`QuickCopyReference-portable-1.0.7-en.zip`。
- 更新：先退出旧托盘程序，将 `QuickCopyReference-update-1.0.7.zip` 中四个 EXE 覆盖到原目录，保留 `reference-settings.json`，再启动 `QuickCopyReference.exe`。
- VS Code 桥接仍为 `vicky-reference-0.1.1.vsix`；旧启动入口保持兼容。

扫描图片、禁止复制的 PDF 或未完成文字识别的页面可能仍只能复制来源。此版本的实际 Chrome／Edge 桌面验证受到本机自动化启动错误阻断；新增回归测试使用受控 PDF 控件，不能代替所有浏览器版本的实际验证。

## English

Fixes references that contained only the PDF filename/address despite a text selection in the browser:

- Empty outer document nodes no longer stop traversal into embedded PDF/iframe selection providers.
- Capture the document selection before reading the address. If accessibility does not expose it, try copying the current selection and restore the previous clipboard.
- Do not use stale clipboard text when no selection exists. Exclude address/find/password fields and detect clipboard updates from other apps.
- Selected text and source stay in the same single-line Markdown/HTML reference. This captures the current selection, not saved highlight annotations or source formatting.

Both README introductions now explain what it does, how to use it, and why, with a PDF reference example and download link. New Windows clipboard and simulated-PDF-control tests cover real copy keystrokes, Unicode/HTML/custom-format restoration, no selection, failed copying, and concurrent updates.

- English full package: `QuickCopyReference-portable-1.0.7-en.zip`.
- Simplified Chinese full package: `QuickCopyReference-portable-1.0.7-zh-CN.zip`.
- Upgrade: exit the old tray app, replace the four EXEs from `QuickCopyReference-update-1.0.7.zip`, retain `reference-settings.json`, and restart `QuickCopyReference.exe`.
- VS Code bridge remains `vicky-reference-0.1.1.vsix`; legacy entry points remain compatible.

Scanned images, copy-restricted PDFs, or pages awaiting text recognition may still produce only the source. Live Chrome/Edge desktop validation for this version was blocked by local automation launch errors. Regression tests use a controlled PDF-like control and do not replace validation across actual browser versions.

## Checks and privacy / 检查与隐私

Published from GitHub Actions artifacts only after the Windows build, bilingual settings, shortcut, format, adapter, clipboard, and package checks pass. Fresh settings and a file allowlist exclude private documents, personal preferences, runtime state, logs, clipboard contents, and debug symbols. SHA256 hashes are attached. / 仅在 GitHub Actions Windows 构建、双语设置、快捷键、格式、来源匹配、剪贴板及打包检查通过后发布，使用首次启动配置和明确文件清单，排除私人文档、个人偏好、运行状态、日志、剪贴板及调试符号，附 SHA256 校验值。

Source available under PolyForm Noncommercial 1.0.0. / 沿用 PolyForm Noncommercial 1.0.0 非商业源码许可。
