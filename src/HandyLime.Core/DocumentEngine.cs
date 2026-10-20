namespace HandyLime.Core;

public static class DocumentEngine
{
    public static async Task Convert(string input, string ext, string output, string engine, Engines engines, CancellationToken token)
    {
        var temp = Path.Combine(Path.GetDirectoryName(output)!,"office"); Directory.CreateDirectory(temp);
        if (engine == "Pandoc") {
            var source = Catalog.Extension(input) switch { "md" => "markdown", "txt" => "markdown", var e => e };
            var target = ext switch { "txt" => "plain", "md" => "markdown", _ => ext };
            await ProcessRunner.Run(engines.Require("Pandoc"),["--sandbox","--standalone","--from",source,"--to",target,"--output",output,input],token,workingDirectory:temp);
            return;
        }
        var profile = Path.Combine(temp,"profile"); Directory.CreateDirectory(profile);
        // Disable macros and external link updates in a disposable per-job profile.
        await File.WriteAllTextAsync(Path.Combine(profile,"registrymodifications.xcu"),"""
            <?xml version="1.0" encoding="UTF-8"?><oor:items xmlns:oor="http://openoffice.org/2001/registry">
            <item oor:path="/org.openoffice.Office.Common/Security/Scripting"><prop oor:name="MacroSecurityLevel" oor:op="fuse"><value>3</value></prop></item>
            <item oor:path="/org.openoffice.Office.Calc/Content/Update"><prop oor:name="Link" oor:op="fuse"><value>0</value></prop></item>
            <item oor:path="/org.openoffice.Office.Writer/Content/Update"><prop oor:name="Link" oor:op="fuse"><value>0</value></prop></item>
            </oor:items>
            """,token);
        var exports = Path.Combine(temp,"exports"); Directory.CreateDirectory(exports);
        var filter = ext switch {
            "tsv" => "csv:Text - txt - csv (StarCalc):9,34,76,1",
            "csv" => "csv:Text - txt - csv (StarCalc):44,34,76,1",
            "txt" => "txt:Text (encoded):UTF8",
            _ => ext
        };
        // Copy to a controlled name to prevent option-like file names and office output collisions.
        var localInput = Path.Combine(temp,"input."+Catalog.Extension(input)); File.Copy(input,localInput);
        var args = new List<string> {"-env:UserInstallation="+new Uri(profile).AbsoluteUri,"--headless","--nologo","--nodefault","--nolockcheck","--norestore"};
        if (Catalog.Extension(input) is "csv" or "tsv") args.Add("--infilter=Text - txt - csv (StarCalc):"+(Catalog.Extension(input)=="tsv"?"9":"44")+",34,76,1");
        args.AddRange(["--convert-to",filter,"--outdir",exports,localInput]);
        await ProcessRunner.Run(engines.Require("LibreOffice"),args,token,workingDirectory:temp);
        var result = Path.Combine(exports,"input."+(ext == "tsv" ? "csv" : ext));
        // Same-format operations are excluded by the catalog, so result cannot be the copied source.
        if (!File.Exists(result)) throw new IOException("LibreOffice did not create an output. The source format or export filter may not be supported.");
        if (ext == "html") Directory.Move(exports,output);
        else File.Move(result,output);
    }
}
