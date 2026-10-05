"""Package only reviewed portable files. Run from the repository root after building."""
from pathlib import Path
import hashlib
import json
import zipfile

root = Path(__file__).resolve().parent.parent
portable = root / 'portable'
output = root / 'dist'
output.mkdir(exist_ok=True)
version = '1.0.4'
executables = ['VickyReference.exe', 'CopyReference.exe', 'ReferenceCapture.exe']
source_files = ['source/build.ps1', 'source/ReferenceCapture.cs', 'source/VickyReference.cs']
bridge_files = ['extension/package.json', 'extension/extension.js', 'extension/LICENSE', 'extension.vsixmanifest', '[Content_Types].xml']
default_settings = {'hotkey': 'Ctrl+Alt+Shift+R', 'showSuccessNotification': True, 'configured': False}

with zipfile.ZipFile(output / 'vicky-reference-0.1.0.vsix', 'w', zipfile.ZIP_DEFLATED) as bundle:
    for name in bridge_files:
        bundle.writestr(name, (portable / 'vscode-bridge' / name).read_bytes())

full_name = 'VickyReference-portable-' + version + '.zip'
with zipfile.ZipFile(output / full_name, 'w', zipfile.ZIP_DEFLATED) as bundle:
    for name in executables + source_files + ['MouseSideButton.ahk']:
        bundle.writestr('VickyReference/' + name, (portable / name).read_bytes())
    bundle.writestr('VickyReference/reference-settings.json', json.dumps(default_settings, indent=2) + '\n')
    bundle.writestr('VickyReference/README.md', (portable / 'README.md').read_bytes())
    bundle.writestr('VickyReference/LICENSE', (root / 'LICENSE').read_bytes())
    bundle.writestr('VickyReference/vicky-reference-0.1.0.vsix', (output / 'vicky-reference-0.1.0.vsix').read_bytes())
    for name in bridge_files:
        bundle.writestr('VickyReference/vscode-bridge/' + name, (portable / 'vscode-bridge' / name).read_bytes())

update_name = 'VickyReference-update-' + version + '.zip'
with zipfile.ZipFile(output / update_name, 'w', zipfile.ZIP_DEFLATED) as bundle:
    for name in executables:
        bundle.writestr(name, (portable / name).read_bytes())
    bundle.writestr('LICENSE', (root / 'LICENSE').read_bytes())

checksums = []
for name in [full_name, update_name, 'vicky-reference-0.1.0.vsix']:
    checksums.append(hashlib.sha256((output / name).read_bytes()).hexdigest() + '  ' + name)
(output / 'SHA256SUMS.txt').write_text('\n'.join(checksums) + '\n', encoding='ascii')
print('Created portable ZIP, update ZIP, matching VSIX and SHA256SUMS.txt using an explicit file allowlist.')
