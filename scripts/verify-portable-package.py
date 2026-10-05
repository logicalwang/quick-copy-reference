"""Verify packaged languages, default settings, allowed files, source and checksums."""
from pathlib import Path
import hashlib, io, json, os, re, zipfile
root=Path(__file__).resolve().parent.parent
portable=root/'portable'
version=re.search(r'Version="([0-9.]+)"',(portable/'source/VickyReference.cs').read_text(encoding='utf8')).group(1)
executables=['VickyReference.exe','CopyReference.exe','ReferenceCapture.exe']
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
    with zipfile.ZipFile(root/'dist'/('VickyReference-portable-'+version+'-'+language+'.zip')) as bundle:
        assert len(bundle.namelist())==len(set(bundle.namelist()))
        for name in bundle.namelist():
            assert name.startswith('VickyReference/') and '..' not in Path(name).parts
            scan(name,bundle.read(name))
        settings=json.loads(bundle.read('VickyReference/reference-settings.json'))
        assert settings=={'hotkey':'Ctrl+Alt+Shift+R','showSuccessNotification':True,'configured':False,'language':language}
        for name in executables:
            assert bundle.read('VickyReference/'+name)==(portable/name).read_bytes()
        for name in ['Localizer.cs','translations.json','build.ps1','ReferenceCapture.cs','VickyReference.cs']:
            assert bundle.read('VickyReference/source/'+name)==(portable/'source'/name).read_bytes()
        for name in ['README.zh-CN.md','README.en.md']:
            assert bundle.read('VickyReference/'+name)==(portable/name).read_bytes()
        assert bundle.read('VickyReference/README.md')==(portable/('README.'+language+'.md')).read_bytes()
with zipfile.ZipFile(root/'dist'/('VickyReference-update-'+version+'.zip')) as update:
    assert set(update.namelist())==set(executables+['LICENSE'])
    for name in executables:assert update.read(name)==(portable/name).read_bytes()
for line in (root/'dist/SHA256SUMS.txt').read_text().splitlines():
    digest,name=line.split('  ');assert hashlib.sha256((root/'dist'/name).read_bytes()).hexdigest()==digest
print('Both localized packages, bilingual binaries/source, fresh settings, upgrade contents, private paths and SHA256 hashes verified.')
