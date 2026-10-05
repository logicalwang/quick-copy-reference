# Copy Reference

[简体中文](README.zh-CN.md) | **English**

[![Windows build and tests](https://github.com/logicalwang/stream-deck-copy-reference/actions/workflows/build.yml/badge.svg)](https://github.com/logicalwang/stream-deck-copy-reference/actions/workflows/build.yml)

A Windows tool that copies a **single-line Markdown reference** to the focused file, document, or web page, with available selected text and locations. The portable edition also writes an HTML clipboard link. Useful for Codex and Markdown editors.

Two entry points are available: a keyboard/mouse portable app and the original Stream Deck plugin. **v1.0.5 releases the bilingual portable app**; the unchanged Stream Deck plugin remains available in [v0.1.0](https://github.com/logicalwang/stream-deck-copy-reference/releases/tag/v0.1.0).

## Portable app: English and Simplified Chinese

Download from [Releases](https://github.com/logicalwang/stream-deck-copy-reference/releases):

- `VickyReference-portable-1.0.5-en.zip`: English by default.
- `VickyReference-portable-1.0.5-zh-CN.zip`: Simplified Chinese by default.
- `VickyReference-update-1.0.5.zip`: update the three EXEs and preserve your settings.

Extract the whole folder and run `VickyReference.exe`. First launch asks you to choose a language and your own shortcut. The same bilingual executable is included in both downloads. Switch languages immediately in the settings dialog or **tray menu → Language**. Mouse software can map a side button to the shortcut or run `CopyReference.exe` directly. Optional startup and success notifications are configurable. Windows 10/11 with .NET Framework 4.x is required; running the portable app needs no Stream Deck, Node.js, or npm.

Upgrading: exit the old tray app, replace the three EXEs using the update ZIP, keep `reference-settings.json`, and restart. VS Code requires the portable app's `vicky-reference-0.1.0.vsix`. Detailed setup, language behavior, privacy, limits, and build commands: [portable English guide](portable/README.en.md) / [中文指南](portable/README.zh-CN.md).

```text
[757–770 (line 757)](C:/Example/notes.tex:757)
[deck.pptx: Slide 2: selected text](C:/Example/deck.pptx)
[notes.md](C:/Example/notes.md) [paper.pdf](C:/Example/paper.pdf)
```

## Supported sources

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

## Original Stream Deck plugin

Requires Stream Deck 7.1+ on Windows 10/11. Download `dev.vicky.copyreference.streamDeckPlugin` from [v0.1.0](https://github.com/logicalwang/stream-deck-copy-reference/releases/tag/v0.1.0), install it, and drag Copy Reference onto a button.

For VS Code, run `powershell -ExecutionPolicy Bypass -File .\scripts\install-vscode-bridge.ps1`, then fully restart VS Code. This installs the original plugin's bridge from `vscode-reference`; it differs from the portable bridge. Both use Ctrl+Alt+Shift+F12 internally and support local files only.

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

[GitHub Actions](https://github.com/logicalwang/stream-deck-copy-reference/actions/workflows/build.yml) runs these checks on main, pull requests, manual dispatch, and version tags using Windows. Successful builds retain artifacts; matching version tags publish both portable language packages only after all checks pass. CI tests compilation, formatting, shortcut safety, bilingual dialogs, legacy settings, and packaging; it does not install Office or validate real Office/browser capture. Live desktop verification remains necessary.

Release packaging uses a reviewed allowlist and fresh configuration. Private documents, logs, clipboard contents, runtime state, and debug symbols are excluded. `SHA256SUMS.txt` provides download hashes.

## License

Source available under [PolyForm Noncommercial 1.0.0](LICENSE). Noncommercial use, modification, and redistribution are allowed; commercial use needs separate permission. The noncommercial restriction means this is source-available, not OSI open source. Third-party dependencies retain their own licenses.

Report the app name/version, trigger method, and error details. Avoid sharing sensitive paths, document content, or selections.
