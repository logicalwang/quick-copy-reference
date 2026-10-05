# Copy Reference Portable v1.0.4

这是无需 Stream Deck 的 Windows 键盘／鼠标便携版。原 Stream Deck 插件请使用 v0.1.0；此次未重新发布插件。

- 首次启动自由设置全局快捷键，支持鼠标侧键映射、程序入口调用及可选开机启动。
- 提示已知常用快捷键和已注册的全局冲突，保护 Ctrl+C／X／V。
- 修复 Office 和 MarkPad 的来源识别及应用对应的错误提示。
- 修复 PowerPoint 编辑窗口识别，引用带当前页码及可读取的选中文字；幻灯片浏览视图多选带所选页码。放映／演讲者视图暂未适配。
- 单行 Markdown 和 HTML 引用保留中文路径，支持 Explorer 多选及 VS Code 本地文件行号。

## 下载与更新

首次使用：下载完整便携 ZIP，解压后启动 `VickyReference.exe`。

已有便携版：退出托盘程序，用更新 ZIP 中三个 EXE 覆盖旧文件，保留自己的 `reference-settings.json`，再启动。更新包不包含配置文件。

VS Code：安装 `vicky-reference-0.1.0.vsix`，重启 VS Code。这是便携版对应的桥接扩展。

## 隐私、许可与验证

发布包使用明确的文件清单，完整包配置首次启动询问快捷键；不附带私人文件、测试文档、剪贴板内容、日志、状态文件或调试符号。源码、EXE 和嵌套 VSIX 已检查个人路径和凭据特征。

Windows 上已验证真实 Word、Excel、MarkPad、PowerPoint 快捷键复制，以及 PowerPoint 页码、选区、多选和两个演示文稿切换。快捷键与引用格式回归通过。其他应用版本和受保护视图仍可能限制读取；不会自动开启文档编辑或改变安全设置。

沿用 PolyForm Noncommercial 1.0.0 许可，源码可见、允许非商业使用；详见 LICENSE。附件 `SHA256SUMS.txt` 提供下载文件校验值。
