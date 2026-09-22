import { execFile } from 'node:child_process';
import { fileURLToPath } from 'node:url';

export class ReferenceError extends Error {
 readonly code: string;
 constructor(code: string, message: string) { super(message); this.code=code; }
}
export type ReferenceResult = { ok: true; app: string; count: number };
let inFlight=false;
export async function copyReference(): Promise<ReferenceResult> {
 if(inFlight) throw new ReferenceError('busy','A reference capture is already running');
 inFlight=true;
 try {
  const helper=fileURLToPath(new URL('../native/ReferenceCapture.exe',import.meta.url));
  return await new Promise<ReferenceResult>((resolve,reject)=>{
   execFile(helper,['--copy'],{windowsHide:true,timeout:8000,maxBuffer:65536,encoding:'utf8'},(error,stdout)=>{
    let result: {ok?:boolean;code?:string;message?:string;app?:string;count?:number};
    try {result=JSON.parse(stdout.trim());}
    catch {reject(new ReferenceError(error?.killed?'timeout':'helper_failed',error?.killed?'Application did not respond in time':'Reference helper did not return a result'));return;}
    if(error || !result.ok){reject(new ReferenceError(result.code || 'capture_failed',result.message || 'Cannot capture reference'));return;}
    if(typeof result.app!=='string' || !Number.isInteger(result.count) || result.count!<1){reject(new ReferenceError('helper_failed','Invalid capture result'));return;}
    resolve(result as ReferenceResult);
   });
  });
 } finally {inFlight=false;}
}
export function referenceErrorTitle(error:unknown): string {
 if(!(error instanceof ReferenceError)) return '复制失败';
 const titles: Record<string,string>={busy:'正在复制',timeout:'应用未响应',unsupported:'暂不支持',nothing_selected:'请先选文件',unsaved:'请先保存',focus_changed:'请重试',clipboard_changed:'请重试',clipboard_busy:'剪贴板忙',editor_unavailable:'路径不可用',browser_unavailable:'地址不可用',office_unavailable:'文档不可用',ambiguous:'无法确定文档'};
 return titles[error.code] || '复制失败';
}
