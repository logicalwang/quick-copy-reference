using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Windows.Forms;

// Small, on-demand desktop helper. No listener, telemetry or background monitoring.
public static class ReferenceCapture {
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 1048576 };
    static IntPtr foreground;
    static string app;
    static uint initialSequence;
    static string strategy;
    public class Item { public string name { get; set; } public string path { get; set; } public string focus { get; set; } public string location { get; set; } public int startLine { get; set; } public int endLine { get; set; } }
    public class EditorReply { public bool ok {get;set;} public List<Item> items {get;set;} public string message {get;set;} }
    public class FormatRequest { public Item[] items { get; set; } }
    sealed class ReferenceError : Exception { public string Code; public ReferenceError(string code, string message) : base(message) { Code=code; } }

    [STAThread]
    public static int Main(string[] args) {
        try {
            if (args.Length != 1) throw new ReferenceError("usage", "Expected --copy, --collect or --format");
            if (args[0] == "--format") {
                var req=Json.Deserialize<FormatRequest>(new StreamReader(Console.OpenStandardInput(),Encoding.UTF8).ReadToEnd());
                Reply(new {ok=true,text=Format(req.items)}); return 0;
            }
            if(args[0] != "--copy" && args[0] != "--collect") throw new ReferenceError("usage", "Unknown command");
            foreground=GetForegroundWindow(); initialSequence=GetClipboardSequenceNumber();
            if(foreground==IntPtr.Zero) throw new ReferenceError("no_window", "No foreground window");
            uint pid; GetWindowThreadProcessId(foreground,out pid);
            app=Process.GetProcessById((int)pid).ProcessName.ToLowerInvariant();
            // Capture before browser address-bar focus can replace the document selection.
            string selectedText=(app!="code" && app!="code - insiders" && SupportsTextSelection(app)) ? ReadSelection() : null;
            var items=Collect();
            if(items.Count==1 && items[0].startLine==0 && string.IsNullOrWhiteSpace(items[0].focus)) items[0].focus=selectedText;
            var text=Format(items);
            EnsureForeground();
            if(args[0]=="--copy") {
                if(GetClipboardSequenceNumber()!=initialSequence) throw new ReferenceError("clipboard_changed", "Clipboard changed while reading the application; try again");
                WriteClipboard(text);
            }
            // Do not return paths/text to plugin logs during normal use.
            if(args[0]=="--collect") Reply(new {ok=true,app=app,items=items,text=text,strategy=strategy});
            else Reply(new {ok=true,app=app,count=items.Count});
            return 0;
        } catch(Exception ex) {
            var known=ex as ReferenceError;
            Reply(new {ok=false,code=known==null?"capture_failed":known.Code,message=known==null?"Cannot read current document in this application":ex.Message,app=app});
            return 1;
        }
    }
    static void Reply(object value) {
        var bytes=new UTF8Encoding(false).GetBytes(Json.Serialize(value)+"\n");
        using(var output=Console.OpenStandardOutput()) output.Write(bytes,0,bytes.Length);
    }
    static void EnsureForeground() { if(GetForegroundWindow()!=foreground) throw new ReferenceError("focus_changed","Focused window changed; press the button again"); }
    static List<Item> Collect() {
        if(app.StartsWith("markpad",StringComparison.OrdinalIgnoreCase)) return Markpad();
        switch(app) {
            case "explorer": return Explorer();
            case "notepad": return Notepad();
            case "winword": return Office("_WwG","word");
            case "excel": return Office("EXCEL7","excel");
            case "powerpnt": return Office("paneClassDC","powerpoint");
            case "chrome": case "msedge": case "brave": case "firefox": return Browser();
            case "code": case "code - insiders": return Vscode();
            case "cursor": return Editor();
            default: throw new ReferenceError("unsupported", "This application is not supported yet: "+app);
        }
    }
    static List<Item> Explorer() {
        dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
        dynamic windows=shell.Windows();
        for(int n=0;n<(int)windows.Count;n++) {
            dynamic w=windows.Item(n);
            if(new IntPtr((long)w.HWND)!=foreground) continue;
            var result=new List<Item>(); dynamic selected=w.Document.SelectedItems();
            for(int i=0;i<(int)selected.Count;i++) {
                dynamic item=selected.Item(i);
                result.Add(LocalItem((string)item.Path));
            }
            if(result.Count==0) throw new ReferenceError("nothing_selected","Select one or more files or folders in Explorer first");
            return result;
        }
        throw new ReferenceError("explorer_unavailable","Open a File Explorer folder and select a file first");
    }
    static List<Item> Office(string className,string kind) {
        var handles=new List<IntPtr>();
        EnumWindowsProc callback=delegate(IntPtr h,IntPtr ignored) {
            var cls=new StringBuilder(256);GetClassName(h,cls,cls.Capacity);
            if(cls.ToString()==className && IsWindowVisible(h)) handles.Add(h);
            return true;
        };
        EnumChildWindows(foreground,callback,IntPtr.Zero);
        var result=new List<Item>();
        foreach(var handle in handles) {
            object native; var iid=new Guid("00020400-0000-0000-C000-000000000046");
            if(AccessibleObjectFromWindow(handle,0xfffffff0,ref iid,out native)!=0 || native==null) continue;
            try {
                dynamic window=native; dynamic doc;
                if(kind=="word") doc=window.Document;
                else if(kind=="excel") doc=window.ActiveSheet.Parent;
                else doc=window.Presentation;
                string folder=(string)doc.Path;
                if(string.IsNullOrEmpty(folder)) throw new ReferenceError("unsaved","Save this document to a file first");
                string full=(string)doc.FullName;
                // Office cloud documents may expose a usable HTTPS URL instead of a local path.
                var reference=ToItem(full,(string)doc.Name);
                ReadOfficeSelection(window,kind,reference);
                result.Add(reference);
            } catch(ReferenceError) {throw;} catch {continue;}
        }
        result=result.GroupBy(i=>i.path,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
        if(result.Count==1) return result;
        if(result.Count>1) throw new ReferenceError("ambiguous","Multiple documents found in the foreground window");
        throw new ReferenceError("office_unavailable","Cannot read this Office document; close modal dialogs and try again");
    }
    static bool SupportsTextSelection(string process) {
        return process.StartsWith("markpad",StringComparison.OrdinalIgnoreCase) ||
            new[]{"notepad","winword","excel","powerpnt","chrome","msedge","brave","firefox","code","code - insiders","cursor"}.Contains(process);
    }
    const int FocusLimit=12000;
    static string ClipFocus(string value) {
        if(string.IsNullOrWhiteSpace(value)) return null;
        value=value.Replace("\r\n","\n").Replace('\r','\n').Replace("\0","").Replace("\a","").Trim();
        if(value.Length<=FocusLimit) return value;
        int length=FocusLimit;
        if(char.IsHighSurrogate(value[length-1])) length--;
        return value.Substring(0,length)+"\n[选区过长，已截断]";
    }
    static string SelectionFrom(AutomationElement element) {
        try {
            if(element.Current.IsPassword || element.Current.IsOffscreen) return null;
            object pattern;
            if(!element.TryGetCurrentPattern(TextPattern.Pattern,out pattern)) return null;
            var text=(TextPattern)pattern;
            if(text.SupportedTextSelection==SupportedTextSelection.None) return null;
            var parts=new List<string>(); int size=0;
            foreach(var range in text.GetSelection()) {
                // A caret is an empty selection, never use DocumentRange or ValuePattern.
                if(range.CompareEndpoints(TextPatternRangeEndpoint.Start,range,TextPatternRangeEndpoint.End)==0) continue;
                string value=range.GetText(FocusLimit+1-size);
                if(!string.IsNullOrWhiteSpace(value)){parts.Add(value);size+=value.Length;}
                if(size>FocusLimit) break;
            }
            return ClipFocus(string.Join("\n",parts));
        } catch(ElementNotAvailableException) {} catch(InvalidOperationException) {} catch(COMException) {}
        return null;
    }
    static string ReadSelection() {
        try {
            var focused=AutomationElement.FocusedElement;
            if(focused==null || focused.Current.IsPassword) return null;
            uint pid;GetWindowThreadProcessId(foreground,out pid);
            if(focused.Current.ProcessId!=(int)pid) {
                // Chromium document nodes may belong to a renderer process; verify ancestry instead.
                var ancestor=focused;bool belongs=false;
                for(int n=0;ancestor!=null && n<40;n++) {
                    if(ancestor.Current.NativeWindowHandle==foreground.ToInt32()){belongs=true;break;}
                    ancestor=TreeWalker.ControlViewWalker.GetParent(ancestor);
                }
                if(!belongs) return null;
            }
            // Address bars, find boxes and application search fields are not document selections.
            string id=focused.Current.AutomationId ?? "", name=focused.Current.Name ?? "";
            if(Regex.IsMatch(id+" "+name,@"omnibox|addressEditBox|urlbar|search|搜索|地址|网址|查找",RegexOptions.IgnoreCase)) return null;
            string direct=SelectionFrom(focused);if(direct!=null) return direct;
            // Find document providers, pruning their descendants to avoid duplicate ranges.
            var queue=new Queue<AutomationElement>();queue.Enqueue(AutomationElement.FromHandle(foreground));
            var timer=Stopwatch.StartNew();int visited=0;var selections=new List<string>();
            while(queue.Count>0 && visited++<700 && timer.ElapsedMilliseconds<1200) {
                var e=queue.Dequeue();
                if(e.Current.IsPassword) continue;
                if(e.Current.ControlType==ControlType.Document) {
                    var selected=SelectionFrom(e);if(selected!=null) selections.Add(selected);
                    continue;
                }
                var children=e.FindAll(TreeScope.Children,Condition.TrueCondition);
                foreach(AutomationElement child in children)queue.Enqueue(child);
            }
            selections=selections.Distinct().ToList();
            return selections.Count==1 ? selections[0] : null;
        } catch(ElementNotAvailableException) {} catch(InvalidOperationException) {} catch(COMException) {}
        return null;
    }
    static void ReadOfficeSelection(dynamic window,string kind,Item item) {
        // Use the native object from the foreground document, not a global active instance.
        try {
            if(kind=="word") {
                dynamic selection=window.Selection;
                if((int)selection.Start!=(int)selection.End) item.focus=ClipFocus((string)selection.Text);
            } else if(kind=="excel") {
                dynamic selection=window.Application.Selection;
                if((string)selection.Parent.Parent.FullName!=item.path) return;
                var pieces=new List<string>();var locations=new List<string>();int chars=0,cells=0;bool cut=false;
                foreach(dynamic area in selection.Areas) {
                    locations.Add((string)area.Address);
                    foreach(dynamic cell in area.Cells) {
                        if(++cells>200 || chars>FocusLimit){cut=true;break;}
                        string value=(string)cell.Text;
                        string line=(string)cell.Address+": "+value;
                        pieces.Add(line);chars+=line.Length;
                    }
                    if(cut)break;
                }
                item.location=(string)selection.Parent.Name+"!"+string.Join(", ",locations);
                item.focus=ClipFocus(string.Join("\n",pieces)+(cut?"\n[选区过大，仅包含前 200 个单元格或 12000 字符]":""));
            } else {
                dynamic selection=window.Selection;
                try {item.location="幻灯片 "+((int)window.View.Slide.SlideIndex).ToString();}catch{}
                int type=(int)selection.Type;
                if(type==3) item.focus=ClipFocus((string)selection.TextRange.Text);
                else if(type==2) {
                    var texts=new List<string>();
                    foreach(dynamic shape in selection.ShapeRange) {
                        if((int)shape.HasTextFrame!=0 && (int)shape.TextFrame.HasText!=0)texts.Add((string)shape.TextFrame.TextRange.Text);
                        if(texts.Sum(t=>t.Length)>FocusLimit)break;
                    }
                    item.focus=ClipFocus(string.Join("\n",texts));
                }
            }
        } catch { /* Optional selection must not prevent a valid source reference. */ }
    }
    static IEnumerable<AutomationElement> Elements(IntPtr handle,bool skipDocuments) {
        var root=AutomationElement.FromHandle(handle);
        var queue=new Queue<Tuple<AutomationElement,int>>();queue.Enqueue(Tuple.Create(root,0));
        var timer=Stopwatch.StartNew();int count=0;
        while(queue.Count>0 && count++<1800 && timer.ElapsedMilliseconds<2500) {
            var item=queue.Dequeue(); AutomationElement e=item.Item1;
            yield return e;
            if(item.Item2>=25) continue;
            try {
                if(skipDocuments && e.Current.ControlType==ControlType.Document) continue;
                var children=e.FindAll(TreeScope.Children,Condition.TrueCondition);
                for(int i=0;i<children.Count;i++) queue.Enqueue(Tuple.Create(children[i],item.Item2+1));
            } catch(ElementNotAvailableException) {}
        }
    }
    static List<Item> Browser() {
        var candidates=new List<string>();
        var addressElements=new List<AutomationElement>();
        foreach(var e in Elements(foreground,true)) {
            try {
                if((e.Current.ControlType!=ControlType.Edit && e.Current.ControlType!=ControlType.ComboBox) || e.Current.IsOffscreen) continue;
                string id=e.Current.AutomationId ?? "";string name=e.Current.Name ?? "";
                bool address=id=="addressEditBox" || id=="urlbar-input" || id=="omnibox" || Regex.IsMatch(name,@"address.*(search|bar)|search.*address|地址|网址|adresse|adressleiste",RegexOptions.IgnoreCase);
                if(!address) continue;
                object p;if(e.TryGetCurrentPattern(ValuePattern.Pattern,out p)) {
                    string value=((ValuePattern)p).Current.Value;
                    if(!string.IsNullOrWhiteSpace(value)){candidates.Add(value.Trim());addressElements.Add(e);}
                }
            } catch(ElementNotAvailableException) {}
        }
        candidates=candidates.Distinct().ToList();
        if(candidates.Count!=1) throw new ReferenceError("browser_unavailable","Cannot identify the browser address bar; finish editing the address and try again");
        string url=candidates[0];
        // Chromium elides the scheme until the omnibox has keyboard focus.
        // Read the actual expanded value, never assume HTTPS or touch the clipboard.
        if(!HasBrowserScheme(url)) {
            if(addressElements.Count!=1) throw new ReferenceError("browser_unavailable","Cannot identify a unique address control");
            var previous=AutomationElement.FocusedElement;
            var address=addressElements[0];
            bool moved=false;
            try {
                EnsureForeground();
                if(previous==null) throw new ReferenceError("browser_unavailable","Cannot preserve browser focus");
                address.SetFocus();moved=true;
                EnsureForeground(); SendKeys.SendWait("^l");
                for(int attempt=0;attempt<12;attempt++) {
                    EnsureForeground();
                    object pattern;
                    if(address.TryGetCurrentPattern(ValuePattern.Pattern,out pattern)) {
                        string expanded=((ValuePattern)pattern).Current.Value.Trim();
                        if(HasBrowserScheme(expanded)){url=expanded;break;}
                    }
                    Thread.Sleep(25);
                }
                if(!HasBrowserScheme(url)) url=CopyBrowserAddress(address);
            } finally {
                if(moved && GetForegroundWindow()==foreground) {
                    try {if(address.Current.HasKeyboardFocus) previous.SetFocus();} catch(ElementNotAvailableException){} catch(InvalidOperationException){}
                }
            }
            if(!HasBrowserScheme(url)) throw new ReferenceError("browser_unavailable","Browser did not expose the complete URL");
            strategy="focused-address";
        } else strategy="address-value";
        var title=WindowTitle(foreground);
        title=Regex.Replace(title,@"\s+[-–—]\s+(Google Chrome|Microsoft Edge|Mozilla Firefox|Brave)$","",RegexOptions.IgnoreCase);
        return new List<Item>{ToItem(url,title)};
    }
    static string CopyBrowserAddress(AutomationElement address) {
        EnsureForeground();
        if(!address.Current.HasKeyboardFocus)throw new ReferenceError("focus_changed","Address bar lost focus");
        if(GetClipboardSequenceNumber()!=initialSequence)throw new ReferenceError("clipboard_changed","Clipboard changed; retry");
        var original=Clipboard.GetDataObject();var backup=new DataObject();bool empty=original==null;
        // Materialize all formats before Chrome replaces the clipboard (no delayed reads).
        if(original!=null) foreach(string format in original.GetFormats(false)) {
            object value=original.GetData(format,false);
            if(value==null)throw new ReferenceError("clipboard_busy","Cannot preserve the current clipboard");
            var stream=value as MemoryStream;
            if(stream!=null)value=new MemoryStream(stream.ToArray());
            backup.SetData(format,false,value);
        }
        uint owned=0;
        try {
            EnsureForeground();
            if(GetClipboardSequenceNumber()!=initialSequence)throw new ReferenceError("clipboard_changed","Clipboard changed; retry");
            // Ctrl+L selects the entire canonical address. Ctrl+C uses Chrome's URL copy rules.
            SendKeys.SendWait("^l");EnsureForeground();SendKeys.SendWait("^c");
            for(int attempt=0;attempt<20;attempt++) {
                uint current=GetClipboardSequenceNumber();
                if(current!=initialSequence){owned=current;break;}
                EnsureForeground();Thread.Sleep(25);
            }
            if(owned==0)throw new ReferenceError("browser_unavailable","Browser did not copy its address");
            EnsureForeground();
            string value=Clipboard.GetText(TextDataFormat.UnicodeText).Trim();
            if(GetClipboardSequenceNumber()!=owned)throw new ReferenceError("clipboard_changed","Clipboard changed; retry");
            if(!HasBrowserScheme(value))throw new ReferenceError("browser_unavailable","Browser copied an unsupported address");
            return value;
        } finally {
            // Never overwrite a subsequent clipboard update from the user or another app.
            if(owned!=0 && GetClipboardSequenceNumber()==owned) {
                if(empty)Clipboard.Clear();else Clipboard.SetDataObject(backup,true,5,40);
                initialSequence=GetClipboardSequenceNumber();
            }
        }
    }
    static bool HasBrowserScheme(string value) {return Regex.IsMatch(value,@"^(https?://|file:/)",RegexOptions.IgnoreCase);}
    static List<Item> Notepad() {
        // New Notepad versions may expose the active tab path directly.
        try { return Editor(); } catch(ReferenceError) {}
        string title=WindowTitle(foreground);
        if(Regex.IsMatch(title,@"^(\*?)(Untitled|无标题|未命名|Sans titre)(\s|$)",RegexOptions.IgnoreCase))
            throw new ReferenceError("unsaved","Save this Notepad document first");
        // Classic Notepad exposes no file path API. Inspect Save As metadata, then cancel.
        // Never use the process command line: File > Open can change the active document.
        IntPtr owner=foreground, dialog=IntPtr.Zero;uint ownerPid;GetWindowThreadProcessId(owner,out ownerPid);
        EnsureForeground();SendKeys.SendWait("^+s");
        try {
            for(int attempt=0;attempt<25;attempt++) {
                Thread.Sleep(40);var candidate=GetForegroundWindow();uint pid;GetWindowThreadProcessId(candidate,out pid);
                var cls=new StringBuilder(128);GetClassName(candidate,cls,cls.Capacity);
                if(candidate!=owner && pid==ownerPid && cls.ToString()=="#32770"){dialog=candidate;break;}
                if(candidate!=owner)break;
            }
            if(dialog==IntPtr.Zero)throw new ReferenceError("editor_unavailable","Notepad did not expose a Save As path");
            string filename=null;var folders=new List<string>();
            foreach(var element in Elements(dialog,true)) {
                string id=element.Current.AutomationId ?? "";
                if(id=="1001" && element.Current.ControlType==ControlType.Edit) {
                    object pattern;if(element.TryGetCurrentPattern(ValuePattern.Pattern,out pattern))filename=((ValuePattern)pattern).Current.Value;
                }
                if(element.Current.ControlType==ControlType.ToolBar) {
                    string name=element.Current.Name ?? "";
                    int separator=name.IndexOf(':');
                    if(separator>=0) {
                        string path=name.Substring(separator+1).Trim();
                        if(Regex.IsMatch(path,@"^(?:[A-Za-z]:[\\/]|\\\\)") && Directory.Exists(path))folders.Add(path);
                    }
                }
            }
            if(string.IsNullOrWhiteSpace(filename))throw new ReferenceError("editor_unavailable","Save As did not expose a filename");
            Item item=TryLocalMetadata(filename);
            if(item==null) {
                folders=folders.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if(folders.Count!=1)throw new ReferenceError("editor_unavailable","Save As did not expose a unique folder");
                item=TryLocalMetadata(Path.Combine(folders[0],filename));
            }
            if(item==null)throw new ReferenceError("unsaved","Save this document before copying its reference");
            strategy="notepad-save-dialog";return new List<Item>{item};
        } finally {
            // Only cancel the exact dialog we opened, never send Escape to another app.
            if(dialog!=IntPtr.Zero && GetForegroundWindow()==dialog) {
                SendKeys.SendWait("{ESC}");
                for(int i=0;i<20 && GetForegroundWindow()==dialog;i++)Thread.Sleep(25);
            }
        }
    }
    static List<Item> Vscode() {
        string directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CopyReference","reference-bridge");
        Directory.CreateDirectory(directory);
        string id=Guid.NewGuid().ToString("N"),request=Path.Combine(directory,"request.json"),response=Path.Combine(directory,id+".json");
        double time=(DateTime.UtcNow-new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalMilliseconds;
        File.WriteAllText(request,Json.Serialize(new{id=id,time=time}),new UTF8Encoding(false));
        try {
            EnsureForeground();SendKeys.SendWait("^%+{F12}");
            for(int i=0;i<100;i++) {
                EnsureForeground();
                if(File.Exists(response)) {
                    var reply=Json.Deserialize<EditorReply>(File.ReadAllText(response));
                    if(!reply.ok)throw new ReferenceError("editor_unavailable",reply.message);
                    if(reply.items==null || reply.items.Count==0)throw new ReferenceError("editor_unavailable","No active editor selection");
                    foreach(var item in reply.items) {
                        LocalItem(item.path);
                        if(item.startLine<1 || item.endLine<item.startLine)throw new ReferenceError("editor_unavailable","Invalid editor line range");
                    }
                    strategy="vscode-extension";return reply.items;
                }
                Thread.Sleep(40);
            }
            throw new ReferenceError("editor_unavailable","Activate Copy Reference in VS Code and focus the text editor, then retry");
        } finally {
            try{if(File.Exists(response))File.Delete(response);}catch{}
            try{if(File.Exists(request) && File.ReadAllText(request).Contains(id))File.Delete(request);}catch{}
        }
    }
    static List<Item> Editor() {
        var selected=new List<Item>();
        // Read selected tabs' full-path metadata; never infer a path from a basename.
        foreach(var e in Elements(foreground,true)) {
            try {
                if(e.Current.ControlType!=ControlType.TabItem) continue;
                object pattern;
                if(!e.TryGetCurrentPattern(SelectionItemPattern.Pattern,out pattern) || !((SelectionItemPattern)pattern).Current.IsSelected) continue;
                foreach(var value in new[]{e.Current.HelpText,e.Current.Name}) {
                    var item=TryLocalMetadata(value);if(item!=null){selected.Add(item);break;}
                }
            } catch(ElementNotAvailableException) {}
        }
        selected=selected.GroupBy(i=>i.path,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
        if(selected.Count==1) return selected;
        throw new ReferenceError("editor_unavailable","Current editor tab does not expose an absolute path");
    }
    static List<Item> Markpad() {
        var candidates=new List<Item>();var active=new List<Item>();
        foreach(var e in Elements(foreground,false)) {
            try {
                // MarkPad 2.6.11 puts the full path in the tab group's title attribute.
                if(e.Current.ControlType!=ControlType.Group && e.Current.ControlType!=ControlType.TabItem) continue;
                if(e.Current.IsOffscreen) continue;
                var item=TryLocalMetadata(e.Current.HelpText) ?? TryLocalMetadata(e.Current.Name);
                if(item==null) continue;
                candidates.Add(item);
                object pattern;
                bool selected=e.TryGetCurrentPattern(SelectionItemPattern.Pattern,out pattern) && ((SelectionItemPattern)pattern).Current.IsSelected;
                string cls=e.Current.ClassName ?? "";
                if(selected || Regex.IsMatch(cls,@"\btab\b.*\bactive\b"))active.Add(item);
            } catch(ElementNotAvailableException) {}
        }
        active=active.GroupBy(i=>i.path,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
        candidates=candidates.GroupBy(i=>i.path,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
        if(active.Count==1){strategy="active-tab";return active;}

        throw new ReferenceError("editor_unavailable","MarkPad did not expose an unambiguous current file path");
    }
    static Item TryLocalMetadata(string value) {
        if(string.IsNullOrWhiteSpace(value)) return null;
        foreach(string part in value.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries)) {
            try { return LocalItem(part.Trim()); } catch { }
        }
        return null;
    }
    static Item LocalItem(string value) {
        if(string.IsNullOrEmpty(value) || !Regex.IsMatch(value,@"^(?:[A-Za-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+)")) throw new ReferenceError("not_local","No absolute file path is available");
        var full=Path.GetFullPath(value);
        if(!File.Exists(full) && !Directory.Exists(full)) throw new ReferenceError("missing_file","The referenced file is not available on this computer");
        return new Item{name=Path.GetFileName(full.TrimEnd('\\','/')),path=full};
    }
    static Item ToItem(string value,string name) {
        if(Regex.IsMatch(value,@"^(?:[A-Za-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+)")) return LocalItem(value);
        Uri uri;
        if(Uri.TryCreate(value,UriKind.Absolute,out uri)) {
            if(uri.IsFile) return LocalItem(uri.LocalPath);
            if(uri.Scheme=="https" || uri.Scheme=="http") return new Item{name=string.IsNullOrWhiteSpace(name)?uri.Host:name,path=value};
        }
        return LocalItem(value);
    }
    public static string Format(IEnumerable<Item> items) {
        if(items==null) throw new ReferenceError("empty","No references found");
        var lines=new List<string>();var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var item in items) {
            if(item==null || string.IsNullOrWhiteSpace(item.path)) throw new ReferenceError("invalid_reference","Empty reference path");
            string destination=item.path;
            bool local=Regex.IsMatch(destination,@"^(?:[A-Za-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+)");
            Uri uri;
            if(local) destination=destination.Replace('\\','/');
            else if(!Uri.TryCreate(destination,UriKind.Absolute,out uri) || (uri.Scheme!="http" && uri.Scheme!="https")) throw new ReferenceError("invalid_reference","Expected a full file path or HTTP(S) URL");
            if(destination.Any(char.IsControl)) throw new ReferenceError("invalid_reference","Invalid characters in reference");
            // Angle destinations preserve whitespace and parentheses. Local # and % must remain literal paths for Codex.
            destination=destination.Replace("<","%3C").Replace(">","%3E");
            string label=string.IsNullOrWhiteSpace(item.name)?Path.GetFileName(item.path.TrimEnd('\\','/')):item.name;
            if(item.startLine>0) {
                if(!local || item.endLine<item.startLine)throw new ReferenceError("invalid_reference","Invalid source line range");
                destination+=":"+item.startLine;
                label=(item.endLine==item.startLine?item.startLine.ToString():item.startLine+"–"+item.endLine)+" (line "+item.startLine+")";
            }
            // Keep context inside the visible link label; the destination remains a real source.
            if(!string.IsNullOrWhiteSpace(item.location)) label+=": "+item.location;
            string focus=ClipFocus(item.focus);
            if(focus!=null) label+=": "+focus;
            label=Regex.Replace(label,@"[\s\p{Cc}]+"," ").Trim();
            label=label.Replace("\\","\\\\").Replace("[","\\[").Replace("]","\\]").Replace("<","&lt;").Replace(">","&gt;").Replace("\x60","\\\x60");
            string line="["+label+"](<"+destination+">)";
            if(seen.Add(line)) lines.Add(line);
        }
        if(lines.Count==0) throw new ReferenceError("empty","No references found");
        return string.Join(" ",lines);
    }
    static void WriteClipboard(string text) {
        for(int n=0;n<5;n++) {
            EnsureForeground();
            if(GetClipboardSequenceNumber()!=initialSequence) throw new ReferenceError("clipboard_changed","Clipboard changed; try again");
            try {Clipboard.SetText(text,TextDataFormat.UnicodeText);return;} catch(ExternalException){Thread.Sleep(40);}
        }
        throw new ReferenceError("clipboard_busy","Clipboard is busy; try again");
    }
    static string WindowTitle(IntPtr h) {var s=new StringBuilder(4096);GetWindowText(h,s,s.Capacity);return s.ToString();}
    delegate bool EnumWindowsProc(IntPtr hwnd,IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
    [DllImport("user32.dll")] static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h,EnumWindowsProc callback,IntPtr data);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder name,int size);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder text,int size);
    [DllImport("oleacc.dll")] static extern int AccessibleObjectFromWindow(IntPtr h,uint id,ref Guid iid,[MarshalAs(UnmanagedType.IDispatch)] out object obj);
}

