using System;

class DocumentCaptureAdapterTest {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    [STAThread] static int Main(string[] args){try{
        if(args.Length==0){
            Check(ReferenceCapture.PowerPointCaptionMatches("deck.pptx - PowerPoint","deck.pptx"),"PowerPoint title correlation failed");
            Check(!ReferenceCapture.PowerPointCaptionMatches("other.pptx - PowerPoint","deck.pptx"),"PowerPoint accepted a different deck");
            Check(!ReferenceCapture.PowerPointCaptionMatches("deck.pptx - PowerPoint",""),"PowerPoint accepted an empty caption");
            var first=new ReferenceCapture.Item{name="one.md",path="C:/fixture/one.md"};
            var second=new ReferenceCapture.Item{name="two.md",path="C:/fixture/two.md"};
            var duplicateName=new ReferenceCapture.Item{name="one.md",path="C:/other/one.md"};
            Check(ReferenceCapture.ResolveMarkdownSource("one.md",new[]{first},new ReferenceCapture.Item[0])[0]==first,"Single full metadata fallback failed");
            Check(ReferenceCapture.ResolveMarkdownSource("two.md",new[]{first,second},new ReferenceCapture.Item[0])[0]==second,"Title-to-full-metadata fallback failed");
            Check(ReferenceCapture.ResolveMarkdownSource("*two.md - MarkPad",new[]{first,second},new ReferenceCapture.Item[0])[0]==second,"Modified title fallback failed");
            Check(ReferenceCapture.ResolveMarkdownSource("unrelated",new[]{first,second},new[]{first})[0]==first,"Selected full metadata did not take priority");
            foreach(var list in new[]{new[]{first,second},new[]{first,duplicateName}}){bool rejected=false;try{ReferenceCapture.ResolveMarkdownSource(list[1].name=="one.md"?"one.md":"unknown",list,new ReferenceCapture.Item[0]);}catch{rejected=true;}Check(rejected,"Ambiguous Markdown source was silently guessed");}
            bool ambiguousActive=false;try{ReferenceCapture.ResolveMarkdownSource("one.md",new[]{first,second},new[]{first,second});}catch{ambiguousActive=true;}Check(ambiguousActive,"Multiple active Markdown sources not rejected");
            Console.WriteLine("Markdown full-metadata fallback, active priority, title matching and ambiguity protection passed.");
            return 0;
        }
        throw new Exception("Run without arguments; this test does not read live documents");
    }catch(Exception error){Console.WriteLine(error.ToString());return 1;}}
}
