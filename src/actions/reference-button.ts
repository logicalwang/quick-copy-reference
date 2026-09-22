import streamDeck, { action, SingletonAction, type KeyDownEvent, type WillAppearEvent } from '@elgato/streamdeck';
import { copyReference, referenceErrorTitle } from '../copy-reference';
@action({UUID:'dev.vicky.copyreference.reference'})
export class ReferenceButton extends SingletonAction {
 private busy=new Set<string>();
 private resets=new Map<string,ReturnType<typeof setTimeout>>();
 override async onWillAppear(ev:WillAppearEvent):Promise<void> {if(ev.action.isKey()) await ev.action.setTitle('');}
 override async onKeyDown(ev:KeyDownEvent):Promise<void> {
  if(this.busy.has(ev.action.id))return;
  this.busy.add(ev.action.id);clearTimeout(this.resets.get(ev.action.id));
  try {
   await ev.action.setTitle('读取中…');
   const result=await copyReference();
   streamDeck.logger.info('Copied references: app='+result.app+', count='+result.count);
   await ev.action.setTitle(result.count>1?'已复制 '+result.count+' 项':'已复制');
   await ev.action.showOk();
  }catch(error){streamDeck.logger.warn(error);await ev.action.setTitle(referenceErrorTitle(error));await ev.action.showAlert();}
  finally{
   this.busy.delete(ev.action.id);
   const timer=setTimeout(()=>{this.resets.delete(ev.action.id);void ev.action.setTitle('').catch(()=>{});},1800);
   timer.unref();this.resets.set(ev.action.id,timer);
  }
 }
}
