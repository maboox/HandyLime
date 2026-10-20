using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
namespace HandyLime.App;

public static class Theme
{
    public static string Preference{get;private set;}="System";
    public static bool Dark{get;private set;}
    public static Brush Brush(string name)=>(Brush)Application.Current.Resources[name];
    public static void Apply(string preference)
    {
        Preference=preference;var systemDark=false;
        if(preference=="System")try{using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");systemDark=(int?)key?.GetValue("AppsUseLightTheme")==0;}catch(System.Security.SecurityException){}
        Dark=preference=="Dark"||preference=="System"&&systemDark;
        string[] names=["Ink","Muted","Accent","AccentInk","Line","Canvas","Surface","Subtle","Selected","Danger","Stage","Sidebar","SidebarInk","SidebarMuted"];
        string[] light=["#202A18","#58664C","#B9E44D","#1C2B08","#D8E1CC","#F4F7EF","#FFFFFF","#EAF0E1","#E1F0BD","#B32635","#161C14","#1E2A1C","#F3F8E9","#B6C5A6"];
        string[] dark=["#EFF5E6","#AFBFA0","#C1EC62","#1A2808","#39472E","#151B13","#20291C","#2B3624","#394D24","#FF919C","#0C100B","#11180F","#F3F8E9","#B6C5A6"];
        var palette=Dark?dark:light;
        for(int i=0;i<names.Length;i++){
            var color=(Color)ColorConverter.ConvertFromString(palette[i]);
            if(Application.Current.Resources[names[i]] is SolidColorBrush existing&&!existing.IsFrozen)existing.Color=color;else Application.Current.Resources[names[i]]=new SolidColorBrush(color);
        }
    }
}
