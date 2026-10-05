// Used only by the successful tag-build job. Credentials stay in memory.
import fs from 'node:fs';
import {createHash} from 'node:crypto';
const version=fs.readFileSync('portable/source/VickyReference.cs','utf8').match(/Version="([0-9.]+)"/)[1];
const tag=process.env.RELEASE_TAG,repository=process.env.RELEASE_REPOSITORY,commit=process.env.RELEASE_COMMIT;
if(tag!=='v'+version || !/^[\w.-]+\/[\w.-]+$/.test(repository??'') || !/^[a-f0-9]{40}$/.test(commit??'') || !process.env.GH_TOKEN)throw Error('Invalid release context');
const names=[`VickyReference-portable-${version}-zh-CN.zip`,`VickyReference-portable-${version}-en.zip`,`VickyReference-update-${version}.zip`,'vicky-reference-0.1.0.vsix','SHA256SUMS.txt'];
const expected=new Map(fs.readFileSync('dist/SHA256SUMS.txt','utf8').trim().split(/\r?\n/).map(line=>{const [hash,name]=line.split('  ');return [name,hash];}));
const digest=data=>'sha256:'+createHash('sha256').update(data).digest('hex');
const assets=new Map(names.map(name=>[name,fs.readFileSync('dist/'+name)]));
if(expected.size!==4 || [...expected.keys()].some(name=>!names.includes(name)))throw Error('Unexpected checksum manifest');
for(const [name,hash] of expected)if(digest(assets.get(name))!=='sha256:'+hash)throw Error('Asset checksum mismatch: '+name);
async function request(url,method='GET',body,type='application/json'){
 const parsed=new URL(url);if(!['api.github.com','uploads.github.com'].includes(parsed.hostname))throw Error('Unexpected credential destination');
 const response=await fetch(url,{method,headers:{Authorization:'Bearer '+process.env.GH_TOKEN,Accept:'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28',...(body!==undefined?{'Content-Type':type}:{})},body:body===undefined?undefined:Buffer.isBuffer(body)?body:JSON.stringify(body),redirect:'error',signal:AbortSignal.timeout(30000)});
 if(response.status===404)return null;
 if(!response.ok)throw Error(`GitHub ${method} ${parsed.pathname}: HTTP ${response.status}`);
 return response.json();
}
const api='https://api.github.com/repos/'+repository;
const ref=await request(api+'/git/ref/tags/'+tag);
if(!ref || ref.object.type!=='commit' || ref.object.sha!==commit)throw Error('Release tag does not point to the checked commit');
let release=await request(api+'/releases/tags/'+tag);
if(!release)release=(await request(api+'/releases?per_page=100')).find(r=>r.tag_name===tag && r.draft);
if(release && !release.draft){
 if(release.assets.length!==names.length || names.some(name=>!release.assets.some(a=>a.name===name && a.state==='uploaded' && a.digest===digest(assets.get(name)))))throw Error('Existing public release differs from the checked build');
 console.log('Matching Release is already public: '+release.html_url);
}else{
 if(!release)release=await request(api+'/releases','POST',{tag_name:tag,target_commitish:commit,name:`Copy Reference Portable v${version} · 简体中文 / English`,body:fs.readFileSync(`RELEASE-v${version}.md`,'utf8'),draft:true,prerelease:false});
 for(const name of names){
  const data=assets.get(name);let asset=release.assets.find(a=>a.name===name);
  if(!asset)asset=await request(release.upload_url.split('{')[0]+'?name='+encodeURIComponent(name),'POST',data,name.endsWith('.txt')?'text/plain':'application/zip');
  if(asset.state!=='uploaded' || asset.size!==data.length || asset.digest!==digest(data))throw Error('Upload verification failed: '+name);
  console.log('Verified '+name);
 }
 release=await request(api+'/releases/'+release.id);
 if(release.assets.length!==names.length)throw Error('Incomplete Release');
 release=await request(api+'/releases/'+release.id,'PATCH',{draft:false,make_latest:'true'});
 if(release.draft || !release.published_at)throw Error('Publication was not confirmed');
 console.log('Published checked Release: '+release.html_url);
}
