# Quick Copy Reference v1.0.8 · 可读文件路径 / Readable file paths

## 简体中文

修复本地文件路径充满 `%20`、难以阅读的问题。路径里的空格、中文及括号保持可读，需要时使用且只使用一对 Markdown 目标尖括号：

```text
[notes.md](<C:/Example/Research Notes/notes.md>)
[757–770 (line 757)](<C:/Example/Research Notes/notes.tex:757>)
```

- 行号仍位于链接目标内，多文件引用仍保持单行。
- 文件名中真正的百分号、井号和实体敏感字符保留必要转义，避免指向其他文件；网页已有编码和签名参数保持原样。
- 新增格式回归测试；本机已验证 Markdown 解析、真实文件路径往返及 Unicode／HTML 剪贴板，并由用户实体按键确认空格恢复。
- 中英文 README 和包内指南补充了可读路径格式说明。

下载中文或英文完整包；已有用户先退出托盘程序，将 `QuickCopyReference-update-1.0.8.zip` 中四个 EXE 覆盖到原目录，保留 `reference-settings.json`，再启动。快捷键、鼠标映射及旧启动入口保持兼容；VS Code 桥接仍为 `vicky-reference-0.1.1.vsix`。

此次更新调整便携版引用格式，不包含厂商 Stream Deck USB 连接／主程序退出问题的修复。

## English

Local file paths now keep spaces, Unicode, and parentheses readable instead of filling the Markdown source with `%20`. Complex local destinations use exactly one pair of angle brackets, with line suffixes inside the destination:

```text
[notes.md](<C:/Example/Research Notes/notes.md>)
[757–770 (line 757)](<C:/Example/Research Notes/notes.tex:757>)
```

- Multiple references remain on one line.
- Literal percent signs, hashes, and entity-sensitive characters retain necessary escaping so links identify the correct file. Existing web URL escapes and signed parameters remain intact.
- Added format regressions. Local validation covers Markdown parsing, real-file round trips, and Unicode/HTML clipboard output; a user confirmed readable spaces with a physical button.
- Both README languages and bundled guides explain readable path syntax.

Choose the English or Chinese full package. To upgrade, exit the tray app, replace the four EXEs from `QuickCopyReference-update-1.0.8.zip`, keep `reference-settings.json`, then restart. Existing shortcuts, mouse mappings, and legacy entry points remain compatible. The VS Code bridge remains `vicky-reference-0.1.1.vsix`.

This release changes portable reference formatting. It does not fix vendor Stream Deck USB connectivity or application shutdown issues.

## Checks and privacy / 检查与隐私

Release packages are published from GitHub Actions artifacts only after Windows builds, bilingual UI and shortcut checks, reference formats, document adapters, clipboard tests, and package verification pass. Fresh settings and file allowlists exclude personal documents, preferences, logs, clipboard contents, runtime state, and debug symbols. SHA256 checksums are included.

仅在 GitHub Actions 构建、双语界面与快捷键、引用格式、文档适配、剪贴板和打包检查通过后发布。使用首次启动配置和文件清单，排除私人文档、偏好、日志、剪贴板内容、运行状态及调试符号，附 SHA256 校验值。

PolyForm Noncommercial 1.0.0 remains the source license. / 源码许可继续使用 PolyForm Noncommercial 1.0.0。
