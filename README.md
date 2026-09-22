# Copy Reference / 复制引用

一个 Windows Stream Deck 按键：读取当前聚焦的文件、文档或网页，把来源和可用的选中内容写成**单行 Markdown 链接**并复制到剪贴板。适合粘贴给 Codex，也能在普通 Markdown 中使用。

例如：

```text
[757–770 (line 757)](<F:/project/Section HHP.tex:757>)
[Sci-Hub: free access to millions of research papers](<https://en.wikipedia.org/wiki/Sci-Hub>)
[notes.md](<F:/notes/notes.md>) [paper.pdf](<F:/papers/paper.pdf>)
```

## 支持范围

| 当前聚焦的软件 | 复制的来源和关注点 |
| --- | --- |
| Windows 资源管理器 | 所选文件或文件夹；多选合并在同一行 |
| Chrome、Edge、Brave、Firefox | 当前网页 URL；能够读取时附上所选文字 |
| Edge 打开的本地 PDF | PDF 文件路径；能够读取时附上所选文字 |
| Word、Excel、PowerPoint | 当前已保存文档路径；可用时附上所选文字、单元格位置或幻灯片号 |
| 记事本、MarkPad | 当前已保存文件路径；能够读取时附上所选文字 |
| VS Code | 当前本地文件路径及选区行号；需要安装下面的桥接扩展 |
| Cursor | 当前本地文件路径；取决于其标签是否暴露完整路径 |

未保存、非本地或无法确定路径的文档会提示失败。部分应用无法公开选中文本，此时只复制来源。按钮只在按下时运行，不持续监控窗口；结果留在本机剪贴板，不上传到网络。运行时可能短暂切换浏览器地址栏或打开并取消记事本“另存为”对话框，以读取可靠的来源地址。

## 安装

需要 Windows 10/11、Stream Deck 7.1+。从 [Releases](../../releases) 下载 `dev.vicky.copyreference.streamDeckPlugin` 并双击安装，然后把“复制引用”动作拖到一个按键上。若尚无 Release，可按“从源码构建”自行打包。

VS Code 的行号功能需要安装仓库中的 `vscode-reference` 桥接扩展。运行 `powershell -ExecutionPolicy Bypass -File .\scripts\install-vscode-bridge.ps1`，然后**完全重启** VS Code。该脚本只复制桥接扩展到当前用户的 VS Code 扩展目录。若键位 `Ctrl+Alt+Shift+F12` 已被占用，请改动扩展中的键位，并同步更改 `ReferenceCapture.cs` 中的发送键位。桥接扩展目前仅支持本地 `file:` 文件。

## 从源码构建

在 Windows 上安装 Node.js、npm 和 .NET Framework 4.x 编译器（Windows 的 `csc.exe`），然后：

```powershell
npm ci
npm run build
npm run typecheck
npm test
npm run validate
npm run pack
```

`pack` 在目录中生成可安装的 `.streamDeckPlugin`。插件源码只包含“复制引用”这一项，不包含原 Vicky Deck 的自动化控制器、用户配置或日志。

## 许可

源码公开，按 [PolyForm Noncommercial License 1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0) 授权。允许非商业使用、修改和分发；商业使用需要另行取得许可。由于包含非商业限制，本项目是 **source-available（源码可见）**，不是 OSI 定义的 open source。详见 [LICENSE](LICENSE)。第三方 npm 依赖各自保留原许可。

问题反馈请附软件名称、版本、触发方式和错误提示；不要附含敏感路径或选中文字的完整日志。
