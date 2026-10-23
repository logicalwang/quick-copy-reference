# Quick Copy Reference v1.0.9 · Office 本地同步路径 / Local Office sync paths

## 简体中文

修复 Word／Excel／PowerPoint 对已同步的 OneDrive 文档返回 SharePoint 云端网址，导致引用无法指向本地文件的问题。

- 从当前用户注册的 OneDrive 同步根读取云端命名空间与本地目录的对应关系，按完整相对路径还原到已存在的本地文件。
- 支持多个工作账号和带 CID 的个人 OneDrive；重复注册项去重，更具体的同步目录优先。
- 不按文件名搜索或猜测。匹配到多个现存副本时拒绝选择；已知同步目录中的文件缺失时提示检查同步状态。没有注册映射的在线文档继续使用原网址。
- URL 路径段只解码一次，保留真实文件名里的百分号；拒绝路径穿越、编码分隔符及账号边界不匹配。
- 保留所选文字、位置，以及 v1.0.8 的可读空格与括号。新增 28 项 Office 同步路径检查，并已核验本机真实注册映射。

更新前退出托盘程序，用 `QuickCopyReference-update-1.0.9.zip` 覆盖四个 EXE，保留 `reference-settings.json`，再启动。也可下载简体中文或英文完整包。VS Code 桥接仍为 `vicky-reference-0.1.1.vsix`。

## English

Fixes references to locally synced OneDrive Office documents when Word, Excel, or PowerPoint reports a SharePoint URL instead of a local filename.

- Read registered cloud namespaces and local mount points for the current Windows user, then resolve the complete relative path to an existing local file.
- Support multiple work accounts and CID-qualified personal OneDrive roots. Deduplicate registration entries and prefer the most specific sync root.
- Never search or guess by filename. Reject multiple existing matches; report a missing file under a known sync root instead of silently switching to its cloud URL. Online documents without a registered mapping retain their original URL.
- Decode URL segments once, preserve literal percent signs in filenames, and reject traversal, encoded separators, and account-boundary mismatches.
- Retain selected text, locations, and readable spaces/parentheses from v1.0.8. Add 28 Office sync-path checks and validate a real registered mapping locally.

To upgrade, exit the tray app, replace the four EXEs from `QuickCopyReference-update-1.0.9.zip`, keep `reference-settings.json`, and restart. Full packages are available in English and Simplified Chinese. The VS Code bridge remains `vicky-reference-0.1.1.vsix`.

## Checks and privacy / 检查与隐私

Published only after GitHub Actions builds, bilingual UI/shortcut tests, format and document-adapter checks, clipboard tests, and package verification pass. Packages contain fresh settings and allowlisted files; no personal account URLs, local paths, documents, preferences, logs, clipboard contents, or debug symbols. SHA256 checksums are included.

仅在自动构建、双语界面／快捷键、格式与来源适配、剪贴板和打包检查通过后发布。包内使用首次启动配置及文件清单，不包含个人账号网址、本机路径、文档、偏好、日志、剪贴板内容或调试符号，附 SHA256 校验值。
