using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
class LanguageDialogTest {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Record(TextBox input,Keys key){typeof(Control).GetMethod("OnKeyDown",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(input,new object[]{new KeyEventArgs(key)});}
    [STAThread] static int Main(){try{
        Application.EnableVisualStyles();string[] args=new string[0];Localizer.Initialize(PortableReference.Root,ref args);
        Localizer.SetLanguage("zh-CN");
        Exception failure=null;bool completed=false;
        using(var timer=new Timer{Interval=150}){
            timer.Tick+=(s,e)=>{var form=Application.OpenForms.Cast<Form>().FirstOrDefault();if(form==null)return;timer.Stop();try{
                form.Opacity=0;
                var controls=form.Controls.Cast<Control>().ToArray();var languages=controls.OfType<ComboBox>().Single();var input=controls.OfType<TextBox>().Single();
                Check(form.Text=="设置复制引用快捷键","Chinese dialog title missing");Record(input,Keys.Control|Keys.C);
                Check(!controls.OfType<Button>().Single(b=>b.Text=="保存并启用").Enabled,"Ctrl+C accepted in Chinese");
                languages.SelectedIndex=1;Check(form.Text=="Set Copy Reference shortcut","Language preview did not refresh title");
                Check(!controls.OfType<Button>().Single(b=>b.Text=="Save and enable").Enabled,"Ctrl+C accepted after language switch");
                Check(controls.OfType<Label>().Any(label=>label.Text.Contains("normal copy, cut, and paste")),"English protected shortcut guidance missing");
                Check(controls.OfType<CheckBox>().Any(c=>c.Text=="Start with Windows"),"Startup checkbox was not translated");
                Record(input,Keys.Control|Keys.Alt|Keys.Shift|Keys.F23);Check(controls.OfType<Button>().Single(b=>b.Text=="Save and enable").Enabled,"Custom shortcut rejected");
                completed=true;controls.OfType<Button>().Single(b=>b.Text=="Cancel").PerformClick();
            }catch(Exception error){failure=error;form.DialogResult=DialogResult.Cancel;form.Close();}};
            timer.Start();var result=PortableReference.Configure(new PortableReference.Settings{hotkey="Ctrl+Alt+Shift+F23",language="zh-CN",showSuccessNotification=false});
            if(failure!=null)throw failure;Check(completed && result==null,"Cancel did not close the test dialog");Check(Localizer.Language=="zh-CN","Cancel did not restore previous language");Check(!File.Exists(Path.Combine(PortableReference.Root,"reference-settings.json")),"Cancel wrote settings");
        }
        completed=false;
        using(var timer=new Timer{Interval=150}){
            timer.Tick+=(s,e)=>{var form=Application.OpenForms.Cast<Form>().FirstOrDefault();if(form==null)return;timer.Stop();try{
                form.Opacity=0;var controls=form.Controls.Cast<Control>().ToArray();controls.OfType<ComboBox>().Single().SelectedIndex=1;
                controls.OfType<Button>().Single(b=>b.Text=="Save and enable").PerformClick();completed=true;
            }catch(Exception error){failure=error;form.DialogResult=DialogResult.Cancel;form.Close();}};
            timer.Start();var result=PortableReference.Configure(new PortableReference.Settings{hotkey="Ctrl+Alt+Shift+F23",language="zh-CN",showSuccessNotification=false});
            if(failure!=null)throw failure;Check(completed && result!=null && result.language=="en","Selected language was not saved");
            var saved=PortableReference.ReadSettings();Check(saved.language=="en" && saved.hotkey=="Ctrl+Alt+Shift+F23" && !saved.showSuccessNotification && saved.configured,"Saved language lost shortcut preferences");
        }
        File.WriteAllText(Path.Combine(PortableReference.Root,"reference-settings.json"),"{\"hotkey\":\"Ctrl+Alt+Shift+F23\",\"configured\":true,\"showSuccessNotification\":false}");
        var legacy=PortableReference.ReadSettings();Check(legacy.hotkey=="Ctrl+Alt+Shift+F23" && legacy.configured && !legacy.showSuccessNotification,"Legacy configuration migration changed preferences");
        Console.WriteLine("Live settings dialogs: bilingual preview, Ctrl+C protection, cancel restoration, persisted language and legacy preferences passed.");return 0;
    }catch(Exception error){Console.WriteLine(error.ToString());return 1;}}
}
