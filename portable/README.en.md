# Vicky Reference Portable

[简体中文](README.zh-CN.md) | **English**

Version **1.0.5**. Copy a reference to the focused file, document, or web page with your own global keyboard shortcut or a mouse side button. Selected text and locations are included when available. The clipboard contains a single-line Markdown reference and an HTML link. No Stream Deck, Node.js, or npm is required to run the app.

## Install and choose a language

1. Download `VickyReference-portable-1.0.5-en.zip` for English or `VickyReference-portable-1.0.5-zh-CN.zip` for Simplified Chinese. Extract the whole folder to a writable, permanent location.
2. Run `VickyReference.exe`. First launch asks you to choose a language and shortcut. Click the shortcut box and press your preferred combination, such as Ctrl+Alt+Shift+R.
3. Save and enable it. Select text or files in a supported app, release the keys, press your shortcut, then paste.

Both packages contain the same bilingual executable. Change the language immediately in **tray menu → Language → English / 简体中文**, or in the shortcut settings dialog. The choice is saved in `reference-settings.json`. Existing settings without a language use the Windows UI language; non-Chinese languages default to English. `--language=en` or `--language=zh-CN` temporarily overrides the language for that invocation.

Settings, tray menus, success/error messages, shortcut conflict explanations, slide labels, and selection truncation notices are translated. Your filenames, paths, document text, and selected text are preserved in their original language.

## Upgrade

Exit the old tray app. Extract `VickyReference-update-1.0.5.zip` over the existing directory, replacing the three EXEs, then restart. Keep your original `reference-settings.json` to retain your shortcut and preferences. The update ZIP contains no settings file. Choose your language from the tray menu after updating.

## Shortcut conflicts

Ctrl, Alt, and Shift combinations with letters, digits, or F1–F24 are supported. Single function keys are allowed; bare letters and digits are rejected. F12 is reserved for debuggers. Windows-key combinations are not supported.

The app checks known common shortcuts and probes actual Windows global registration. Ctrl+C, Ctrl+X, and Ctrl+V are always blocked to preserve normal copy, cut, and paste. Other known common shortcuts require explicit confirmation before overriding their original function. An occupied registered global shortcut cannot be overridden. Custom app bindings and mouse macros cannot all be detected, so a clear check does not guarantee that every app is conflict-free.

## Mouse and startup

Map a side button to your chosen shortcut in your mouse software. If it supports launching a program, select `CopyReference.exe` with no arguments; it requests a copy from the resident app or captures directly when the app is not running. Optional `MouseSideButton.ahk` requires AutoHotkey v2 and replaces the forward button, XButton2, while running; change it to XButton1 to use the back button.

Enable **Start with Windows** in the tray menu or settings dialog. Startup is per Windows account and computer. Keep the folder in place, or configure startup again after moving it. Windows 10/11 with .NET Framework 4.x is required; macOS and Linux are not supported.

## Supported apps and limits

| Focused app | Reference |
| --- | --- |
| File Explorer | Selected files and folders; multiple selections stay on one line |
| Word | Saved document and available selected text |
| Excel | Saved workbook, worksheet/cell address, and available cell text |
| PowerPoint | Saved presentation, current slide, selected text or shapes; slide-sorter multi-selection |
| Chrome, Edge, Brave, Firefox | Page URL and available selected text |
| Edge local PDF | Local PDF path and available selected text |
| Notepad, MarkPad | Saved file and available selected text |
| VS Code | Local saved file and selection line range; requires the bridge below |
| Notepad++, Typora, MarkText, Cursor | Depends on exposure of a full path in a title or tab |

PowerPoint slide shows and presenter view are not adapted; return to the editing window. Unsaved files, protected views, higher-privilege apps, unavailable paths, and application versions that do not expose metadata may prevent capture. The app does not enable editing or alter security settings. Ambiguous sources are rejected rather than guessed. The receiving app decides whether to paste HTML or Markdown.

The app waits for shortcut release and refuses capture if the foreground window changes. Browser capture may briefly focus the address bar. Classic Notepad may open and cancel Save As to read its path. These actions do not save your document.

## VS Code bridge

Choose **Extensions → Install from VSIX** and install the bundled `vicky-reference-0.1.0.vsix`, then restart VS Code. Focus a saved local file. The bridge uses Ctrl+Alt+Shift+F12 internally; this is not the global shortcut you press. SSH/WSL/other remote URIs are unsupported. The portable bridge differs from the original Stream Deck bridge; use the matching instructions.

## Privacy and troubleshooting

Capture runs on demand, not as continuous window monitoring. The app does not send document data over the network. Release packages use a file allowlist, fresh first-launch settings, and exclude private documents, runtime state, logs, clipboard contents, and debug symbols.

**View latest error** opens `%LOCALAPPDATA%\VickyReference\last-error.json`. This records the version, app process name, error code, and diagnostic stage/type/HRESULT; it excludes document paths, content, and selections. `status.json` also records the local executable location for process identification and is not shipped. VS Code briefly exchanges local request/response files in `%LOCALAPPDATA%\VickyDeck\reference-bridge` to obtain a path and line range. Share only the necessary error details, not documents or clipboard content.

## Command line and source builds

```text
VickyReference.exe --start
VickyReference.exe --copy
VickyReference.exe --status
VickyReference.exe --quit
VickyReference.exe --start --language=en
```

Resident `--copy` reports that a request was submitted; the tray notification reports the capture outcome. Without a resident instance, it captures directly and returns an exit status. `ReferenceCapture.exe --diagnose` reports status without returning document paths or selections. Direct helper invocation has no tray feedback or key-release wait.

From the repository root on Windows, with Node.js, Python 3, and the .NET Framework compiler:

```powershell
& .\portable\source\build.ps1
node --test .\portable\tests\portable.test.mjs
& .\scripts\test-portable-adapters.ps1
& .\scripts\test-portable-dialog.ps1
python .\scripts\package-portable.py
python .\scripts\verify-portable-package.py
```

Outputs are in `dist`. GitHub Actions also builds/tests the Stream Deck plugin, tests both languages, and checks the portable packages. Hosted CI does not run real Office or browser apps; live capture still needs desktop testing. CI builds on main and pull requests; version-tag pushes publish the bilingual portable Release only after checks pass.

Source is available under **PolyForm Noncommercial 1.0.0**, allowing noncommercial use, modification, and redistribution. Commercial use needs separate permission. See LICENSE. This is source-available, not OSI open source. Bridge source is included in `vscode-bridge`; localization is embedded in the EXEs and does not require loose translation files at runtime.
