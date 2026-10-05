"""Package only reviewed portable files. Run from the repository root after building."""
from pathlib import Path
import hashlib
import json
import re
import zipfile

root = Path(__file__).resolve().parent.parent
portable = root / 'portable'
output = root / 'dist'
output.mkdir(exist_ok=True)
version = re.search(r'Version="([0-9.]+)"', (portable / 'source/VickyReference.cs').read_text(encoding='utf8')).group(1)
executables = ['QuickCopyReference.exe', 'VickyReference.exe', 'CopyReference.exe', 'ReferenceCapture.exe']
source_files = ['source/build.ps1', 'source/ReferenceCapture.cs', 'source/VickyReference.cs', 'source/Localizer.cs', 'source/translations.json']
bridge_files = ['extension/package.json', 'extension/extension.js', 'extension/LICENSE', 'extension.vsixmanifest', '[Content_Types].xml']
default_settings = {'hotkey': 'Ctrl+Alt+Shift+R', 'showSuccessNotification': True, 'configured': False}

with zipfile.ZipFile(output / 'vicky-reference-0.1.1.vsix', 'w', zipfile.ZIP_DEFLATED) as bundle:
    for name in bridge_files:
        bundle.writestr(name, (portable / 'vscode-bridge' / name).read_bytes())

full_names = []
for language in ['zh-CN', 'en']:
    full_name = 'QuickCopyReference-portable-' + version + '-' + language + '.zip'
    full_names.append(full_name)
    with zipfile.ZipFile(output / full_name, 'w', zipfile.ZIP_DEFLATED) as bundle:
        for name in executables + source_files + ['MouseSideButton.ahk']:
            bundle.writestr('QuickCopyReference/' + name, (portable / name).read_bytes())
        bundle.writestr('QuickCopyReference/reference-settings.json', json.dumps(dict(default_settings, language=language), indent=2) + '\n')
        bundle.writestr('QuickCopyReference/README.md', (portable / ('README.' + language + '.md')).read_bytes())
        for name in ['README.zh-CN.md', 'README.en.md']:
            bundle.writestr('QuickCopyReference/' + name, (portable / name).read_bytes())
        bundle.writestr('QuickCopyReference/LICENSE', (root / 'LICENSE').read_bytes())
        bundle.writestr('QuickCopyReference/vicky-reference-0.1.1.vsix', (output / 'vicky-reference-0.1.1.vsix').read_bytes())
        for name in bridge_files:
            bundle.writestr('QuickCopyReference/vscode-bridge/' + name, (portable / 'vscode-bridge' / name).read_bytes())

update_name = 'QuickCopyReference-update-' + version + '.zip'
with zipfile.ZipFile(output / update_name, 'w', zipfile.ZIP_DEFLATED) as bundle:
    for name in executables:
        bundle.writestr(name, (portable / name).read_bytes())
    bundle.writestr('LICENSE', (root / 'LICENSE').read_bytes())

checksums = []
for name in full_names + [update_name, 'vicky-reference-0.1.1.vsix']:
    checksums.append(hashlib.sha256((output / name).read_bytes()).hexdigest() + '  ' + name)
(output / 'SHA256SUMS.txt').write_text('\n'.join(checksums) + '\n', encoding='ascii')
print('Created portable ZIP, update ZIP, matching VSIX and SHA256SUMS.txt using an explicit file allowlist.')
