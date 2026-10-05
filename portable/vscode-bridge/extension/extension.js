const vscode=require('vscode');
const fs=require('node:fs');
const path=require('node:path');
function capture(editor){
 if(!editor || editor.document.isUntitled)throw Error('Save a file and focus its editor first');
 if(editor.document.uri.scheme!=='file')throw Error('Only local files are supported by this bridge');
 return editor.selections.map(selection=>{
  const start=selection.start.line+1;
  const end=selection.isEmpty?start:Math.max(start,selection.end.line+(selection.end.character===0?0:1));
  return {name:path.basename(editor.document.uri.fsPath),path:editor.document.uri.fsPath,startLine:start,endLine:end};
 });
}
function activate(context){
 context.subscriptions.push(vscode.commands.registerCommand('vickyReference.capture',()=>{
  const directory=path.join(process.env.LOCALAPPDATA,'VickyDeck','reference-bridge');
  const requestPath=path.join(directory,'request.json');
  let request;
  try{request=JSON.parse(fs.readFileSync(requestPath,'utf8').replace(/^\uFEFF/,''));}catch{return;}
  if(!/^[a-f0-9]{32}$/.test(request.id) || Math.abs(Date.now()-request.time)>6000 || !vscode.window.state.focused)return;
  let response;
  try{response={ok:true,items:capture(vscode.window.activeTextEditor)};}
  catch(error){response={ok:false,message:error.message};}
  const output=path.join(directory,request.id+'.json');
  fs.writeFileSync(output+'.tmp',JSON.stringify(response));fs.renameSync(output+'.tmp',output);
 }));
}
module.exports={activate,capture};
