# Vicky Reference 便携版

当前版本：1.0.4。更新已有便携目录时，先在托盘退出旧版，再替换三个 EXE；保留自己的 `reference-settings.json` 即可保留快捷键和提示偏好。不要同时运行旧目录里的程序。

## 第一次使用

1. 解压整个文件夹，放在你准备长期使用的位置。保留 EXE 和配置文件在同一目录。
2. 双击 `VickyReference.exe`，首次启动会出现快捷键设置窗口。
3. 点击快捷键输入框，直接按你喜欢的组合键，例如 `Ctrl+Alt+Shift+R`、`Ctrl+Shift+F9`，也支持单个 `F9`。录入后检查已知常用快捷键及已注册的全局冲突；发现冲突会显示具体用途并禁用“保存并启用”。保存前还会再次检查。
4. 到目标软件中选中文字或文件，松开按键后按你设置的快捷键，再粘贴。

以后改快捷键：右下角托盘图标 → **设置快捷键…**。无需编辑配置文件。设置窗口还可以选择开机启动、关闭成功提示。取消首次设置时不会启用任何快捷键；下次启动仍会询问。

支持 Ctrl、Alt、Shift 配合字母、数字、功能键，也支持单个功能键。单个普通字母不接受，以免拦截打字；F12 被 Windows 调试器保留，不能用作此工具的全局快捷键。

你选择的按键会被全局用于复制引用。

冲突检测分两部分：

- 已知常用快捷键清单，例如 `Ctrl+C` 复制、`Ctrl+S` 保存、`Ctrl+F` 查找、`Ctrl+Z` 撤销、`F5` 刷新、`F9` VS Code 断点。显示按键及具体用途，不会将它们当成没有冲突。
- Windows 已注册的全局快捷键：实际注册探测，发现已被占用就禁止保存，不能通过勾选覆盖。

`Ctrl+C`、`Ctrl+X`、`Ctrl+V` 始终禁止用于复制引用，保留正常复制／剪切／粘贴。其他已知常用快捷键默认也不能保存；如果你确实想替换其原功能，可以明确勾选“仍然使用此常用快捷键”，再保存。重新录入按键会清除该确认。启动和重新加载配置也检查这些规则，不能通过把 `Ctrl+C` 写进 JSON 绕过限制。

