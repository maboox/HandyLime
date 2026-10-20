using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using HandyLime.Core;

namespace HandyLime.App;

public static class FileActions
{
    public static readonly Operation[] All=[
        new("action:float","Float files","Keep a small floating shelf for dragging files into another app."),
        new("action:copy","Copy files","Copy file references to the clipboard."),
        new("action:copyto","Copy to…","Copy files or folders into a chosen directory without overwriting."),
        new("action:move","Move to…","Move files or folders after confirmation. This changes their location."),
        new("action:path","Copy paths","Copy absolute paths to the clipboard."),
        new("action:reveal","Show in Explorer","Reveal the first file."),
        new("action:terminal","Open terminal","Open PowerShell in the containing folder; no file commands are executed.")];
    public static void Reveal(string path)
    {
        var start=new ProcessStartInfo("explorer.exe") {UseShellExecute=true};
        start.Arguments="/select,\""+path+"\""; Process.Start(start);
    }
    public static async Task Run(App app,string[] paths,Operation operation)
    {
        try {
            switch(operation.Id) {
                case "action:path": Clipboard.SetText(string.Join(Environment.NewLine,paths)); break;
                case "action:copy": var collection=new StringCollection(); collection.AddRange(paths); Clipboard.SetFileDropList(collection); break;
                case "action:float": new ShelfWindow(paths).Show(); break;
                case "action:reveal": Reveal(paths[0]); break;
                case "action:terminal":
                    Process.Start(new ProcessStartInfo("powershell.exe") {UseShellExecute=true,WorkingDirectory=Directory.Exists(paths[0])?paths[0]:Path.GetDirectoryName(paths[0])!}); break;
                case "action:copyto": case "action:move":
                    app.ShowShell(); var dialog=new OpenFolderDialog {Title="Choose destination"}; if(dialog.ShowDialog(app.Shell)!=true)return;
                    var move=operation.Id=="action:move";
                    if(move && Dialogs.Show(app.Shell,$"Move {paths.Length} item(s) to:\n{dialog.FolderName}\n\nTheir original paths will change.","Move files",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
                    await Task.Run(()=> {
                        foreach(var path in paths) {
                            var target=Path.Combine(dialog.FolderName,Path.GetFileName(path.TrimEnd('\\')));
                            if(File.Exists(target)||Directory.Exists(target))throw new IOException("Destination already exists: "+target+". Earlier items may already have completed.");
                            if(Directory.Exists(path) && Path.GetFullPath(target).StartsWith(Path.GetFullPath(path).TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))throw new IOException("Cannot put a folder inside itself.");
                            if(move) { if(Directory.Exists(path))Directory.Move(path,target); else File.Move(path,target); }
                            else if(Directory.Exists(path))CopyFolder(path,target); else File.Copy(path,target,false);
                        }
                    });
                    Reveal(Path.Combine(dialog.FolderName,Path.GetFileName(paths[0]))); break;
            }
        } catch(Exception ex) {Dialogs.Show(app.Shell,ex.Message,"File action",MessageBoxButton.OK,MessageBoxImage.Error);}
    }
    static void CopyFolder(string source,string target)
    {
        if((File.GetAttributes(source)&FileAttributes.ReparsePoint)!=0)throw new IOException("Folder links are not copied.");
        Directory.CreateDirectory(target);
        foreach(var file in Directory.GetFiles(source)) {if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new IOException("File links are not copied."); File.Copy(file,Path.Combine(target,Path.GetFileName(file)),false);}
        foreach(var folder in Directory.GetDirectories(source))CopyFolder(folder,Path.Combine(target,Path.GetFileName(folder)));
    }
}
public sealed class ShelfWindow : Window
{
    public ShelfWindow(string[] paths)
    {
        Title="HandyLime shelf"; Width=320; Height=250; Topmost=true; AllowDrop=true;
        var list=new ListBox {Margin=new Thickness(15),SelectionMode=SelectionMode.Extended,DisplayMemberPath="Name",ItemsSource=paths.Select(p=>new FileItem(p)).ToList()};
        Content=list;
        Point start=default; list.PreviewMouseLeftButtonDown+=(_,e)=>start=e.GetPosition(list);
        list.PreviewMouseMove+=(_,e)=> {
            if(e.LeftButton!=MouseButtonState.Pressed || (e.GetPosition(list)-start).Length<8)return;
            var selection=list.SelectedItems.Cast<FileItem>().Select(f=>f.Path).ToArray(); if(selection.Length==0)return;
            DragDrop.DoDragDrop(list,new DataObject(DataFormats.FileDrop,selection),DragDropEffects.Copy);
        };
    }
}
