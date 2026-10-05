# Copy Reference Portable v1.0.5 — 简体中文 / English

## 简体中文

提供完整的简体中文和英文界面，以及两种语言的 README。首次设置窗口和托盘语言菜单均可立即切换，并保存选择。成功／错误提示、快捷键冲突说明、页码与截断提示随语言变化；文档正文、路径和所选文字保持原文。

- 中文完整包：`VickyReference-portable-1.0.5-zh-CN.zip`。
- 英文完整包：`VickyReference-portable-1.0.5-en.zip`。
- 旧便携版更新：退出托盘，用 `VickyReference-update-1.0.5.zip` 覆盖三个 EXE，保留自己的 `reference-settings.json`，再启动并选择语言。
- VS Code 需安装随包或单独附件的 `vicky-reference-0.1.0.vsix`。

新增 GitHub Actions Windows 构建、测试、校验和打包。此 Release 只在云端检查通过后从该次构建产物发布；云端不运行真实 Office／浏览器。保留 v1.0.4 的 Office／MarkPad 修复。PowerPoint 放映／演讲者视图仍需回到编辑窗口。原 Stream Deck 插件仍使用 v0.1.0。

## English

Complete Simplified Chinese and English interfaces and READMEs. Switch languages immediately in the first-run/settings dialog or tray language menu; the choice is saved. Success/error messages, shortcut conflict explanations, slide labels, and truncation notices follow the chosen language. Document text, paths, and selections are preserved.

- English full package: `VickyReference-portable-1.0.5-en.zip`.
- Simplified Chinese full package: `VickyReference-portable-1.0.5-zh-CN.zip`.
- Upgrade: exit the tray app, replace the three EXEs with `VickyReference-update-1.0.5.zip`, keep your `reference-settings.json`, restart, and choose your language.
- VS Code requires the included or separately attached `vicky-reference-0.1.0.vsix`.

Adds GitHub Actions Windows builds, tests, validation, and packaging. This Release is published from CI artifacts only after checks pass; hosted CI does not run real Office/browser apps. Keeps the v1.0.4 Office/MarkPad fixes. Return from PowerPoint slide shows/presenter view to the editor to copy. The original Stream Deck plugin remains at v0.1.0.

## Privacy / 隐私与许可

Both full packages use the same bilingual binaries, fresh first-launch settings, a reviewed file allowlist, and the original LICENSE. No private documents, personal settings, logs, clipboard content, runtime state, or debug symbols are included. / 两个完整包使用同一双语程序、首次启动配置、明确文件清单和原 LICENSE，不含私人文档、个人配置、日志、剪贴板、运行状态或调试符号。

SHA256 hashes are attached. / 附带 SHA256 校验值。Source available under PolyForm Noncommercial 1.0.0; commercial use needs separate permission. / 沿用非商业源码许可，商业使用需另行许可。
