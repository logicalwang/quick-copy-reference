import {test} from 'node:test';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
function invoke(name,args,input){const r=spawnSync(path.join(root,name),args,{input,encoding:'utf8',windowsHide:true,timeout:10000});assert.equal(r.error,undefined);assert.equal(r.status,0,r.stdout+r.stderr);return JSON.parse(r.stdout);}
function format(items,language='zh-CN'){return invoke('ReferenceCapture.exe',['--format','--language='+language],JSON.stringify({items}));}
test('default configuration asks for a shortcut and contains no saved user preferences',()=>{
 assert.deepEqual(JSON.parse(fs.readFileSync(path.join(root,'reference-settings.json'),'utf8')),{hotkey:'Ctrl+Alt+Shift+R',showSuccessNotification:true,configured:false,language:'auto'});
});
test('portable shortcut parsing, clipboard-shortcut protection and actual registration probe',()=>{
 for(const name of ['QuickCopyReference.exe','VickyReference.exe'])for(const language of ['zh-CN','en']){const reply=invoke(name,['--self-test','--language='+language]);assert.equal(reply.ok,true);assert.equal(reply.version,'1.0.6');assert.equal(reply.language,language);assert.equal(reply.product,language==='en'?'Quick Copy Reference':'快速复制引用');assert.ok(reply.protectedShortcut.includes('Ctrl+C'));assert.equal(reply.menuLabel,language==='en'?'View latest error':'查看最近错误');assert.equal(reply.successMessage,language==='en'?'Reference copied. Ready to paste.':'已复制引用，可以粘贴了');}
});
test('English truncation marker is localized while Chinese document text stays intact',()=>{
 const result=format([{name:'文档.md',path:'C:/Example/文档.md',focus:'中文'.repeat(6500)}],'en');assert.ok(result.text.includes('中文'));assert.ok(result.text.includes('selection truncated'));assert.ok(!result.text.includes('选区过长'));
});
test('Chinese paths and focus stay readable, preserving literal filename underscores',()=>{
 const result=format([{name:'Q2_协同分析.md',path:'C:/Example/OneDrive - Example/资料/Q2_协同分析.md',focus:'三种量'}]);
 assert.ok(result.text.includes('Q2_协同分析.md: 三种量'));assert.ok(!result.text.includes('%E'));assert.ok(result.htmlFragment.includes('C:/Example/OneDrive - Example/资料/Q2_协同分析.md'));
});
test('PowerPoint selected text and slide numbers are included in a single reference',()=>{
 const result=format([{name:'deck.pptx',path:'C:/Example/deck.pptx',location:'幻灯片 2',focus:'first\nsecond'}]);
 assert.ok(result.text.includes('deck.pptx: 幻灯片 2: first second'));assert.ok(!/[\r\n]/.test(result.text));assert.ok(result.htmlFragment.includes('幻灯片 2: first second'));
});
test('VS Code range remains a clickable start line',()=>{
 const result=format([{name:'notes.tex',path:'C:/Example/Research Notes/notes.tex',startLine:757,endLine:770}]);
 assert.ok(result.text.includes('757–770 (line 757)'));assert.ok(result.text.includes('notes.tex:757'));assert.ok(result.htmlFragment.includes('notes.tex:757'));
});
test('multi-file output is inline, keeps both sources and removes duplicates',()=>{
 const first={path:'C:/Example/a.md'},second={path:'C:/Example/b.pdf'};const result=format([first,second,first]);
 assert.ok(!/[\r\n]/.test(result.text));assert.equal((result.htmlFragment.match(/href=/g)||[]).length,2);
});
test('signed web URLs preserve query escapes',()=>{
 const url='https://example.com/doc?sig=a%2Fb%2Bc%3D&name=Notes+2026#section';
 assert.ok(format([{name:'page',path:url}]).htmlFragment.includes('sig=a%2Fb%2Bc%3D&amp;name=Notes+2026#section'));
});
