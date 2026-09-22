import {test} from 'node:test';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import path from 'node:path';
const helper=path.resolve('dev.vicky.copyreference.sdPlugin/native/ReferenceCapture.exe');
function format(items){const r=spawnSync(helper,['--format'],{input:JSON.stringify({items}),encoding:'utf8',windowsHide:true,timeout:3000});assert.equal(r.error,undefined);return JSON.parse(r.stdout);}
test('reference: Unicode, spaces, parentheses and brackets remain usable Markdown',()=>{
 const r=format([{name:'报告 [终稿] (1).docx',path:'F:\\资料目录\\报告 [终稿] (1).docx'}]);
 assert.equal(r.ok,true);assert.equal(r.text,'[报告 \\[终稿\\] (1).docx](<F:/资料目录/报告 [终稿] (1).docx>)');
});
test('reference: multiple files are inline and duplicates removed',()=>{
 const one={name:'a.md',path:'F:\\a.md'},two={name:'b.pdf',path:'F:\\b.pdf'};
 assert.equal(format([one,two,one]).text,'[a.md](<F:/a.md>) [b.pdf](<F:/b.pdf>)');
});
test('reference: UNC paths and web URLs',()=>{
 assert.equal(format([{name:'x.md',path:'\\\\server\\share\\x.md'}]).text,'[x.md](<//server/share/x.md>)');
 assert.equal(format([{name:'A\nB [x]',path:'https://example.com/a?q=(1)#section'}]).text,'[A B \\[x\\]](<https://example.com/a?q=(1)#section>)');
});
test('reference: reject empty, relative, control-character and non-web links',()=>{
 for(const items of [[],[{path:'file.md'}],[{path:'javascript:alert(1)'}],[{path:'https://example.com/\nwrong'}],[{path:''}]])assert.equal(format(items).ok,false);
});

test('focus: multiline selection becomes a single visible link label',()=>{
 assert.equal(format([{name:'page',path:'https://example.com',focus:' first\r\nsecond\nthird '}]).text,'[page: first second third](<https://example.com>)');
});
test('focus: location survives without selection and sources remain associated',()=>{
 assert.equal(format([{name:'a.xlsx',path:'F:\\a.xlsx',location:'Sheet1!$A$1',focus:'$A$1: 42'},{name:'b.pptx',path:'F:\\b.pptx',location:'幻灯片 3'}]).text,'[a.xlsx: Sheet1!$A$1: $A$1: 42](<F:/a.xlsx>) [b.pptx: 幻灯片 3](<F:/b.pptx>)');
});
test('focus: quote text cannot close the label or create an HTML tag',()=>{
 const text=format([{name:'page',path:'https://example.com/a?q=(1)#x',focus:'](https://wrong.test) <b> [x]'}]).text;
 assert.equal(text,'[page: \\](https://wrong.test) &lt;b&gt; \\[x\\]](<https://example.com/a?q=(1)#x>)');
});
test('focus: whitespace, controls and Unicode separators never create extra lines',()=>{
 const text=format([{name:'a\nfile',path:'F:\\a.md',location:'line\t12',focus:'A\u2028B\u2029C\u0001D\u00a0E'}]).text;
 assert.equal(text,'[a file: line 12: A B C D E](<F:/a.md>)');
});
test('focus: empty and long selections retain predictable output',()=>{
 assert.equal(format([{name:'a',path:'F:\\a',focus:' \r\n '}]).text,'[a](<F:/a>)');
 const text=format([{name:'a',path:'F:\\a',focus:'x'.repeat(12001)}]).text;
 assert.ok(text.includes('选区过长，已截断'));assert.ok(text.length<12100);assert.ok(!/[\r\n]/.test(text));
});
test('VS Code: range label and clickable start line preserve spaced paths',()=>{
 assert.equal(format([{name:'Section HHP.tex',path:'F:\\Research Notes\\Section HHP.tex',startLine:757,endLine:770}]).text,'[757–770 (line 757)](<F:/Research Notes/Section HHP.tex:757>)');
 assert.equal(format([{path:'F:\\a.tex',startLine:3,endLine:3}]).text,'[3 (line 3)](<F:/a.tex:3>)');
 assert.equal(format([{path:'F:\\a.tex',startLine:4,endLine:2}]).ok,false);
});
