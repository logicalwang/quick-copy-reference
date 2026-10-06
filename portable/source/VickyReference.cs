using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

public static class PortableReference {
    public const string Version="1.0.7";
    public static string L(string value){return Localizer.Text(value);}
    public static readonly JavaScriptSerializer Json=new JavaScriptSerializer();
    public static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
    static readonly string Identity=WindowsIdentity.GetCurrent().User.Value;
    public static readonly string CopyEvent="Local\\VickyReference.Copy."+Identity;
    public static readonly string QuitEvent="Local\\VickyReference.Quit."+Identity;
    public static readonly string InstanceMutex="Local\\VickyReference.Resident."+Identity;
    public static readonly string CaptureMutex="Local\\VickyReference.Capture."+Identity;
    public static readonly string StateFile=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VickyReference","status.json");
    public class Settings {public string hotkey {get;set;} public bool showSuccessNotification {get;set;} public bool configured {get;set;} public bool allowCommonShortcut {get;set;} public string language {get;set;} }
    public class Result {public bool ok {get;set;} public string code {get;set;} public string message {get;set;} public string app {get;set;} public int count {get;set;} public string strategy {get;set;} public string[] diagnostics {get;set;} }
    public class Shortcut {public uint Modifiers;public Keys Key;public string Text;}
    public static Shortcut ParseShortcut(string value) {
        if(string.IsNullOrWhiteSpace(value))throw new Exception(L("快捷键不能为空"));
        uint modifiers=0;Keys key=Keys.None;int count=0;
        foreach(string part in value.Split('+')) {
            string token=part.Trim().ToUpperInvariant();uint modifier=token=="CTRL"||token=="CONTROL"?2u:token=="ALT"?1u:token=="SHIFT"?4u:0u;
            if(modifier!=0){if((modifiers&modifier)!=0)throw new Exception(L("快捷键包含重复修饰键"));modifiers|=modifier;continue;}
            Keys parsed;
            if(token.Length==1 && token[0]>='0' && token[0]<='9')parsed=Keys.D0+token[0]-'0';
            else if(!System.Text.RegularExpressions.Regex.IsMatch(token,@"^(?:[A-Z]|F(?:[1-9]|1[0-9]|2[0-4]))$") || !Enum.TryParse<Keys>(token,true,out parsed))throw new Exception(L("无法识别快捷键：")+part);
            bool letter=parsed>=Keys.A && parsed<=Keys.Z,number=parsed>=Keys.D0 && parsed<=Keys.D9,function=parsed>=Keys.F1 && parsed<=Keys.F24;
            if(!letter && !number && !function)throw new Exception(L("主键请使用字母、数字或 F1–F24"));
            if(parsed==Keys.F12)throw new Exception(L("F12 为调试器保留，请选其他按键"));
            key=parsed;count++;
        }
        if(count!=1 || (modifiers==0 && !(key>=Keys.F1 && key<=Keys.F24)))throw new Exception(L("请使用组合键，或单个 F1–F24 功能键（F12 除外）"));
        return new Shortcut{Modifiers=modifiers,Key=key,Text=((modifiers&2)!=0?"Ctrl+":"")+((modifiers&1)!=0?"Alt+":"")+((modifiers&4)!=0?"Shift+":"")+(key>=Keys.D0&&key<=Keys.D9?((int)key-(int)Keys.D0).ToString():key.ToString())};
    }
    public static Settings ReadSettings() {
        string file=Path.Combine(Root,"reference-settings.json");
        var settings=File.Exists(file)?Json.Deserialize<Settings>(File.ReadAllText(file,Encoding.UTF8)):new Settings{hotkey="Ctrl+Alt+Shift+R",showSuccessNotification=true};
        if(settings==null)throw new Exception(L("配置内容无效"));if(!Localizer.ExplicitLanguage)Localizer.SetLanguage(settings.language);settings.language=Localizer.Language;ParseShortcut(settings.hotkey);return settings;
    }
    public static void SaveSettings(Settings settings) {
        var key=ParseShortcut(settings.hotkey);string conflict=ChoiceError(key,settings.allowCommonShortcut);if(conflict!=null)throw new Exception(conflict);
        settings.hotkey=key.Text;settings.configured=true;settings.language=Localizer.Normalize(settings.language);
        string file=Path.Combine(Root,"reference-settings.json");
        File.WriteAllText(file,Json.Serialize(settings),new UTF8Encoding(false));
    }
    static int ProbeShortcut(Shortcut key) {
        var window=new MessageWindow();try{if(!RegisterHotKey(window.Handle,501,key.Modifiers|0x4000,(uint)key.Key)){int error=Marshal.GetLastWin32Error();return error==0?-1:error;}UnregisterHotKey(window.Handle,501);return 0;}finally{window.DestroyHandle();}
    }
    static bool Available(Shortcut key){return ProbeShortcut(key)==0;}
    static string RegistrationError(int error){return error==1409?L("这个快捷键已被系统或其他程序注册占用，请换一个。"):L("无法注册此快捷键（错误 ")+error+L("），请换一个。");}
    static readonly Dictionary<string,string> CommonShortcuts=new Dictionary<string,string>{
        {"Ctrl+A","全选"},{"Ctrl+B","加粗"},{"Ctrl+C","复制"},{"Ctrl+D","浏览器收藏页面"},
        {"Ctrl+F","查找"},{"Ctrl+G","编辑器跳转／查找下一项"},{"Ctrl+H","替换／浏览器历史记录"},
        {"Ctrl+I","斜体"},{"Ctrl+J","浏览器下载列表"},{"Ctrl+K","插入链接／编辑器组合快捷键前缀"},
        {"Ctrl+L","浏览器地址栏"},{"Ctrl+N","新建文档／窗口"},{"Ctrl+O","打开文件"},
        {"Ctrl+P","打印／VS Code 快速打开"},{"Ctrl+R","刷新"},{"Ctrl+S","保存"},
        {"Ctrl+T","浏览器新建标签页"},{"Ctrl+U","下划线／网页源代码"},{"Ctrl+V","粘贴"},
        {"Ctrl+W","关闭文档／标签页"},{"Ctrl+X","剪切"},{"Ctrl+Y","重做"},{"Ctrl+Z","撤销"},
        {"Ctrl+Shift+N","新建文件夹／浏览器无痕窗口"},{"Ctrl+Shift+P","VS Code 命令面板／浏览器打印对话框"},
        {"Ctrl+Shift+S","另存为"},{"Ctrl+Shift+T","恢复关闭的浏览器标签页"},
        {"Ctrl+Shift+F","编辑器全局搜索"},{"Ctrl+Shift+R","浏览器强制刷新"},
        {"Ctrl+Shift+V","粘贴纯文本／编辑器 Markdown 预览"},{"Ctrl+Shift+W","关闭浏览器窗口"},
        {"Ctrl+Shift+Z","重做"},{"Ctrl+F4","关闭当前文档"},{"Ctrl+F5","刷新（浏览器忽略缓存）"},
        {"Alt+F4","关闭窗口"},{"Alt+F8","登录界面显示密码"},{"Alt+F","打开文件／浏览器菜单"},
        {"F1","帮助／VS Code 命令面板"},{"F2","重命名"},{"F3","查找下一项"},
        {"F5","刷新／启动调试"},{"F6","切换界面区域／地址栏"},
        {"F9","VS Code 切换断点"},{"F10","菜单栏／调试逐过程"},{"F11","全屏／调试逐语句"}
    };
    public static string KnownShortcutUse(Shortcut key){string use;if(CommonShortcuts.TryGetValue(key.Text,out use))return L(use);if(key.Modifiers==2 && key.Key>=Keys.D1 && key.Key<=Keys.D9)return L("切换浏览器标签页");return null;}
    static bool ClipboardShortcut(Shortcut key){return key.Modifiers==2 && (key.Key==Keys.C || key.Key==Keys.X || key.Key==Keys.V);}
    public static string ChoiceError(Shortcut key,bool allowCommon) {
        string use=KnownShortcutUse(key);if(ClipboardShortcut(key))return key.Text+L(" 已用于“")+use+L("”，不能作为复制引用快捷键。\n请保留正常复制／剪切／粘贴功能，换一个组合键。");
        return use!=null && !allowCommon?key.Text+L(" 已常用于“")+use+L("”。\n请换一个，或明确勾选“仍然使用”以覆盖原功能。"):null;
    }
    static string ShortcutAdvice(Shortcut key){return key.Modifiers==0?L("单个功能键可能覆盖软件原有功能，建议使用多修饰键组合。"):key.Modifiers==2 || key.Modifiers==1?L("此组合可能与软件内部快捷键重叠，建议使用多修饰键组合。"):L("应用内部快捷键仍可能重叠，请避开你常用的按键。");}
    public static Settings Configure(Settings current) {
        string previousLanguage=Localizer.Language;
        using(var form=new Form{Text=L("设置快速复制引用快捷键"),Width=680,Height=525,Font=new Font(SystemFonts.MessageBoxFont.FontFamily,10),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,StartPosition=FormStartPosition.CenterScreen}) {
            var languageLabel=new Label{Left=20,Top=20,Width=130,Text=L("语言")};
            var language=new ComboBox{Left=150,Top=16,Width=220,DropDownStyle=ComboBoxStyle.DropDownList};
            language.Items.AddRange(new object[]{"简体中文","English"});language.SelectedIndex=Localizer.Language=="zh-CN"?0:1;
            var title=new Label{Left=20,Top=60,Width=625,Height=50,Text=L("点击下面的输入框，直接按下你想用的快捷键。\n支持 Ctrl／Alt／Shift 组合键，或单个功能键。")};
            var input=new TextBox{Left=20,Top=116,Width=625,ReadOnly=true,Text=current.hotkey};
            var hint=new Label{Left=20,Top=154,Width=625,Height=85};
            var limitation=new Label{Left=20,Top=244,Width=625,Height=80,Text=L("检测包含常用快捷键清单和已注册的全局快捷键。\n其他软件自定义按键／鼠标宏仍无法全面检测；\n启用后可能覆盖原功能，请避开你常用的按键。")};
            var acknowledge=new CheckBox{Left=20,Top=330,Width=625,Height=45,Text=L("仍然使用此常用快捷键（将覆盖原功能）"),Checked=current.allowCommonShortcut};
            var success=new CheckBox{Left=20,Top=388,Width=300,Text=L("复制成功时显示提示"),Checked=current.showSuccessNotification};
            var boot=new CheckBox{Left=360,Top=388,Width=260,Text=L("开机启动"),Checked=File.Exists(StartupFile)};
            var save=new Button{Left=365,Top=440,Width=160,Height=30,Text=L("保存并启用")};var cancel=new Button{Left=540,Top=440,Width=105,Height=30,Text=L("取消"),DialogResult=DialogResult.Cancel};
            Settings chosen=null;
            Action check=()=>{try{var key=ParseShortcut(input.Text);string use=KnownShortcutUse(key);acknowledge.Visible=use!=null && !ClipboardShortcut(key);string conflict=ChoiceError(key,acknowledge.Checked);if(conflict!=null){save.Enabled=false;hint.Text=conflict;hint.ForeColor=Color.Firebrick;return;}int error=ProbeShortcut(key);save.Enabled=error==0;hint.Text=error!=0?RegistrationError(error):use!=null?key.Text+L("：已知用途“")+use+L("”将被覆盖。\n你已确认使用；未发现已注册的全局冲突。"):L("未发现已知常用／已注册的全局冲突：")+key.Text+"\n"+ShortcutAdvice(key);hint.ForeColor=error==0?Color.DarkGoldenrod:Color.Firebrick;}catch(Exception error){save.Enabled=false;hint.Text=error.Message;hint.ForeColor=Color.Firebrick;}};
            acknowledge.CheckedChanged+=(s,e)=>check();
            language.SelectedIndexChanged+=(s,e)=>{
                Localizer.SetLanguage(language.SelectedIndex==0?"zh-CN":"en");
                form.Text=L("设置快速复制引用快捷键");languageLabel.Text=L("语言");
                title.Text=L("点击下面的输入框，直接按下你想用的快捷键。\n支持 Ctrl／Alt／Shift 组合键，或单个功能键。");
                limitation.Text=L("检测包含常用快捷键清单和已注册的全局快捷键。\n其他软件自定义按键／鼠标宏仍无法全面检测；\n启用后可能覆盖原功能，请避开你常用的按键。");
                acknowledge.Text=L("仍然使用此常用快捷键（将覆盖原功能）");success.Text=L("复制成功时显示提示");boot.Text=L("开机启动");save.Text=L("保存并启用");cancel.Text=L("取消");check();
            };
            input.KeyDown+=(s,e)=>{
                if(e.KeyCode==Keys.ControlKey || e.KeyCode==Keys.ShiftKey || e.KeyCode==Keys.Menu)return;
                if(e.KeyCode==Keys.Escape){e.SuppressKeyPress=true;form.DialogResult=DialogResult.Cancel;form.Close();return;}
                if(e.KeyCode==Keys.Tab){e.SuppressKeyPress=true;form.SelectNextControl(input,!e.Shift,true,true,true);return;}
                e.SuppressKeyPress=true;e.Handled=true;
                try{var key=RecordedKey(e);input.Text=key.Text;acknowledge.Checked=false;check();}catch(Exception error){save.Enabled=false;hint.Text=error.Message;hint.ForeColor=Color.Firebrick;}
            };
            save.Click+=(s,e)=>{
                try{
                    var key=ParseShortcut(input.Text);string conflict=ChoiceError(key,acknowledge.Checked);if(conflict!=null){save.Enabled=false;throw new Exception(conflict);}int registrationError=ProbeShortcut(key);if(registrationError!=0){save.Enabled=false;throw new Exception(RegistrationError(registrationError));}
                    var next=new Settings{hotkey=key.Text,showSuccessNotification=success.Checked,configured=true,allowCommonShortcut=KnownShortcutUse(key)!=null && acknowledge.Checked,language=Localizer.Language};
                    bool oldBoot=File.Exists(StartupFile);if(boot.Checked!=oldBoot)Startup(boot.Checked);
                    try{SaveSettings(next);}catch{if(boot.Checked!=oldBoot)Startup(oldBoot);throw;}
                    chosen=next;form.DialogResult=DialogResult.OK;form.Close();
                }catch(Exception error){hint.Text=error.Message;hint.ForeColor=Color.Firebrick;}
            };
            form.Controls.AddRange(new Control[]{languageLabel,language,title,input,hint,limitation,acknowledge,success,boot,save,cancel});form.CancelButton=cancel;form.Shown+=(s,e)=>{check();input.Focus();};
            try{return form.ShowDialog()==DialogResult.OK?chosen:null;}finally{if(chosen==null)Localizer.SetLanguage(previousLanguage);}
        }
    }
    public static Shortcut RecordedKey(KeyEventArgs input) {
        if((GetAsyncKeyState(91)&0x8000)!=0 || (GetAsyncKeyState(92)&0x8000)!=0)throw new Exception(L("此工具不使用 Windows 键组合，请选择其他快捷键"));
        string main=input.KeyCode>=Keys.D0 && input.KeyCode<=Keys.D9?((int)input.KeyCode-(int)Keys.D0).ToString():input.KeyCode.ToString();
        return ParseShortcut((input.Control?"Ctrl+":"")+(input.Alt?"Alt+":"")+(input.Shift?"Shift+":"")+main);
    }
    public static string StartupFile {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"VickyReference.lnk");}}
    public static void Startup(bool enabled) {
        if(!enabled){if(File.Exists(StartupFile))File.Delete(StartupFile);return;}
        object shell=null,shortcut=null;
        try {
            shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));dynamic automation=shell;
            shortcut=automation.CreateShortcut(StartupFile);dynamic link=shortcut;
            link.TargetPath=Application.ExecutablePath;link.Arguments="--start";link.WorkingDirectory=Root;link.WindowStyle=7;link.Description="Quick Copy Reference keyboard shortcut";link.Save();
        }finally{if(shortcut!=null)Marshal.FinalReleaseComObject(shortcut);if(shell!=null)Marshal.FinalReleaseComObject(shell);}
    }
    public static Result Failure(string code,string message){return new Result{ok=false,code=code,message=message};}
    public static string Message(Result result) {
        if(result.ok)return result.count>1?L("已复制 ")+result.count+L(" 个引用"):L("已复制引用，可以粘贴了");
        string application=(result.app??"").ToLowerInvariant();
        if(result.code=="editor_unavailable"){
            if(application=="code" || application=="code - insiders")return L("VS Code：无法读取当前文件。请安装随包扩展，并聚焦已保存文件的编辑区。");
            if(application.StartsWith("markpad"))return L("MarkPad：无法确定当前文件的完整路径。请确认文件已保存，并切换到要引用的标签；可先只保留该标签再试。");
            if(application=="notepad")return L("记事本：无法取得当前文件的完整路径。请先保存文件，并关闭另存为等对话框后重试。");
            return L("当前阅读器／编辑器未提供明确的文件路径。请先保存文件，并启用窗口标题显示完整路径后重试。");
        }
        if(result.code=="powerpoint_window_unavailable")return L("PowerPoint：无法确认当前演示文稿，请聚焦编辑窗口再试；最近错误详情可从托盘菜单查看。");
        if(result.code=="office_unavailable")return (application=="winword"?"Word":application=="excel"?"Excel":application=="powerpnt"?"PowerPoint":"Office")+L("：无法读取当前文档。请关闭弹出的对话框并聚焦文档区；最近错误详情可从托盘菜单查看。");
        if(result.code=="capture_failed")return L("无法读取当前软件的引用；请从托盘菜单查看最近错误详情。");
        var known=new Dictionary<string,string>{{"unsupported",L("当前软件暂不支持")},{"nothing_selected",L("请在资源管理器中先选中文件或文件夹")},{"unsaved",L("请先保存文档")},{"focus_changed",L("目标窗口已改变，请重新按快捷键")},{"clipboard_changed",L("剪贴板已改变，请重新按快捷键")},{"clipboard_busy",L("剪贴板暂时忙，请重试")},{"browser_unavailable",L("无法读取网页地址，请结束地址栏编辑后重试")},{"missing_file",L("当前文件不可用，请确认路径和 OneDrive 文件状态")},{"busy",L("正在复制，请稍候")},{"keys_held",L("请松开快捷键后再试")},{"timeout",L("目标软件未响应；若出现另存为窗口，请取消它")}};
        return known.ContainsKey(result.code??"")?known[result.code]:L("无法确认当前来源，请关闭对话框并重新聚焦文档后重试。");
    }
    static string TargetApp(IntPtr target){try{uint pid;GetWindowThreadProcessId(target,out pid);return Process.GetProcessById((int)pid).ProcessName.ToLowerInvariant();}catch{return null;}}
    static Result WithApp(Result result,IntPtr target){if(string.IsNullOrEmpty(result.app))result.app=TargetApp(target);return result;}
    public static string ErrorFile {get{return Path.Combine(Path.GetDirectoryName(StateFile),"last-error.json");}}
    static void SaveError(Result result){if(result==null || result.ok)return;try{Directory.CreateDirectory(Path.GetDirectoryName(ErrorFile));File.WriteAllText(ErrorFile,Json.Serialize(new{timeUtc=DateTime.UtcNow.ToString("o"),version=Version,app=result.app,code=result.code,description=Message(new Result{ok=false,app=result.app,code=result.code}),diagnostics=result.diagnostics}),new UTF8Encoding(false));}catch{}}
    public static Result Capture(IntPtr target) {
        using(var gate=new Mutex(false,CaptureMutex)) {
            bool acquired=false;
            try {
                try{acquired=gate.WaitOne(0);}catch(AbandonedMutexException){acquired=true;}
                if(!acquired)return Failure("busy",null);
                if(GetForegroundWindow()!=target)return Failure("focus_changed",null);
                string helper=Path.Combine(Root,"ReferenceCapture.exe");
                if(!File.Exists(helper))return Failure("missing_helper",L("请把 ReferenceCapture.exe 和启动程序放在同一文件夹"));
                var info=new ProcessStartInfo(helper,"--copy --language="+Localizer.Language){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Root};
                using(var process=Process.Start(info)) {
                    var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();
                    if(!process.WaitForExit(15000)){try{process.Kill();process.WaitForExit(1000);}catch{}return WithApp(Failure("timeout",null),target);}
                    if(!output.Wait(1000))return Failure("helper_failed",L("引用程序没有返回结果"));
                    Result result;
                    try{result=Json.Deserialize<Result>(output.Result);}catch{return Failure("helper_failed",L("引用程序没有返回有效结果"));}
                    if(result==null || (result.ok && (process.ExitCode!=0 || result.count<1)))return Failure("helper_failed",L("引用程序结果无效"));
                    return WithApp(result,target);
                }
            }catch(Exception error){return Failure("helper_failed",error.Message);}finally{if(acquired)gate.ReleaseMutex();}
        }
    }
    public static bool KeysReleased(Shortcut shortcut) {
        foreach(int key in new[]{16,17,18,(int)shortcut.Key})if((GetAsyncKeyState(key)&0x8000)!=0)return false;return true;
    }
    public static bool Signal(string name){try{using(var handle=EventWaitHandle.OpenExisting(name)){handle.Set();return true;}}catch(WaitHandleCannotBeOpenedException){return false;}}
    static void Output(object value){byte[] bytes=new UTF8Encoding(false).GetBytes(Json.Serialize(value)+"\n");Console.OpenStandardOutput().Write(bytes,0,bytes.Length);}
    [STAThread] public static int Main(string[] args) {
        try {
            Localizer.Initialize(Root,ref args);
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            string mode=args.Length==0?(string.Equals(Path.GetFileNameWithoutExtension(Application.ExecutablePath),"CopyReference",StringComparison.OrdinalIgnoreCase)?"--copy":"--start"):args.Length==1?args[0]:"invalid";
            if(mode=="--status") {
                bool running=false;object saved=null;
                if(File.Exists(StateFile)){var state=Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(StateFile,Encoding.UTF8));try{var process=Process.GetProcessById(Convert.ToInt32(state["pid"]));running=!process.HasExited && string.Equals(process.MainModule.FileName,Convert.ToString(state["executable"]),StringComparison.OrdinalIgnoreCase);}catch{}saved=state;}
                Output(new{ok=true,running=running,state=saved});return 0;
            }
            if(mode=="--quit"){Output(new{ok=true,requested=Signal(QuitEvent)});return 0;}
            if(mode=="--install-startup" || mode=="--remove-startup"){Startup(mode=="--install-startup");Output(new{ok=true});MessageBox.Show(mode=="--install-startup"?L("已设置开机启动。请保持此文件夹的位置不变。"):L("已取消开机启动。"),L("快速复制引用"));return 0;}
            if(mode=="--self-test"){SelfTest();Output(new{ok=true,version=Version,product=L("快速复制引用"),language=Localizer.Language,successMessage=Message(new Result{ok=true,count=1}),protectedShortcut=ChoiceError(ParseShortcut("Ctrl+C"),true),menuLabel=L("查看最近错误")});return 0;}
            if(mode=="--copy") {
                IntPtr target=GetForegroundWindow();if(Signal(CopyEvent)){Output(new{ok=true,requested=true});return 0;}
                var shortcut=ParseShortcut(ReadSettings().hotkey);var timer=Stopwatch.StartNew();while(!KeysReleased(shortcut) && timer.ElapsedMilliseconds<2000)Thread.Sleep(20);
                Result result=WithApp(KeysReleased(shortcut)?Capture(target):Failure("keys_held",null),target);SaveError(result);Output(result);
                if(!result.ok)using(var icon=new NotifyIcon{Icon=SystemIcons.Application,Visible=true}){icon.ShowBalloonTip(2000,L("快速复制引用"),Message(result),ToolTipIcon.Warning);Thread.Sleep(1000);}
                return result.ok?0:1;
            }
            if(mode!="--start")throw new Exception(L("参数：--start、--copy、--status、--quit、--install-startup、--remove-startup"));
            bool created;using(var instance=new Mutex(true,InstanceMutex,out created)) {
                if(!created)return 0;
                try{
                    var settings=ReadSettings();
                    if(!settings.configured || ChoiceError(ParseShortcut(settings.hotkey),settings.allowCommonShortcut)!=null){Directory.CreateDirectory(Path.GetDirectoryName(StateFile));File.WriteAllText(StateFile,Json.Serialize(new{pid=Process.GetCurrentProcess().Id,executable=Application.ExecutablePath,version=Version,registered=false,setupRequired=true}),new UTF8Encoding(false));settings=Configure(settings);if(settings==null){File.Delete(StateFile);return 0;}}
                    using(var context=new Resident(settings))Application.Run(context);
                }finally{instance.ReleaseMutex();}
            }
            return 0;
        }catch(Exception error){Output(new{ok=false,message=error.Message});bool interactive=args.Length==0?!string.Equals(Path.GetFileNameWithoutExtension(Application.ExecutablePath),"CopyReference",StringComparison.OrdinalIgnoreCase):args[0]=="--start" || args[0]=="--install-startup" || args[0]=="--remove-startup";if(interactive)MessageBox.Show(error.Message,L("快速复制引用"),MessageBoxButtons.OK,MessageBoxIcon.Warning);return 1;}
    }
    static void SelfTest() {
        string language=Localizer.Language;try{Localizer.SetLanguage("en");foreach(string key in Localizer.Keys){string text=L(key);if(string.IsNullOrWhiteSpace(text) || System.Text.RegularExpressions.Regex.IsMatch(text,@"[\u4e00-\u9fff]"))throw new Exception("Incomplete English translation");}}finally{Localizer.SetLanguage(language);}
        if(ParseShortcut("shift+Ctrl+Alt+r").Text!="Ctrl+Alt+Shift+R")throw new Exception("Shortcut normalization failed");
        if(ParseShortcut("Ctrl+Alt+9").Key!=Keys.D9 || ParseShortcut("Ctrl+Shift+F9").Key!=Keys.F9)throw new Exception("Shortcut key parse failed");
        if(ParseShortcut("F9").Modifiers!=0)throw new Exception("Single function key parse failed");
        if(RecordedKey(new KeyEventArgs(Keys.Control|Keys.Shift|Keys.D9)).Text!="Ctrl+Shift+9" || RecordedKey(new KeyEventArgs(Keys.F9)).Text!="F9")throw new Exception("Settings dialog key recording failed");
        foreach(string protectedKey in new[]{"Ctrl+C","Ctrl+X","Ctrl+V"}){var protectedShortcut=ParseShortcut(protectedKey);if(KnownShortcutUse(protectedShortcut)==null || ChoiceError(protectedShortcut,false)==null || ChoiceError(protectedShortcut,true)==null)throw new Exception("Clipboard shortcut protection failed: "+protectedKey);}
        foreach(string common in new[]{"Ctrl+S","Ctrl+F","Ctrl+Z","Ctrl+R","Ctrl+Shift+T","Ctrl+1","F5","F9","Alt+F4"}){var key=ParseShortcut(common);if(ChoiceError(key,false)==null || ChoiceError(key,true)!=null)throw new Exception("Common shortcut acknowledgment failed: "+common);}
        if(ChoiceError(ParseShortcut("Ctrl+Alt+Shift+R"),false)!=null)throw new Exception("Custom shortcut incorrectly blocked");
        foreach(string bad in new[]{"R","Ctrl+Alt","Ctrl+Ctrl+R","Ctrl+F12","Ctrl+R+T","Win+R","Ctrl+Enter","Ctrl+A,B"}){bool rejected=false;try{ParseShortcut(bad);}catch{rejected=true;}if(!rejected)throw new Exception("Unsafe shortcut accepted");}
        ReadSettings();if(!File.Exists(Path.Combine(Root,"ReferenceCapture.exe")))throw new Exception("Missing portable capture helper");
        if(Message(Failure("focus_changed",null))==null)throw new Exception("Missing failure feedback");
        if(Message(new Result{ok=false,code="editor_unavailable",app="markpad_2.7.6_x64"}).Contains("VS Code") || !Message(new Result{ok=false,code="editor_unavailable",app="code"}).Contains("VS Code"))throw new Exception("Application-specific editor failure guidance is incorrect");
        if(!Message(new Result{ok=false,code="office_unavailable",app="excel"}).StartsWith("Excel"))throw new Exception("Office failure guidance is incorrect");
        var testKey=ParseShortcut("Ctrl+Alt+Shift+F24");if(!Available(testKey))throw new Exception("Native hotkey registration test failed");
        var conflictWindow=new MessageWindow();try{if(!RegisterHotKey(conflictWindow.Handle,502,testKey.Modifiers|0x4000,(uint)testKey.Key))throw new Exception("Conflict fixture registration failed");if(ProbeShortcut(testKey)!=1409)throw new Exception("Occupied global shortcut was not detected");}finally{UnregisterHotKey(conflictWindow.Handle,502);conflictWindow.DestroyHandle();}
        if(!Available(testKey))throw new Exception("Conflict probe failed to release temporary registration");
    }
    public class MessageWindow:NativeWindow {
        public Action Copy;public Action Quit;public Action Completed;
        public MessageWindow(){CreateHandle(new CreateParams{Caption="VickyReference",Parent=new IntPtr(-3)});}
        protected override void WndProc(ref System.Windows.Forms.Message message){if(message.Msg==0x312 || message.Msg==0x8001){if(Copy!=null)Copy();}else if(message.Msg==0x8002){if(Quit!=null)Quit();}else if(message.Msg==0x8003){if(Completed!=null)Completed();}base.WndProc(ref message);}
    }
    class Resident:ApplicationContext {
        readonly MessageWindow window=new MessageWindow();readonly NotifyIcon icon=new NotifyIcon();readonly System.Windows.Forms.Timer releaseTimer=new System.Windows.Forms.Timer{Interval=25};
        readonly EventWaitHandle copy=new EventWaitHandle(false,EventResetMode.AutoReset,CopyEvent),quit=new EventWaitHandle(false,EventResetMode.AutoReset,QuitEvent);
        RegisteredWaitHandle copyWait,quitWait;Settings settings;Shortcut shortcut;int hotkeyId=42,completedCount;Result lastResult;bool registered,busy,editing;volatile bool disposed;volatile Result completion;IntPtr target;DateTime requested;
        ToolStripMenuItem startup;
        public Resident(Settings value) {
            settings=value;shortcut=ParseShortcut(settings.hotkey);window.Copy=Request;window.Quit=ExitThread;window.Completed=Completed;
            string conflict=ChoiceError(shortcut,settings.allowCommonShortcut);if(conflict!=null)throw new Exception(conflict);
            registered=RegisterHotKey(window.Handle,hotkeyId,shortcut.Modifiers|0x4000,(uint)shortcut.Key);
            icon.Icon=SystemIcons.Application;icon.Text=L("快速复制引用")+" · "+shortcut.Text;icon.Visible=true;
            var menu=new ContextMenuStrip();menu.Items.Add(L("设置快捷键…（")+shortcut.Text+"）",null,(s,e)=>Edit());
            menu.Items.Add(L("编辑配置"),null,(s,e)=>Process.Start(new ProcessStartInfo("notepad.exe","\""+Path.Combine(Root,"reference-settings.json")+"\""){UseShellExecute=true}));
            menu.Items.Add(L("重新加载配置"),null,(s,e)=>Reload());
            menu.Items.Add(L("查看最近错误"),null,(s,e)=>{if(File.Exists(ErrorFile))Process.Start(new ProcessStartInfo("notepad.exe","\""+ErrorFile+"\""){UseShellExecute=true});else Notify(L("暂无错误记录"),false);});
            startup=new ToolStripMenuItem(L("开机启动")){Checked=File.Exists(StartupFile)};startup.Click+=(s,e)=>{try{Startup(!startup.Checked);startup.Checked=File.Exists(StartupFile);}catch(Exception error){Notify(error.Message,true);}};menu.Items.Add(startup);
            menu.Items.Add(L("打开工具文件夹"),null,(s,e)=>Process.Start(new ProcessStartInfo(Root){UseShellExecute=true}));
            var languages=new ToolStripMenuItem(L("语言"));
            languages.DropDownItems.Add("简体中文",null,(s,e)=>ChangeLanguage("zh-CN"));languages.DropDownItems.Add("English",null,(s,e)=>ChangeLanguage("en"));menu.Items.Add(languages);
            menu.Items.Add(L("退出"),null,(s,e)=>ExitThread());icon.ContextMenuStrip=menu;
            releaseTimer.Tick+=(s,e)=>Released();
            copyWait=ThreadPool.RegisterWaitForSingleObject(copy,(s,t)=>PostMessage(window.Handle,0x8001,IntPtr.Zero,IntPtr.Zero),null,Timeout.Infinite,false);
            quitWait=ThreadPool.RegisterWaitForSingleObject(quit,(s,t)=>PostMessage(window.Handle,0x8002,IntPtr.Zero,IntPtr.Zero),null,Timeout.Infinite,false);
            RefreshMenu();WriteState();Notify(registered?L("已启用 ")+shortcut.Text:L("快捷键未能注册；请从托盘“设置快捷键…”更换按键"),!registered);
        }
        void RefreshMenu(){icon.Text=L("快速复制引用")+" · "+shortcut.Text;var items=icon.ContextMenuStrip.Items;items[0].Text=L("设置快捷键…（")+shortcut.Text+(Localizer.Language=="en"?")":"）");items[1].Text=L("编辑配置");items[2].Text=L("重新加载配置");items[3].Text=L("查看最近错误");items[4].Text=L("开机启动");items[5].Text=L("打开工具文件夹");items[6].Text=L("语言");items[7].Text=L("退出");var languages=(ToolStripMenuItem)items[6];((ToolStripMenuItem)languages.DropDownItems[0]).Checked=Localizer.Language=="zh-CN";((ToolStripMenuItem)languages.DropDownItems[1]).Checked=Localizer.Language=="en";}
        void ChangeLanguage(string language){if(busy || editing)return;string previous=settings.language;try{settings.language=language;SaveSettings(settings);Localizer.SetLanguage(language);RefreshMenu();WriteState();}catch(Exception error){settings.language=previous;Notify(error.Message,true);}}
        void WriteState(){try{Directory.CreateDirectory(Path.GetDirectoryName(StateFile));File.WriteAllText(StateFile,Json.Serialize(new{pid=Process.GetCurrentProcess().Id,executable=Application.ExecutablePath,version=Version,language=Localizer.Language,hotkey=shortcut.Text,registered=registered,completedCount=completedCount,lastResult=lastResult==null?null:new{ok=lastResult.ok,code=lastResult.code,count=lastResult.count,app=lastResult.app,strategy=lastResult.strategy,diagnostics=lastResult.diagnostics}}),new UTF8Encoding(false));}catch{}}
        void Edit() {
            if(busy || editing){Notify(L("正在复制，请稍后设置"),false);return;}
            editing=true;var old=shortcut;if(registered)UnregisterHotKey(window.Handle,hotkeyId);registered=false;
            try{var next=Configure(settings);if(next!=null){settings=next;shortcut=ParseShortcut(next.hotkey);}registered=RegisterHotKey(window.Handle,hotkeyId,shortcut.Modifiers|0x4000,(uint)shortcut.Key);if(!registered){shortcut=old;registered=RegisterHotKey(window.Handle,hotkeyId,shortcut.Modifiers|0x4000,(uint)shortcut.Key);Notify(L("快捷键启用失败，已尝试恢复原快捷键"),true);}icon.Text=L("快速复制引用")+" · "+shortcut.Text;RefreshMenu();startup.Checked=File.Exists(StartupFile);WriteState();}finally{editing=false;}
        }
        void Reload() {
            if(busy || editing){Notify(L("正在复制或设置，请稍后重新加载"),false);return;}
            try {
                var next=ReadSettings();var key=ParseShortcut(next.hotkey);
                string conflict=ChoiceError(key,next.allowCommonShortcut);if(conflict!=null)throw new Exception(conflict);
                if(key.Modifiers!=shortcut.Modifiers || key.Key!=shortcut.Key || !registered) {
                    int nextId=hotkeyId==42?43:42;
                    if(!RegisterHotKey(window.Handle,nextId,key.Modifiers|0x4000,(uint)key.Key))throw new Exception(L("新快捷键被占用，原快捷键已保留"));
                    if(registered)UnregisterHotKey(window.Handle,hotkeyId);hotkeyId=nextId;registered=true;shortcut=key;
                }
                settings=next;icon.Text=L("快速复制引用")+" · "+shortcut.Text;RefreshMenu();WriteState();Notify(L("配置已更新：")+shortcut.Text,false);
            }catch(Exception error){Localizer.SetLanguage(settings.language);Notify(error.Message,true);}
        }
        void Request(){if(disposed || busy || editing)return;busy=true;target=GetForegroundWindow();requested=DateTime.UtcNow;releaseTimer.Start();}
        void Released() {
            if(!KeysReleased(shortcut)){if((DateTime.UtcNow-requested).TotalSeconds>2){releaseTimer.Stop();busy=false;Notify(Message(Failure("keys_held",null)),true);}return;}
            releaseTimer.Stop();if(GetForegroundWindow()!=target){busy=false;Notify(Message(Failure("focus_changed",null)),true);return;}
            IntPtr captured=target;
            ThreadPool.QueueUserWorkItem(s=>{completion=Capture(captured);if(!disposed)PostMessage(window.Handle,0x8003,IntPtr.Zero,IntPtr.Zero);});
        }
        void Completed(){busy=false;if(disposed)return;var result=completion;completion=null;if(result!=null){completedCount++;lastResult=result;SaveError(result);WriteState();if(!result.ok || settings.showSuccessNotification)Notify(Message(result),!result.ok);}}
        void Notify(string text,bool error){if(!disposed)icon.ShowBalloonTip(2000,L("快速复制引用"),text,error?ToolTipIcon.Warning:ToolTipIcon.Info);}
        protected override void Dispose(bool disposing){if(disposing && !disposed){disposed=true;releaseTimer.Stop();releaseTimer.Dispose();if(registered)UnregisterHotKey(window.Handle,hotkeyId);if(copyWait!=null)copyWait.Unregister(null);if(quitWait!=null)quitWait.Unregister(null);copy.Dispose();quit.Dispose();icon.Visible=false;icon.Dispose();window.DestroyHandle();try{File.Delete(StateFile);}catch{}}base.Dispose(disposing);}
    }
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr window,int id);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
}
