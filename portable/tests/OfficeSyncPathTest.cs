using System;
using System.IO;
using System.Collections.Generic;

class OfficeSyncPathTest {
    static int checks;
    static readonly string Namespace="https://tenant.example.test/personal/alex/Documents/";
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static ReferenceCapture.OfficeSyncRoot Root(string url,string mount){return new ReferenceCapture.OfficeSyncRoot{urlNamespace=url,mountPoint=mount};}
    static string Resolve(string url,params ReferenceCapture.OfficeSyncRoot[] roots){return ReferenceCapture.ResolveSyncedOfficePath(url,roots);}
    static string FileAt(string root,string relative){string path=Path.Combine(root,relative);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,"fixture only");return path;}
    static void Refuses(Action operation,string message){bool rejected=false;try{operation();}catch{rejected=true;}Check(rejected,message);}
    [STAThread] static int Main(){
        string temp=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"reference-office-sync-"+Guid.NewGuid().ToString("N")));
        try {
            string a=Path.Combine(temp,"Account A"),b=Path.Combine(temp,"Account B"),nested=Path.Combine(temp,"Nested");
            string relative=Path.Combine("Research Notes","文档 (final) # literal%20.docx");
            string first=FileAt(a,relative),second=FileAt(b,relative);
            string cloud=Namespace+"Research%20Notes/"+Uri.EscapeDataString(Path.GetFileName(first));
            var account=Root(Namespace,a);
            Check(Resolve(cloud,account)==first,"Business URL did not map to its exact local file");
            Check(Resolve(cloud+"?web=1#section",account)==first,"Query or fragment changed source mapping");
            Check(Resolve(cloud,account,Root(Namespace,a))==first,"Duplicate registry entries were ambiguous");
            Check(Resolve(cloud.Replace("tenant.example.test","TENANT.EXAMPLE.TEST"),account)==first,"Host case comparison failed");
            Check(Resolve(cloud,Root(Namespace,b))==second,"Registered second mount was ignored");
            Refuses(()=>Resolve(cloud,account,Root(Namespace,b)),"Two existing synchronized copies were guessed");
            Check(Resolve(cloud.Replace("tenant.example.test","other.example.test"),account)==null,"Cross-tenant URL accepted");
            Check(Resolve(cloud.Replace("/alex/","/other/"),account)==null,"Different account URL accepted");
            Check(Resolve(cloud.Replace("/Documents/","/Documents-other/"),account)==null,"Partial namespace boundary accepted");
            Check(Resolve(cloud.Replace("https://","http://"),account)==null,"HTTP URL accepted as sync authority");
            Check(Resolve(cloud.Replace("https://","https://user@"),account)==null,"Credentials in authority accepted");
            Check(Resolve(cloud.Replace("tenant.example.test","tenant.example.test:444"),account)==null,"Different port accepted");
            Check(Resolve(cloud,Root("https://tenant.example.test/",a))==null,"Generic host namespace accepted");
            Check(Resolve(cloud,Root(Namespace,"relative"))==null,"Relative local mount accepted");
            foreach(string segment in new[]{"%2e%2e","%2Foutside","%5Coutside","%00bad","bad%3Astream","bad%20","bad%"})
                Check(Resolve(Namespace+segment+"/name.docx",account)==null,"Unsafe URL segment accepted: "+segment);
            string personal="https://d.docs.live.net/0123456789abcdef/";
            Check(Resolve(personal+"Research%20Notes/"+Uri.EscapeDataString(Path.GetFileName(first)),Root(personal,a))==first,"Personal CID mapping failed");
            Check(Resolve(personal+"Research%20Notes/name.docx",Root("https://d.docs.live.net/",a))==null,"CID-less namespace accepted");
            string projectFile=FileAt(nested,"proposal.docx");
            FileAt(a,Path.Combine("Project","proposal.docx"));
            var project=Root(Namespace+"Project/",nested);
            Check(Resolve(Namespace+"Project/proposal.docx",account,project)==projectFile,"Most specific sync root did not win");
            File.Delete(projectFile);
            Refuses(()=>Resolve(Namespace+"Project/proposal.docx",account,project),"Missing specific file fell back to a stale broad-root copy");
            Refuses(()=>Resolve(Namespace+"missing.docx",account),"Missing synchronized file silently became a cloud URL");
            Check(Resolve("https://other.example.test/notes.docx",account)==null,"Unknown cloud source was guessed");
            Check(Resolve(first,account)==null,"A local file was treated as a cloud URL");
            Console.WriteLine("Office sync mapping: "+checks+" checks passed (accounts, exact paths, encoding, specific roots, missing files and ambiguity).");return 0;
        }catch(Exception error){Console.Error.WriteLine(error);return 1;}
        finally {
            string prefix=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(temp.StartsWith(prefix,StringComparison.OrdinalIgnoreCase) && Path.GetFileName(temp).StartsWith("reference-office-sync-",StringComparison.Ordinal) && Directory.Exists(temp))Directory.Delete(temp,true);
        }
    }
}
