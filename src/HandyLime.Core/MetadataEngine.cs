using System.Text.Json;
namespace HandyLime.Core;
public sealed record MetadataEntry(string Property,string Value);
public static class MetadataEngine
{
    public static async Task<MetadataEntry[]> Read(string path,Engines engines,CancellationToken token)
    {
        var rows=new List<MetadataEntry>{new("File",Path.GetFileName(path)),new("Size",$"{new FileInfo(path).Length:N0} bytes")};
        if(Catalog.Kind(path)==FileKind.Image){
            await Task.Run(()=>{
                using var image=ImageEngine.Read(path,false);rows.Add(new("Dimensions",$"{image.Width} × {image.Height}"));rows.Add(new("Color space",image.ColorSpace.ToString()));
                foreach(var name in image.AttributeNames){token.ThrowIfCancellationRequested();rows.Add(new(name,Limit(image.GetAttribute(name))));}
                foreach(var name in image.ProfileNames)rows.Add(new("Profile",name));
                var exif=image.GetExifProfile();if(exif!=null)foreach(var item in exif.Values)rows.Add(new("EXIF · "+item.Tag,Limit(item.ToString())));
            },token);
        }else{
            var json=await ProcessRunner.Run(engines.Require("FFprobe"),["-v","error","-protocol_whitelist","file,pipe","-show_format","-show_streams","-of","json",path],token);
            using var doc=JsonDocument.Parse(json);Flatten(doc.RootElement,"");
            void Flatten(JsonElement element,string prefix){
                if(rows.Count>1500)return;
                if(element.ValueKind==JsonValueKind.Object){foreach(var property in element.EnumerateObject())Flatten(property.Value,prefix.Length==0?property.Name:prefix+" · "+property.Name);}
                else if(element.ValueKind==JsonValueKind.Array){var i=0;foreach(var item in element.EnumerateArray())Flatten(item,prefix+" "+i++);}
                else if(!prefix.EndsWith("filename"))rows.Add(new(prefix,Limit(element.ToString())));
            }
        }
        return rows.ToArray();
    }
    static string Limit(string? value)=>value is null?"":value.Length>4096?value[..4096]+"…":value;
}
