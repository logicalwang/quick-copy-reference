# Quick Copy Reference

[简体中文](README.zh-CN.md) | **English**

[![Windows build and tests](https://github.com/logicalwang/quick-copy-reference/actions/workflows/build.yml/badge.svg)](https://github.com/logicalwang/quick-copy-reference/actions/workflows/build.yml)

**Press a hotkey to copy what you are looking at as a link with its source and selected text.**

For **Windows 10/11**. Use a global keyboard shortcut, mouse side button, or script command to read the focused supported app. Paste the reference into Codex, Markdown documents, notes, or another app that accepts text or links. Simplified Chinese and English interfaces are included. Stream Deck is optional.

```text
Select text or files → Press your hotkey or mouse button → Paste a source link
```

## Get started in 30 seconds

1. Download the English or Chinese full portable package from [Releases](https://github.com/logicalwang/quick-copy-reference/releases) and extract it to a permanent folder.
2. Run `QuickCopyReference.exe` and choose your shortcut, for example **Ctrl+Alt+Shift+R**.
3. Focus the content in a browser, Office app, file manager, or editor. Select text to include a point of interest, or select files to reference them.
4. Press your shortcut, then paste in the receiving app. A mouse side button can use the same shortcut or launch `CopyReference.exe` directly.

Running the app only needs Windows .NET Framework 4.x. Startup is optional; capture runs on demand and the result stays in the local clipboard.

## What gets copied

The source, selected text, and location form **one single-line Markdown link**, with an HTML clipboard link too. References to multiple selected files also stay on one line.

```text
[Page title: the passage I want to discuss](https://example.com/article)
[757–770 (line 757)](C:/Example/notes.tex:757)
[deck.pptx: Slide 2: selected text](C:/Example/deck.pptx)
[notes.md](C:/Example/notes.md) [paper.pdf](C:/Example/paper.pdf)
```

## Supported source apps

The global shortcut is available across Windows; automatic source capture supports the apps listed below. Not every app exposes a file path or selection. Copied references can be pasted into other apps.

| Focused app | Reference |
| --- | --- |
| File Explorer | Selected files/folders, including multi-selection |
| Chrome, Edge, Brave, Firefox | Page URL and available selected text |
| Edge local PDF | Local PDF path and available selected text |
| Word, Excel, PowerPoint | Saved file, available selection, cells, or slide location |
| Notepad, MarkPad | Saved file and available selected text |
| VS Code | Saved local file and selected line range; bridge required |
| Notepad++, Typora, MarkText, Cursor | Depends on full path exposure in a title or tab |

PowerPoint editing and slide-sorter views are supported in the portable app; slide shows and presenter view are not adapted. Unsaved documents, remote URIs, protected views, higher-privilege apps, and unsupported application versions may prevent capture. Ambiguous sources are rejected. Filenames and selected text are not translated. Capture is on demand; document data is not uploaded. Browser address focus or a canceled Notepad Save As dialog may be used to identify the source. Receiving apps control HTML/Markdown paste behavior.

## Keyboard, mouse, and script entry points

| Entry point | How to use it |
| --- | --- |
| Keyboard | Start the app and choose a global shortcut |
| Mouse side button | Map the shortcut or launch `CopyReference.exe` |
| Script / launcher | Call `QuickCopyReference.exe --copy`, or launch `CopyReference.exe` |
| Stream Deck | Map a button to the shortcut or `CopyReference.exe` |

Keep the source app focused when triggering capture; scripts need to run in the same interactive Windows desktop session. Use the tray menu to change the shortcut or language, or enable Windows startup. The app warns about shortcut conflicts and blocks Ctrl+C/Ctrl+X/Ctrl+V.

## Downloads, language, and upgrades

Download from [Releases](https://github.com/logicalwang/quick-copy-reference/releases):

- `QuickCopyReference-portable-1.0.6-en.zip`: English by default.
- `QuickCopyReference-portable-1.0.6-zh-CN.zip`: Simplified Chinese by default.
- `QuickCopyReference-update-1.0.6.zip`: update the four EXEs and preserve your settings.

Extract the whole folder and run `QuickCopyReference.exe`. First launch asks you to choose a language and your own shortcut. The same bilingual executable is included in both downloads. Switch languages immediately in the settings dialog or **tray menu → Language**. Mouse software can map a side button to the shortcut or run `CopyReference.exe` directly. Optional startup and success notifications are configurable. Windows 10/11 with .NET Framework 4.x is required; running the portable app needs no Stream Deck, Node.js, or npm.

Upgrading: exit the old tray app, replace the four EXEs using the update ZIP, keep `reference-settings.json`, and restart. VS Code requires the portable app's `vicky-reference-0.1.1.vsix`. Detailed setup, language behavior, privacy, limits, and build commands: [portable English guide](portable/README.en.md) / [中文指南](portable/README.zh-CN.md).

The old `VickyReference.exe` remains a compatible entry point for existing mouse mappings, scripts, and startup links.

## Build and automatic checks

On Windows with Node.js, npm, Python 3, and the .NET Framework compiler:

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

[GitHub Actions](https://github.com/logicalwang/quick-copy-reference/actions/workflows/build.yml) runs these checks on main, pull requests, manual dispatch, and version tags using Windows. Successful builds retain artifacts; matching version tags publish both portable language packages only after all checks pass. CI tests compilation, formatting, shortcut safety, bilingual dialogs, legacy settings, and packaging; it does not install Office or validate real Office/browser capture. Live desktop verification remains necessary.

Release packaging uses a reviewed allowlist and fresh configuration. Private documents, logs, clipboard contents, runtime state, and debug symbols are excluded. `SHA256SUMS.txt` provides download hashes.

## License

Source available under [PolyForm Noncommercial 1.0.0](LICENSE). Noncommercial use, modification, and redistribution are allowed; commercial use needs separate permission. The noncommercial restriction means this is source-available, not OSI open source. Third-party dependencies retain their own licenses.

Report the app name/version, trigger method, and error details. Avoid sharing sensitive paths, document content, or selections.

## Optional: original Stream Deck plugin

Requires Stream Deck 7.1+ on Windows 10/11. Download `dev.vicky.copyreference.streamDeckPlugin` from [v0.1.0](https://github.com/logicalwang/quick-copy-reference/releases/tag/v0.1.0), install it, and drag Copy Reference onto a button.

For VS Code, run `powershell -ExecutionPolicy Bypass -File .\scripts\install-vscode-bridge.ps1`, then fully restart VS Code. This installs the original plugin's bridge from `vscode-reference`; it differs from the portable bridge. Both use Ctrl+Alt+Shift+F12 internally and support local files only.
