# Quick Copy Reference · 快速复制引用

**简体中文** | [English](README.en.md)

版本 **1.0.6**。使用自选全局快捷键或鼠标侧键，复制当前聚焦文件、文档或网页的引用；可读取时加入选中文字和位置。剪贴板同时提供单行 Markdown 引用和 HTML 链接。运行不需要 Stream Deck、Node.js 或 npm。

## 安装与语言

1. 中文下载 `QuickCopyReference-portable-1.0.6-zh-CN.zip`，英文下载 `QuickCopyReference-portable-1.0.6-en.zip`。完整解压到可写、准备长期使用的目录。
2. 启动 `QuickCopyReference.exe`，首次选择语言和快捷键。点击输入框，直接按喜欢的组合键，例如 Ctrl+Alt+Shift+R。
3. 保存并启用。在支持的软件中选中文字或文件，松开按键，按快捷键，再粘贴。

两个包使用相同的双语程序。可在 **托盘菜单 → 语言 → 简体中文／English**，或快捷键设置窗口切换，立即生效。选择保存在 `reference-settings.json`。旧配置没有语言字段时按 Windows 界面语言选择；非中文系统默认英文。`--language=zh-CN` 或 `--language=en` 可临时覆盖本次调用的语言。

设置窗口、托盘菜单、成功与失败提示、快捷键冲突说明、幻灯片标签和选区截断提示均提供两种语言。文件名、路径、文档正文和所选文字保留原语言。

## 更新

`QuickCopyReference.exe` 是新的主入口；旧名 `VickyReference.exe` 保留为同一程序的兼容入口，原鼠标映射、脚本和开机启动仍可使用。内部配置和本地状态目录沿用旧名称，保留已有偏好。


先退出旧托盘程序，用 `QuickCopyReference-update-1.0.6.zip` 覆盖四个 EXE，再启动。保留原来的 `reference-settings.json`，以保留快捷键和偏好。更新包没有配置文件；更新后可从托盘选择语言。

## 快捷键冲突

支持 Ctrl、Alt、Shift 配合字母、数字或 F1–F24，允许单个功能键；不接受裸字母／数字。F12 为调试器保留，不支持 Windows 键组合。

程序检查已知常用快捷键，并实际探测 Windows 全局注册。Ctrl+C、Ctrl+X、Ctrl+V 始终禁止，保留正常复制／剪切／粘贴。其他已知常用快捷键需要明确确认才能覆盖；已被注册占用的全局键不能覆盖。无法全面检测应用自定义键位和鼠标宏，检查通过不保证每个应用都无冲突。

## 鼠标与开机启动

在鼠标软件中把侧键映射为所选组合键。若支持运行程序，选择 `CopyReference.exe`，无需参数；它可通知托盘程序，也可在未常驻时直接读取。可选 `MouseSideButton.ahk` 需要 AutoHotkey v2，运行时替代前进侧键 XButton2；改为 XButton1 可使用后退侧键。

从托盘或设置窗口启用 **开机启动**。按 Windows 用户和电脑分别设置；移动目录后需重新设置。需要 Windows 10／11 与 .NET Framework 4.x，不支持 macOS 或 Linux。

## 支持范围与限制

| 聚焦软件 | 引用内容 |
| --- | --- |
| 资源管理器 | 选中的文件／文件夹，多选保持同一行 |
| Word | 已保存文档与可读取的选中文字 |
| Excel | 已保存工作簿、工作表／单元格位置及可读取的单元格文字 |
| PowerPoint | 已保存演示文稿、当前页、所选文字／形状；幻灯片浏览视图多选 |
| Chrome、Edge、Brave、Firefox | 网页地址与可读取的选中文字 |
| Edge 本地 PDF | PDF 文件路径与可读取的选中文字 |
| 记事本、MarkPad | 已保存文件与可读取的选中文字 |
| VS Code | 本地已保存文件及选区行号，需安装桥接扩展 |
| Notepad++、Typora、MarkText、Cursor | 取决于标题或标签是否提供完整路径 |

PowerPoint 放映／演讲者视图暂未适配，请返回编辑窗口。未保存文件、受保护视图、高权限应用、不可用路径及不暴露元数据的软件版本可能无法读取。不会自动开启编辑或改变安全设置；来源不确定时拒绝猜测。接收应用决定粘贴 HTML 还是 Markdown。

程序等待快捷键释放，期间前台窗口改变则拒绝读取。浏览器可能短暂聚焦地址栏；经典记事本可能打开并取消另存为，以取得路径。这些操作不保存文档。

## VS Code 桥接

在 **扩展 → 从 VSIX 安装** 选择包内的 `vicky-reference-0.1.1.vsix`，重启 VS Code，然后聚焦本地已保存文件。内部使用 Ctrl+Alt+Shift+F12，不是你手动按的全局快捷键。暂不支持 SSH／WSL 等远程 URI。便携版与原 Stream Deck 版桥接扩展不同，请按各自说明安装。

## 隐私与排错

按需读取，不持续监控窗口，不通过网络发送文档数据。发布包使用明确的文件清单及首次启动配置，不纳入私人文档、运行状态、日志、剪贴板内容或调试符号。

**查看最近错误** 打开 `%LOCALAPPDATA%\VickyReference\last-error.json`，记录版本、进程名、错误代码及诊断阶段／异常类型／HRESULT，不含文档路径、正文或选区。`status.json` 另含程序的本地位置，用于识别运行进程，不随发布包分发。VS Code 在 `%LOCALAPPDATA%\VickyDeck\reference-bridge` 短暂交换本地请求／响应文件以取得路径和行号。反馈时仅提供必要的错误信息，避免分享文档或剪贴板内容。

## 脚本调用与源码构建

```text
QuickCopyReference.exe --start
QuickCopyReference.exe --copy
QuickCopyReference.exe --status
QuickCopyReference.exe --quit
QuickCopyReference.exe --start --language=zh-CN
```

常驻时 `--copy` 返回请求已提交，实际结果由托盘提示；未常驻时直接读取，并以退出码表示结果。`ReferenceCapture.exe --diagnose` 只返回状态，不返回文档路径或选区。直接调用底层程序没有托盘反馈及组合键释放等待。

在 Windows 仓库根目录，安装 Node.js、Python 3 并具备 .NET Framework 编译器后运行：

```powershell
& .\portable\source\build.ps1
node --test .\portable\tests\portable.test.mjs
& .\scripts\test-portable-adapters.ps1
& .\scripts\test-portable-dialog.ps1
python .\scripts\package-portable.py
python .\scripts\verify-portable-package.py
```

产物位于 `dist`。GitHub Actions 还会构建／测试 Stream Deck 插件、测试两种语言并检查便携包。云端 CI 不运行真实 Office 或浏览器，实际读取仍需桌面验证。main 和 PR 自动检查；版本标签推送后，检查通过才发布双语便携版 Release。

沿用 **PolyForm Noncommercial 1.0.0**，允许非商业使用、修改和分发，商业使用需另行许可；详见 LICENSE。属于源码可见，不属于 OSI 定义的开源。桥接源码位于 `vscode-bridge`；翻译已嵌入 EXE，运行无需额外翻译文件。
