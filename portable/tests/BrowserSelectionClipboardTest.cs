using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

class BrowserSelectionClipboardTest {
    const BindingFlags Private=BindingFlags.Static|BindingFlags.NonPublic;
    const string Selected="Selected PDF passage: 中文 focus, not just appendix.pdf";
    static uint baseline;
    static object Invoke(string name,params object[] args){try{return typeof(ReferenceCapture).GetMethod(name,Private).Invoke(null,args);}catch(TargetInvocationException error){throw error.InnerException;}}
    static void Field(string name,object value){typeof(ReferenceCapture).GetField(name,Private).SetValue(null,value);}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Reset(){Field("initialSequence",GetClipboardSequenceNumber());}
    static string Probe(Action copy,Action verify,Func<bool> owns,bool required){return (string)Invoke("ProbeCopiedText",copy,verify,owns,required);}
    static byte[] Html(){return (byte[])Invoke("ReadClipboardHtmlBytes");}
    static DataObject Snapshot(){var result=new DataObject();var original=Clipboard.GetDataObject();if(original!=null)foreach(string format in original.GetFormats(false)){object value=format==DataFormats.Html?(object)new MemoryStream(Html()):original.GetData(format,false);if(value is MemoryStream)value=new MemoryStream(((MemoryStream)value).ToArray());result.SetData(format,false,value);}return result;}
    class PdfSurface:Control {
        public PdfSurface(){SetStyle(ControlStyles.Selectable,true);TabStop=true;AccessibleRole=AccessibleRole.Document;AccessibleName="Research PDF document";}
        protected override bool ProcessCmdKey(ref Message message,Keys key){if(key==(Keys.Control|Keys.C)){Clipboard.SetText(Selected);return true;}return base.ProcessCmdKey(ref message,key);}
    }
    [STAThread] static int Main(){var backup=Snapshot();baseline=GetClipboardSequenceNumber();try{
        Application.EnableVisualStyles();string[] args=new string[0];Localizer.Initialize(AppDomain.CurrentDomain.BaseDirectory,ref args);
        Check(!ReferenceCapture.IsBrowserChromeField(false,"","Research PDF document"),"Research title treated as a search field");
        Check(ReferenceCapture.IsBrowserChromeField(true,"omnibox",""),"Address field accepted");
        Check(ReferenceCapture.IsBrowserChromeField(true,"","Find in document"),"Find field accepted");
        var original=new DataObject();original.SetText("Prior clipboard 中文");original.SetData(DataFormats.Html,false,new MemoryStream(Encoding.UTF8.GetBytes("<b>原剪贴板</b>\0")));original.SetData("QuickReferenceTest",false,"unchanged custom format");Clipboard.SetDataObject(original,true);
        var html=Html();Reset();
        string selected=Probe(()=>Clipboard.SetText(Selected),()=>{},()=>true,false);
        Check(selected==Selected,"PDF copied selection missing");
        string reference=ReferenceCapture.Format(new[]{new ReferenceCapture.Item{path="C:/Example/appendix.pdf",focus=selected}});
        Check(reference.Contains("appendix.pdf: "+Selected) && !reference.Contains("\n"),"Copied PDF passage was not included in the inline reference");
        Check(Clipboard.GetText()=="Prior clipboard 中文","Original text not restored");
        Check(Convert.ToBase64String(Html())==Convert.ToBase64String(html),"Original UTF-8 HTML not restored");
        Check((string)Clipboard.GetData("QuickReferenceTest")=="unchanged custom format","Custom format lost");
        Reset();Check(Probe(()=>{},()=>{},()=>true,false)==null,"Stale clipboard treated as a selection");
        Check(Clipboard.GetText()=="Prior clipboard 中文","Empty selection changed clipboard");
        Reset();bool failed=false;try{Probe(()=>{Clipboard.SetText(Selected);throw new Exception("fixture copy failure");},()=>{},()=>true,false);}catch{failed=true;}
        Check(failed && Clipboard.GetText()=="Prior clipboard 中文","Copy failure did not restore clipboard");
        Reset();failed=false;try{Probe(()=>Clipboard.SetText("Concurrent update"),()=>{},()=>false,false);}catch{failed=true;}
        Check(failed && Clipboard.GetText()=="Concurrent update","Foreign update was overwritten");
        Reset();int checks=0;failed=false;try{Probe(()=>Clipboard.SetText(Selected),()=>{if(++checks==3)Clipboard.SetText("Later update");},()=>true,false);}catch{failed=true;}
        Check(failed && Clipboard.GetText()=="Later update","Subsequent clipboard update was overwritten");
        Reset();Check(Probe(()=>Clipboard.SetText("https://example.com/a.pdf?sig=a%2Fb"),()=>{},()=>true,true)=="https://example.com/a.pdf?sig=a%2Fb","Address probe changed a signed URL");
        Check(Clipboard.GetText()=="Later update","Address probe did not restore clipboard");
        Exception failure=null;bool completed=false;
        using(var form=new Form{Text="Quick Reference PDF selection test",Width=460,Height=160}){
            var surface=new PdfSurface{Dock=DockStyle.Fill};form.Controls.Add(surface);
            form.Shown+=(s,e)=>form.BeginInvoke(new Action(()=>{try{
                form.Activate();SetForegroundWindow(form.Handle);surface.Select();surface.Focus();Application.DoEvents();
                Field("foreground",form.Handle);Field("app",Process.GetCurrentProcess().ProcessName);
                Reset();
                Check(Probe(()=>{SendKeys.SendWait("^c");Application.DoEvents();},()=>{Application.DoEvents();Check(GetForegroundWindow()==form.Handle && surface.Focused,"Fixture focus changed");},()=>((bool)Invoke("BrowserOwnsClipboard")),false)==Selected,"Focused PDF copy fallback failed");
                Check(Clipboard.GetText()=="Later update","Real copy fallback did not restore original clipboard");completed=true;
            }catch(Exception error){failure=error;}finally{form.Close();}}));
            Application.Run(form);
        }
        if(failure!=null)throw failure;Check(completed,"PDF fixture did not complete");
        Console.WriteLine("PDF clipboard fallback: real focused copy, Unicode/HTML/custom restoration, no selection, failed copy, concurrent updates, signed URL and chrome-field guards passed.");return 0;
    }catch(Exception error){Console.WriteLine(error.ToString());return 1;}finally{
        IntPtr owner=GetClipboardOwner();uint pid;GetWindowThreadProcessId(owner,out pid);
        if(pid==(uint)Process.GetCurrentProcess().Id || GetClipboardSequenceNumber()==baseline)Clipboard.SetDataObject(backup,true);
    }}
    [DllImport("user32.dll")] static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")] static extern IntPtr GetClipboardOwner();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr handle,out uint pid);
}
