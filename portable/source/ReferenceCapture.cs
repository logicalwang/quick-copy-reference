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
    static readonly List<string> diagnostics=new List<string>();
    public class Item { public string name { get; set; } public string path { get; set; } public string focus { get; set; } public string location { get; set; } public int startLine { get; set; } public int endLine { get; set; } }
    public class EditorReply { public bool ok {get;set;} public List<Item> items {get;set;} public string message {get;set;} }
    public class FormatRequest { public Item[] items { get; set; } }
    public class ReferencePayload { public string text {get;set;} public string htmlFragment {get;set;} public string clipboardHtml {get;set;} }
    sealed class ReferenceError : Exception { public string Code; public ReferenceError(string code, string message) : base(message) { Code=code; } }

    [STAThread]
    public static int Main(string[] args) {
        try {
            if (args.Length != 1) throw new ReferenceError("usage", "Expected --copy, --collect, --diagnose or --format");
            if (args[0] == "--format") {
                var req=Json.Deserialize<FormatRequest>(new StreamReader(Console.OpenStandardInput(),Encoding.UTF8).ReadToEnd());
                var payload=BuildReference(req.items);
                Reply(new {ok=true,text=payload.text,htmlFragment=payload.htmlFragment,clipboardHtml=payload.clipboardHtml}); return 0;
            }
            if(args[0] != "--copy" && args[0] != "--collect" && args[0] != "--diagnose") throw new ReferenceError("usage", "Unknown command");
            foreground=GetForegroundWindow(); initialSequence=GetClipboardSequenceNumber();
            if(foreground==IntPtr.Zero) throw new ReferenceError("no_window", "No foreground window");
            uint pid; GetWindowThreadProcessId(foreground,out pid);
            app=Process.GetProcessById((int)pid).ProcessName.ToLowerInvariant();
            // Capture before browser address-bar focus can replace the document selection.
            // Office has its own selection API. A generic UIA tree scan can block on
            // providers and must not prevent a valid document reference.
            string selectedText=(args[0]!="--diagnose" && app!="code" && app!="code - insiders" && app!="winword" && app!="excel" && app!="powerpnt" && SupportsTextSelection(app)) ? ReadSelection() : null;
            var items=Collect();
            if(args[0]=="--diagnose"){EnsureForeground();Reply(new{ok=true,app=app,count=items.Count,strategy=strategy,diagnostics=diagnostics.ToArray()});return 0;}
            if(items.Count==1 && items[0].startLine==0 && string.IsNullOrWhiteSpace(items[0].focus)) items[0].focus=selectedText;
            var reference=BuildReference(items);
            EnsureForeground();
            if(args[0]=="--copy") {
                if(GetClipboardSequenceNumber()!=initialSequence) throw new ReferenceError("clipboard_changed", "Clipboard changed while reading the application; try again");
                WriteClipboard(reference);
            }
            // Do not return paths/text to plugin logs during normal use.
            if(args[0]=="--collect") Reply(new {ok=true,app=app,items=items,text=reference.text,strategy=strategy});
            else Reply(new {ok=true,app=app,count=items.Count,strategy=strategy});
            return 0;
        } catch(Exception ex) {
            var known=ex as ReferenceError;
            if(known==null)RecordFailure("capture",ex);
            Reply(new {ok=false,code=known==null?"capture_failed":known.Code,message=known==null?"Cannot read current document in this application":ex.Message,app=app,diagnostics=diagnostics.ToArray()});
            return 1;
        }
    }
    static void Reply(object value) {
        var bytes=new UTF8Encoding(false).GetBytes(Json.Serialize(value)+"\n");
        using(var output=Console.OpenStandardOutput()) output.Write(bytes,0,bytes.Length);
    }
    static void EnsureForeground() { if(GetForegroundWindow()!=foreground) throw new ReferenceError("focus_changed","Focused window changed; press the button again"); }
    static void RecordFailure(string stage,Exception error){if(diagnostics.Count<12)diagnostics.Add(stage+":"+error.GetType().Name+":0x"+error.HResult.ToString("X8"));}
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
            case "notepad++": case "typora": case "marktext": return EditorDocument();
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
        diagnostics.Add("office-visible-document-handles:"+handles.Count);
        var result=new List<Item>();
        foreach(var handle in handles) {
            object native; var iid=new Guid("00020400-0000-0000-C000-000000000046");
            int status=AccessibleObjectFromWindow(handle,0xfffffff0,ref iid,out native);
            if(status!=0 || native==null){if(diagnostics.Count<12)diagnostics.Add("office-native-access:0x"+status.ToString("X8"));continue;}
            try {
                dynamic window=native; dynamic doc;
                if(kind=="word") doc=window.Document;
                else if(kind=="excel") doc=window.ActiveSheet.Parent;
                else doc=window.Presentation;
                var reference=OfficeDocument(doc);
                ReadOfficeSelection(window,kind,reference);
                result.Add(reference);
            } catch(ReferenceError) {throw;} catch(Exception error){RecordFailure("office-native-document",error);continue;}
        }
        result=result.GroupBy(i=>i.path,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
        if(result.Count==1){strategy="office-native-window";return result;}
        if(result.Count>1) throw new ReferenceError("ambiguous","Multiple documents found in the foreground window");
        if(kind=="powerpoint")return PowerPointWindow();
        // Some Office builds expose no accessible native document object. Attach
        // read-only through COM, accepting only a window whose HWND matches the
        // foreground window. Never use a global ActiveDocument blindly.
        try {
            string progId=kind=="word"?"Word.Application":kind=="excel"?"Excel.Application":"PowerPoint.Application";
            dynamic application=Marshal.GetActiveObject(progId);
            dynamic windows=application.Windows;
            foreach(dynamic window in windows) {
                if(!OfficeWindowMatches(window))continue;
                dynamic doc=kind=="word"?window.Document:kind=="excel"?window.ActiveSheet.Parent:window.Presentation;
                var reference=OfficeDocument(doc);ReadOfficeSelection(window,kind,reference);
                strategy="office-com-window";return new List<Item>{reference};
            }
        }catch(ReferenceError){throw;}catch(Exception error){RecordFailure("office-com-window",error);}
        throw new ReferenceError("office_unavailable","Cannot read this Office document; close modal dialogs and try again");
    }
    static Item OfficeDocument(dynamic doc){string folder=(string)doc.Path;if(string.IsNullOrEmpty(folder))throw new ReferenceError("unsaved","Save this document to a file first");return ToItem((string)doc.FullName,(string)doc.Name);}
    static bool OfficeWindowMatches(dynamic window){try{long value=Convert.ToInt64(window.Hwnd);IntPtr handle=IntPtr.Size==8?new IntPtr(unchecked((long)(uint)value)):new IntPtr(unchecked((int)value));return handle==foreground || GetAncestor(handle,2)==foreground;}catch{return false;}}
    static List<Item> PowerPointWindow() {
        // DocumentWindow exposes Caption and Active, but has no documented HWND.
        // Correlate a unique COM document window with the foreground editor title.
        // Reject other instances and duplicate captions instead of taking ActivePresentation.
        var cls=new StringBuilder(256);GetClassName(foreground,cls,cls.Capacity);
        if(cls.ToString()!="PPTFrameClass")throw new ReferenceError("powerpoint_window_unavailable","Focus the PowerPoint editing window and try again");
        uint pid;GetWindowThreadProcessId(foreground,out pid);
        int session=Process.GetProcessById((int)pid).SessionId;
        int instances=0;
        foreach(var process in Process.GetProcessesByName("POWERPNT")) {
            using(process){try{if(process.SessionId==session)instances++;}catch(Exception error){RecordFailure("powerpoint-process",error);}}
        }
        if(instances!=1)throw new ReferenceError("ambiguous","Cannot identify the foreground PowerPoint instance safely");
        try {
            dynamic application=Marshal.GetActiveObject("PowerPoint.Application");
            string title=WindowTitle(foreground);
            var matches=new List<object>();
            foreach(dynamic window in application.Windows) {
                if(PowerPointCaptionMatches(title,(string)window.Caption))matches.Add((object)window);
            }
            if(matches.Count>1)throw new ReferenceError("ambiguous","Multiple PowerPoint windows have the same caption");
            if(matches.Count==1) {
                dynamic window=matches[0];
                if((int)window.Active==0)throw new ReferenceError("powerpoint_window_unavailable","PowerPoint document window is not active");
                var item=OfficeDocument(window.Presentation);
                ReadOfficeSelection(window,"powerpoint",item);
                strategy="powerpoint-com-caption";return new List<Item>{item};
            }
            diagnostics.Add("powerpoint-matching-document-windows:0");
        }catch(ReferenceError){throw;}catch(Exception error){RecordFailure("powerpoint-com-caption",error);}
        throw new ReferenceError("powerpoint_window_unavailable","Cannot identify the current PowerPoint presentation");
    }
    public static bool PowerPointCaptionMatches(string title,string caption) {
        if(string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(caption))return false;
        return string.Equals(title,caption,StringComparison.OrdinalIgnoreCase) ||
            string.Equals(title,caption+" - PowerPoint",StringComparison.OrdinalIgnoreCase);
    }
    static bool SupportsTextSelection(string process) {
        return process.StartsWith("markpad",StringComparison.OrdinalIgnoreCase) ||
            new[]{"notepad","notepad++","typora","marktext","winword","excel","powerpnt","chrome","msedge","brave","firefox","code","code - insiders","cursor"}.Contains(process);
    }
    const int FocusLimit=12000;
    static string ClipFocus(string value) {
        if(string.IsNullOrWhiteSpace(value)) return null;
        value=VisibleText(value);
        if(value.Length<=FocusLimit) return value;
        int length=FocusLimit;
        if(char.IsHighSurrogate(value[length-1])) length--;
        return value.Substring(0,length)+" [选区过长，已截断]";
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
        } catch(Exception error){RecordFailure("selection-range",error);}
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
        } catch(Exception error){RecordFailure("optional-selection",error);}
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
                try {item.location="幻灯片 "+((int)window.View.Slide.SlideIndex).ToString();}catch{}
                dynamic selection=window.Selection;
                int type=(int)selection.Type;
                if(type==1) {
                    var slides=new List<int>();foreach(dynamic slide in selection.SlideRange)slides.Add((int)slide.SlideIndex);
                    if(slides.Count>0)item.location="幻灯片 "+string.Join(", ",slides.Select(n=>n.ToString()).ToArray());
                }
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
            } catch(Exception error){RecordFailure("uia-children",error);}
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
            } catch(Exception error){RecordFailure("browser-address",error);}
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
            // Framework GetData(Html) uses the system code page and can corrupt Chinese
            // during restore. Preserve the real UTF-8 clipboard bytes instead.
            object value=format==DataFormats.Html ? (object)new MemoryStream(ReadClipboardHtmlBytes()) : original.GetData(format,false);
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
        string directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VickyDeck","reference-bridge");
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
            throw new ReferenceError("editor_unavailable","Activate Vicky Deck Reference in VS Code and focus the text editor, then retry");
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
                foreach(var value in new[]{e.Current.HelpText,e.Current.Name,e.Current.ItemStatus,e.Current.AutomationId}) {
                    var item=TryLocalMetadata(value);if(item!=null){selected.Add(item);break;}
                }
            } catch(Exception error){RecordFailure("editor-tab",error);}
        }
        selected=selected.GroupBy(i=>i.path,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
        if(selected.Count==1) return selected;
        throw new ReferenceError("editor_unavailable","Current editor tab does not expose an absolute path");
    }
    static List<Item> EditorDocument(){
        try{return Editor();}catch(ReferenceError){}
        string title=WindowTitle(foreground);var item=TryLocalMetadata(title);
        if(item==null){string suffix=app=="notepad++"?"Notepad++":app=="typora"?"Typora":"MarkText";item=TryLocalMetadata(Regex.Replace(title,@"\s+[-—]\s+"+Regex.Escape(suffix)+@"\s*$","",RegexOptions.IgnoreCase).TrimStart('*'));}
        if(item!=null){strategy="editor-full-path-title";return new List<Item>{item};}
        throw new ReferenceError("editor_unavailable","This editor does not expose the current file's full path; enable full paths in its window title or use VS Code with the bundled extension");
    }
    static List<Item> Markpad() {
        var candidates=new List<Item>();var active=new List<Item>();
        foreach(var e in Elements(foreground,false)) {
            try {
                // MarkPad 2.6.11 puts the full path in the tab group's title attribute.
                if(e.Current.ControlType!=ControlType.Group && e.Current.ControlType!=ControlType.TabItem) continue;
                if(e.Current.IsOffscreen) continue;
                var item=TryLocalMetadata(e.Current.HelpText) ?? TryLocalMetadata(e.Current.Name) ?? TryLocalMetadata(e.Current.ItemStatus) ?? TryLocalMetadata(e.Current.AutomationId);
                if(item==null) continue;
                candidates.Add(item);
                object pattern;
                bool selected=e.TryGetCurrentPattern(SelectionItemPattern.Pattern,out pattern) && ((SelectionItemPattern)pattern).Current.IsSelected;
                string cls=e.Current.ClassName ?? "";
                if(selected || Regex.IsMatch(cls,@"\btab\b.*\bactive\b"))active.Add(item);
            } catch(Exception error){RecordFailure("markpad-tab",error);}
        }
        return ResolveMarkdownSource(WindowTitle(foreground),candidates,active);
    }
    public static List<Item> ResolveMarkdownSource(string windowTitle,IEnumerable<Item> allCandidates,IEnumerable<Item> activeCandidates){
        var active=activeCandidates.GroupBy(i=>i.path,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
        var candidates=allCandidates.GroupBy(i=>i.path,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
        if(active.Count==1){strategy="active-tab";return active;}
        if(active.Count>1)throw new ReferenceError("ambiguous","Multiple active Markdown sources are exposed");
        if(candidates.Count==1){strategy="markpad-unique-metadata";return candidates;}
        // Correlate a title with full paths already exposed by tabs; do not search
        // the filesystem or fabricate a path from a basename.
        string title=(windowTitle??"").TrimStart('*').Trim();
        var matching=candidates.Where(i=>string.Equals(title,i.name,StringComparison.OrdinalIgnoreCase) || string.Equals(title,i.name+" - MarkPad",StringComparison.OrdinalIgnoreCase)).ToList();
        if(matching.Count==1){strategy="markpad-title-and-metadata";return matching;}
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
        bool local;value=NormalizeSource(value,out local);
        if(!local)throw new ReferenceError("not_local","No absolute file path is available");
        var full=Path.GetFullPath(value);
        if(!File.Exists(full) && !Directory.Exists(full)) throw new ReferenceError("missing_file","The referenced file is not available on this computer");
        return new Item{name=Path.GetFileName(full.TrimEnd('\\','/')),path=full};
    }
    static Item ToItem(string value,string name) {
        bool local;value=NormalizeSource(value,out local);
        if(local)return LocalItem(value);
        return new Item{name=string.IsNullOrWhiteSpace(name)?new Uri(value).Host:name,path=value};
    }
    static bool IsLocalPath(string value) {
        return value!=null && Regex.IsMatch(value,@"^(?:[A-Za-z]:[\\/]|[\\/]{2}[^\\/]+[\\/][^\\/]+)");
    }
    static string NormalizeSource(string value,out bool local) {
        if(string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl))throw new ReferenceError("invalid_reference","Invalid reference path");
        // Strip Windows extended-path prefixes, not Markdown or user text wrappers.
        if(value.StartsWith(@"\\?\UNC\",StringComparison.OrdinalIgnoreCase))value=@"\\"+value.Substring(8);
        else if(value.StartsWith(@"\\?\",StringComparison.Ordinal))value=value.Substring(4);
        local=IsLocalPath(value);
        Uri uri;
        if(!local && value.StartsWith("file:",StringComparison.OrdinalIgnoreCase)) {
            if(!Uri.TryCreate(value,UriKind.Absolute,out uri) || !uri.IsFile || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                throw new ReferenceError("invalid_reference","Expected an unambiguous file URL");
            value=uri.LocalPath;local=IsLocalPath(value);
        }
        if(local) {
            value=value.Replace('\\','/');
            // Embedded colon can be confused with Codex line suffixes or NTFS alternate streams.
            string tail=Regex.IsMatch(value,@"^[A-Za-z]:/")?value.Substring(3):value.Substring(2);
            if(tail.IndexOfAny(new[]{'<','>','"','|','?','*',':'})>=0 || value.Any(char.IsControl))
                throw new ReferenceError("invalid_reference","Invalid Windows path characters");
            foreach(string segment in tail.Split('/')) {
                if(segment=="." || segment=="..")continue;
                if(segment.EndsWith(" ") || segment.EndsWith(".") || Regex.IsMatch(segment,@"^(?:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)",RegexOptions.IgnoreCase))
                    throw new ReferenceError("invalid_reference","Windows device names or ambiguous trailing spaces/dots cannot be referenced");
            }
            return value;
        }
        if(!Uri.TryCreate(value,UriKind.Absolute,out uri) || (uri.Scheme!="http" && uri.Scheme!="https") || string.IsNullOrEmpty(uri.Host))
            throw new ReferenceError("invalid_reference","Expected a full file path or HTTP(S) URL");
        return value;
    }
    static string VisibleText(string value) {return Regex.Replace(value ?? "",@"[\s\p{Cc}]+"," ").Trim();}
    static string EscapeLabel(string label) {
        // Entities are escaped first so literal '&lt;' remains visible literal text.
        label=label.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;");
        // An underscore inside a word cannot open emphasis: preserve Q2_协同 and file_name.
        // Parentheses, hashes, braces, pipes and exclamation marks are ordinary link text.
        label=Regex.Replace(label,@"[\\\x60*\[\]~$]",@"\$0");
        string escaped=label;
        return Regex.Replace(escaped,@"_+",m=>m.Index>0 && m.Index+m.Length<escaped.Length && char.IsLetterOrDigit(escaped[m.Index-1]) && char.IsLetterOrDigit(escaped[m.Index+m.Length]) ? m.Value : m.Value.Replace("_",@"\_"));
    }
    public static string Format(IEnumerable<Item> items) {return BuildReference(items).text;}
    public static ReferencePayload BuildReference(IEnumerable<Item> items) {
        if(items==null) throw new ReferenceError("empty","No references found");
        var links=new List<string>();var html=new List<string>();var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var item in items) {
            if(item==null)throw new ReferenceError("invalid_reference","Empty reference");
            bool local;string source=NormalizeSource(item.path,out local);
            string label=item.name;
            if(string.IsNullOrWhiteSpace(label)) {
                label=local ? source.TrimEnd('/').Split('/').Last() : new Uri(source).Host;
                if(string.IsNullOrWhiteSpace(label))label=source;
            }
            string destination=LinkDestination(source,local);
            string richDestination=RichDestination(source,local);
            if(item.startLine!=0 || item.endLine!=0) {
                if(!local || item.startLine<1 || item.endLine<item.startLine)throw new ReferenceError("invalid_reference","Invalid source line range");
                destination+=":"+item.startLine;
                richDestination+=":"+item.startLine;
                label=(item.endLine==item.startLine?item.startLine.ToString():item.startLine+"–"+item.endLine)+" (line "+item.startLine+")";
            }
            if(!string.IsNullOrWhiteSpace(item.location))label+=": "+VisibleText(item.location);
            string focus=ClipFocus(item.focus);if(focus!=null)label+=": "+focus;
            label=VisibleText(label);
            string link="["+EscapeLabel(label)+"]("+destination+")";
            if(seen.Add(link)){links.Add(link);html.Add("<a href=\""+HtmlEscape(richDestination)+"\">"+HtmlEscape(label)+"</a>");}
        }
        if(links.Count==0)throw new ReferenceError("empty","No references found");
        string fragment="<span>"+string.Join(" ",html)+"</span>";
        return new ReferencePayload{text=string.Join(" ",links),htmlFragment=fragment,clipboardHtml=ClipboardHtml(fragment)};
    }
    static string HtmlEscape(string value) {return value.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;");}
    static string RichDestination(string value,bool local) {
        // HTML attributes permit spaces and Unicode. Percent and hash still have URI meaning.
        // The HTML destination does not need Markdown escaping: preserve a web URL
        // byte-for-byte, including signatures whose query contains literal () or [].
        return local?value.Replace("%","%25").Replace("#","%23"):value;
    }
    static string ClipboardHtml(string fragment) {
        const string header="Version:1.0\r\nStartHTML:{0:0000000000}\r\nEndHTML:{1:0000000000}\r\nStartFragment:{2:0000000000}\r\nEndFragment:{3:0000000000}\r\n";
        const string before="<html><head><meta charset=\"utf-8\"></head><body><!--StartFragment-->";
        const string after="<!--EndFragment--></body></html>";
        int start=Encoding.UTF8.GetByteCount(string.Format(System.Globalization.CultureInfo.InvariantCulture,header,0,0,0,0));
        int fragmentStart=start+Encoding.UTF8.GetByteCount(before),fragmentEnd=fragmentStart+Encoding.UTF8.GetByteCount(fragment);
        int end=fragmentEnd+Encoding.UTF8.GetByteCount(after);
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,header,start,end,fragmentStart,fragmentEnd)+before+fragment+after;
    }
    static byte[] ReadClipboardHtmlBytes() {
        bool opened=false;
        for(int i=0;i<5 && !opened;i++){opened=OpenClipboard(IntPtr.Zero);if(!opened)Thread.Sleep(20);}
        if(!opened)throw new ReferenceError("clipboard_busy","Cannot preserve clipboard HTML");
        try {
            IntPtr memory=GetClipboardData(RegisterClipboardFormat(DataFormats.Html));
            long size=memory==IntPtr.Zero?0:(long)GlobalSize(memory).ToUInt64();
            if(size<1 || size>33554432)throw new ReferenceError("clipboard_busy","Cannot preserve clipboard HTML");
            IntPtr data=GlobalLock(memory);if(data==IntPtr.Zero)throw new ReferenceError("clipboard_busy","Cannot preserve clipboard HTML");
            try{var bytes=new byte[(int)size];Marshal.Copy(data,bytes,0,bytes.Length);return bytes;}finally{GlobalUnlock(memory);}
        }finally{CloseClipboard();}
    }
    static string EscapeLinkPart(string value) { return Uri.EscapeDataString(value).Replace("(","%28").Replace(")","%29"); }
    static string LinkDestination(string destination,bool local) {
        if(local) {
            // Preserve readable Unicode. Encode only characters that break a bare Markdown target
            // or have URI meaning; never decode raw filesystem names such as literal%20.md.
            var readable=new StringBuilder();
            foreach(char c in destination) {
                if(char.IsWhiteSpace(c) || "%&#()[]<>\"`\\".IndexOf(c)>=0)readable.Append(EscapeLinkPart(c.ToString()));
                else readable.Append(c);
            }
            return readable.ToString();
        }
        // Do not canonicalize signed URLs, plus signs, existing escapes, query order or fragments.
        int authorityEnd=destination.IndexOfAny(new[]{'/','?','#'},destination.IndexOf("://",StringComparison.Ordinal)+3);
        if(authorityEnd<0)authorityEnd=destination.Length;
        var result=new StringBuilder();
        for(int i=0;i<destination.Length;i++) {
            char c=destination[i];
            bool ipv6Bracket=(c=='[' || c==']') && i<authorityEnd;
            if(c=='%' && (i+2>=destination.Length || !Uri.IsHexDigit(destination[i+1]) || !Uri.IsHexDigit(destination[i+2])))result.Append("%25");
            else if(char.IsWhiteSpace(c) || c=='(' || c==')' || c=='<' || c=='>' || c=='\\' || c=='"' || c==(char)96 || ((c=='[' || c==']') && !ipv6Bracket))result.Append(EscapeLinkPart(c.ToString()));
            else result.Append(c);
        }
        return result.ToString();
    }
    static IntPtr ClipboardMemory(byte[] bytes) {
        IntPtr memory=GlobalAlloc(2,new UIntPtr((uint)bytes.Length));
        if(memory==IntPtr.Zero)throw new ReferenceError("clipboard_busy","Cannot allocate clipboard data");
        IntPtr data=GlobalLock(memory);
        if(data==IntPtr.Zero){GlobalFree(memory);throw new ReferenceError("clipboard_busy","Cannot prepare clipboard data");}
        bool prepared=false;
        try{Marshal.Copy(bytes,0,data,bytes.Length);prepared=true;}finally{GlobalUnlock(memory);if(!prepared)GlobalFree(memory);}
        return memory;
    }
    static void WriteClipboard(ReferencePayload payload) {
        IntPtr text=IntPtr.Zero,html=IntPtr.Zero;
        try {
            // Fully materialize both formats before changing the clipboard. No delayed OLE
            // rendering: these bytes remain valid after the short-lived helper exits.
            text=ClipboardMemory(Encoding.Unicode.GetBytes(payload.text+"\0"));
            html=ClipboardMemory(new UTF8Encoding(false).GetBytes(payload.clipboardHtml+"\0"));
            uint htmlFormat=RegisterClipboardFormat(DataFormats.Html);
            if(htmlFormat==0)throw new ReferenceError("clipboard_busy","Cannot register clipboard HTML");
            using(var owner=new Control()) {
                IntPtr handle=owner.Handle;bool opened=false;
                for(int n=0;n<5;n++) {
                    EnsureForeground();
                    if(GetClipboardSequenceNumber()!=initialSequence)throw new ReferenceError("clipboard_changed","Clipboard changed; try again");
                    opened=OpenClipboard(handle);if(opened)break;Thread.Sleep(40);
                }
                if(!opened)throw new ReferenceError("clipboard_busy","Clipboard is busy; try again");
                try {
                    EnsureForeground();
                    if(GetClipboardSequenceNumber()!=initialSequence)throw new ReferenceError("clipboard_changed","Clipboard changed; try again");
                    if(!EmptyClipboard())throw new ReferenceError("clipboard_busy","Cannot update clipboard");
                    if(SetClipboardData(13,text)==IntPtr.Zero)throw new ReferenceError("clipboard_busy","Cannot write clipboard text");
                    text=IntPtr.Zero;
                    if(SetClipboardData(htmlFormat,html)==IntPtr.Zero)throw new ReferenceError("clipboard_busy","Cannot write clipboard HTML");
                    html=IntPtr.Zero;
                }finally{CloseClipboard();}
            }
        }finally{if(text!=IntPtr.Zero)GlobalFree(text);if(html!=IntPtr.Zero)GlobalFree(html);}
    }
    static string WindowTitle(IntPtr h) {var s=new StringBuilder(4096);GetWindowText(h,s,s.Capacity);return s.ToString();}
    delegate bool EnumWindowsProc(IntPtr hwnd,IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
    [DllImport("user32.dll")] static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll")] static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] static extern bool CloseClipboard();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern uint RegisterClipboardFormat(string name);
    [DllImport("user32.dll")] static extern IntPtr GetClipboardData(uint format);
    [DllImport("user32.dll")] static extern bool EmptyClipboard();
    [DllImport("user32.dll")] static extern IntPtr SetClipboardData(uint format,IntPtr memory);
    [DllImport("kernel32.dll")] static extern IntPtr GlobalAlloc(uint flags,UIntPtr bytes);
    [DllImport("kernel32.dll")] static extern IntPtr GlobalFree(IntPtr memory);
    [DllImport("kernel32.dll")] static extern IntPtr GlobalLock(IntPtr memory);
    [DllImport("kernel32.dll")] static extern bool GlobalUnlock(IntPtr memory);
    [DllImport("kernel32.dll")] static extern UIntPtr GlobalSize(IntPtr memory);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h,EnumWindowsProc callback,IntPtr data);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr h,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder name,int size);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder text,int size);
    [DllImport("oleacc.dll")] static extern int AccessibleObjectFromWindow(IntPtr h,uint id,ref Guid iid,[MarshalAs(UnmanagedType.IDispatch)] out object obj);
}