仍不能全面检测所有应用自定义按键或鼠标宏。“未发现已知常用／已注册的全局冲突”不保证所有软件都没有重复按键。常用清单依据 [Microsoft Windows 快捷键](https://support.microsoft.com/en-us/accessibility/windows/keyboard-shortcuts-in-windows)、[Chrome 快捷键](https://support.google.com/chrome/answer/157179?hl=en) 和 [VS Code 快捷键](https://code.visualstudio.com/docs/configure/keybindings)。注册机制参见 [Microsoft RegisterHotKey 文档](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)；此接口不能告知占用快捷键的软件名称。

## 鼠标侧键

优先在鼠标自带的软件中，把一个侧键设置为你选择的快捷键。工具在托盘运行时，侧键和键盘的效果一致。

鼠标软件如果支持“运行程序”：直接选择本目录的 **`CopyReference.exe`**，不需要填参数。该入口既可以通知已经运行的托盘程序，也可以在未常驻时直接复制；它不会先弹出主窗口。也可以用 `VickyReference.exe` 加参数 `--copy`。

没有鼠标配置软件时，可选安装 **AutoHotkey v2**，再双击随包的 `MouseSideButton.ahk`。此脚本将前进侧键 `XButton2` 用于复制引用，运行期间会替代该侧键原本的前进功能；退出脚本即可恢复。将脚本中的 `XButton2` 改为 `XButton1` 可换为后退侧键。这是可选方案，键盘快捷键不依赖 AutoHotkey。脚本遵循 [AutoHotkey v2 按键名称](https://www.autohotkey.com/docs/v2/KeyList.htm) 和 [Run 用法](https://www.autohotkey.com/docs/v2/lib/Run.htm)。

## 在另一台电脑使用

复制这个文件夹或重新解压便携包到另一台 **Windows 10／11** 电脑即可。无需 Stream Deck、Node.js 或 npm。工具使用 Windows 的 .NET Framework 4.x，若系统没有启用该运行库，需要先启用／安装；参见 [Microsoft 的 Windows 与 .NET Framework 版本说明](https://learn.microsoft.com/en-us/dotnet/framework/install/versions-and-dependencies)。macOS、Linux 不在此包支持范围。

包里的默认配置会在首次启动询问快捷键。如果复制的是已经配置好的文件夹，则会保留原快捷键；可在托盘菜单重新设置。

开机启动按每台电脑分别设置，创建的是当前用户 Startup 目录下的 `VickyReference.lnk`。移动工具目录后，在新位置重新设置开机启动。退出前不要删除／移动正在运行的目录。

## VS Code

要在另一台电脑读取 VS Code 的真实文件路径与所选行号，需要在 VS Code 扩展界面选择 **从 VSIX 安装**，安装本目录的 `vicky-reference-0.1.0.vsix`。完成后打开一个已保存的本地文件并聚焦编辑区，再使用你设置的全局快捷键。

扩展内部的 `Ctrl+Alt+Shift+F12` 是工具自动触发的读取接口，不是你要手动使用的全局快捷键。远程 SSH／WSL 文档的 URI 不是本地 Windows 文件，此桥接扩展当前不支持它们。

## 支持与限制

基于 Stream Deck 引用引擎，便携版已单独更新 Office 和 Markdown 读取。支持资源管理器多选文件／文件夹、Word、Excel、PowerPoint、Chrome／Edge 网页与 Edge 中打开的本地 PDF、记事本、MarkPad、VS Code。Notepad++、Typora、MarkText 在能从选中标签或窗口标题取得完整路径时也可读取；只有文件名时不会猜测路径。选区放进链接标题，多个文件仍输出同一行，中文路径保持可读，剪贴板同时提供 Markdown 文本和 HTML 链接。

文件需有明确来源，未保存文档请先保存；某些应用／版本无法公开选区时只能复制来源。接收应用决定是否使用 HTML 或纯文本，不能保证所有编辑器的粘贴呈现一致。权限高于本工具的窗口可能无法读取；此工具默认以普通用户运行。

快捷键只等待释放组合键后再读取；如果期间换了窗口，会提示重试。正在读取时不会同时启动另一轮。失败通过托盘通知提示，不输出文档内容日志，也不联网。

## Office 和 Markdown 修复

Office 优先读取当前窗口的原生文档和选区，不再先扫描通用 UIA 选区树，减少辅助功能提供程序异常阻断引用的机会。原生对象不可用时，Word／Excel 使用只读 COM 备用读取并核对窗口句柄。PowerPoint 的 DocumentWindow 没有可用的 HWND 属性，改为核对当前编辑窗口、同一会话的唯一 PowerPoint 进程以及唯一匹配的文档窗口标题与活动状态；同名窗口或多个进程不确定时拒绝猜测。PowerPoint 普通编辑视图带当前幻灯片页码，选中文字／形状文字进入链接标题，幻灯片浏览视图带所选页码。放映／演讲者视图暂未适配，请回到编辑窗口复制。弹出的对话框、未保存文档或受保护视图仍可能限制读取，不会自动开启编辑或改变安全设置。

MarkPad 优先取活动标签。活动状态未暴露时，使用唯一的完整路径元数据，或将当前窗口标题与已暴露的多个完整路径明确匹配；同名／多个候选不确定时拒绝猜测。普通阅读器读取失败时不会再提示安装 VS Code 扩展，只有实际前台应用为 VS Code 才会显示该说明。

仍失败时，在托盘菜单选择 **查看最近错误**。记录位于 `%LOCALAPPDATA%\VickyReference\last-error.json`，包括版本、应用进程名、错误代码和读取阶段／异常类型／HRESULT，不含文件路径、文档正文或选区内容，不联网。你可将其中的应用和错误信息提供给开发者继续定位。直接调用 `CopyReference.exe` 失败也会写此记录。

## 可供其他脚本调用

```text
VickyReference.exe --copy
VickyReference.exe --start
VickyReference.exe --status
VickyReference.exe --quit
```

`--copy` 在托盘已经运行时返回“请求已提交”，实际成功／失败由托盘提示；未运行时直接执行并以退出码表示结果。也可以直接调用底层 `ReferenceCapture.exe --copy`，但它没有托盘反馈和组合键释放等待。

底层 `ReferenceCapture.exe --diagnose` 只返回来源读取的状态和诊断字段，不输出文档路径或选区，不写入最终引用；面向开发排查。浏览器若只能通过复制地址栏取得地址，仍可能短暂读取并恢复剪贴板。

源码在 `source` 目录。需要自己修改时，先退出工具，再运行 `source/build.ps1` 编译；日常使用不需要运行这个脚本。

在仓库根目录构建、测试和打包：

```powershell
& .\portable\source\build.ps1
node --test .\portable\tests\portable.test.mjs
& .\scripts\test-portable-adapters.ps1
python .\scripts\package-portable.py
```

打包脚本需要 Python 3，生成的文件位于 `dist`。使用明确的文件清单，完整包始终生成首次启动配置，不会打包当前用户保存的快捷键、日志或运行状态。

便携版对应的 VS Code 桥接源码位于 `vscode-bridge`；打包脚本从该源码生成 VSIX。源码按仓库的 PolyForm Noncommercial 1.0.0 许可提供，发布包包含 LICENSE。

## 本次验证

已在 Windows 上使用 Word、Excel、MarkPad（两个 Markdown 标签）中测试 Ctrl+Alt+Shift+R 键盘事件，经常驻程序读取并实写剪贴板，核对 Unicode Markdown 和 HTML 链接，测试后恢复原剪贴板。Word 和 Excel 的 COM 备用窗口匹配也实际验证通过。另已验证 Markdown 唯一元数据、标题匹配、活动标签优先及同名歧义拒绝。1.0.4 在 PowerPoint 普通编辑界面复现旧版失败并验证修复：第 2／3 页、选中文字、幻灯片浏览视图多选第 1／3 页、两份 PPT 切换均引用正确；真实全局快捷键实写剪贴板也通过，之后恢复原剪贴板。

快捷键录入、Ctrl+C 保护、常用按键确认、Windows 注册占用检测、中文／空格目录搬迁、重复启动、鼠标程序入口调度、退出及中文路径格式回归已检查。鼠标硬件映射、可选 AutoHotkey 脚本、不同 Office／阅读器版本和受保护视图尚未逐一实测；不保证所有应用版本都能公开路径或选区。
