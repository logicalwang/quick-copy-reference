"""Verify packaged languages, default settings, allowed files, source and checksums."""
from pathlib import Path
import hashlib, io, json, os, re, zipfile
root=Path(__file__).resolve().parent.parent
portable=root/'portable'
version=re.search(r'Version="([0-9.]+)"',(portable/'source/VickyReference.cs').read_text(encoding='utf8')).group(1)
executables=['QuickCopyReference.exe', 'VickyReference.exe','CopyReference.exe','ReferenceCapture.exe']
assert (portable/'QuickCopyReference.exe').read_bytes()==(portable/'VickyReference.exe').read_bytes()==(portable/'CopyReference.exe').read_bytes()
bridge=json.loads((portable/'vscode-bridge/extension/package.json').read_text(encoding='utf8'))
assert bridge['version']=='0.1.1' and bridge['displayName']=='Quick Copy Reference Bridge'
assert bridge['name']=='vicky-reference' and bridge['publisher']=='vicky-local'
private_paths=[str(root.resolve()), os.environ.get('USERPROFILE','')]
def scan(name,data):
    if name.endswith(('.pdb','.log','.docx','.xlsx','.pptx','.pdf')) or Path(name).name in ['status.json','last-error.json','request.json']:
        raise AssertionError('Unexpected private/runtime file: '+name)
    for text in [data.decode('utf8',errors='ignore'),data.decode('utf-16-le',errors='ignore')]:
        for value in private_paths:
            if value and (value.lower() in text.lower() or value.replace('\\','/').lower() in text.lower()):
                raise AssertionError('Build-machine path found in '+name)
        if re.search(r'gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{30,}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',text):
            raise AssertionError('Credential signature found in '+name)
    if name.endswith('.vsix'):
        with zipfile.ZipFile(io.BytesIO(data)) as nested:
            for entry in nested.namelist():scan(entry,nested.read(entry))
for language in ['zh-CN','en']:
    with zipfile.ZipFile(root/'dist'/('QuickCopyReference-portable-'+version+'-'+language+'.zip')) as bundle:
        assert len(bundle.namelist())==len(set(bundle.namelist()))
        for name in bundle.namelist():
            assert name.startswith('QuickCopyReference/') and '..' not in Path(name).parts
            scan(name,bundle.read(name))
        settings=json.loads(bundle.read('QuickCopyReference/reference-settings.json'))
        assert settings=={'hotkey':'Ctrl+Alt+Shift+R','showSuccessNotification':True,'configured':False,'language':language}
        for name in executables:
            assert bundle.read('QuickCopyReference/'+name)==(portable/name).read_bytes()
        for name in ['Localizer.cs','translations.json','build.ps1','ReferenceCapture.cs','VickyReference.cs']:
            assert bundle.read('QuickCopyReference/source/'+name)==(portable/'source'/name).read_bytes()
        for name in ['README.zh-CN.md','README.en.md']:
            assert bundle.read('QuickCopyReference/'+name)==(portable/name).read_bytes()
        assert bundle.read('QuickCopyReference/README.md')==(portable/('README.'+language+'.md')).read_bytes()
with zipfile.ZipFile(root/'dist'/('QuickCopyReference-update-'+version+'.zip')) as update:
    assert set(update.namelist())==set(executables+['LICENSE'])
    for name in executables:assert update.read(name)==(portable/name).read_bytes()
for line in (root/'dist/SHA256SUMS.txt').read_text().splitlines():
    digest,name=line.split('  ');assert hashlib.sha256((root/'dist'/name).read_bytes()).hexdigest()==digest
print('Both localized packages, bilingual binaries/source, fresh settings, upgrade contents, private paths and SHA256 hashes verified.')
