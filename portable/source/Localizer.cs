using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

public static class Localizer {
    public static string Language {get;private set;}
    public static bool ExplicitLanguage {get;private set;}
    static readonly Dictionary<string,string> English=Load();
    static Dictionary<string,string> Load(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("translations.json")){if(stream==null)throw new Exception("Missing embedded translations");using(var reader=new StreamReader(stream,Encoding.UTF8))return new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(reader.ReadToEnd());}}
    public static string Normalize(string language){if(string.IsNullOrEmpty(language) || language=="auto")return CultureInfo.CurrentUICulture.Name.StartsWith("zh",StringComparison.OrdinalIgnoreCase)?"zh-CN":"en";if(string.Equals(language,"en",StringComparison.OrdinalIgnoreCase))return "en";if(string.Equals(language,"zh-CN",StringComparison.OrdinalIgnoreCase))return "zh-CN";throw new Exception("Unsupported language. Use en or zh-CN.");}
    public static void SetLanguage(string language){Language=Normalize(language);}
    public static void Initialize(string directory,ref string[] args){
        string language=null;var remaining=new List<string>();
        foreach(string arg in args){if(arg.StartsWith("--language=",StringComparison.Ordinal)){if(language!=null)throw new Exception("Specify --language once");language=arg.Substring(11);}else remaining.Add(arg);}
        args=remaining.ToArray();
        if(language==null)language=Environment.GetEnvironmentVariable("VICKY_REFERENCE_LANGUAGE");
        ExplicitLanguage=!string.IsNullOrEmpty(language);
        if(!ExplicitLanguage){try{string file=Path.Combine(directory,"reference-settings.json");if(File.Exists(file)){var settings=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(file,Encoding.UTF8));if(settings!=null && settings.ContainsKey("language"))language=Convert.ToString(settings["language"]);}}catch{}}
        SetLanguage(language);
    }
    public static string Text(string chinese){if(Language!="en")return chinese;string english;if(!English.TryGetValue(chinese,out english))throw new Exception("Missing translation: "+chinese);return english;}
    public static string[] Keys {get{var keys=new string[English.Count];English.Keys.CopyTo(keys,0);return keys;}}
}
