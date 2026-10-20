using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace HandyLime.App;

// Reads only the current state of six control keys and the pointer. No keyboard hook,
// text collection, injected input, Explorer extension or administrator access.
public sealed class DragTrigger : IDisposable
{
    readonly App app;
    readonly DispatcherTimer timer = new() { Interval=TimeSpan.FromMilliseconds(35) };
    bool wasDown;
    Native.POINT origin;
    readonly Dictionary<int,bool> keys=[];
    WheelWindow? wheel;
    public DragTrigger(App app) {this.app=app; timer.Tick+=Tick; timer.Start();}
    bool Edge(int key) {var down=Native.Down(key);var prior=keys.GetValueOrDefault(key);keys[key]=down;return down&&!prior;}
    void Tick(object? sender,EventArgs e)
    {
        var down=Native.Down(1); Native.GetCursorPos(out var pointer);
        if(down&&!wasDown)origin=pointer;
        var shift=Edge(0x10);var alt=Edge(0x12);var control=Edge(0x11);var escape=Edge(0x1B);var tab=Edge(0x09);
        if(wheel!=null) {
            if(escape || !down) {wheel.Close();wheel=null;}
            else if(shift)wheel.ToggleMode(0);else if(alt)wheel.ToggleMode(1);else if(control)wheel.ToggleMode(2);else if(tab)wheel.NextPage();
        } else if(app.Settings.DragWheelEnabled && down && (Math.Abs(pointer.X-origin.X)>6 || Math.Abs(pointer.Y-origin.Y)>6) && (shift||alt||control)) {
            wheel=new WheelWindow(app,control?2:alt?1:0); wheel.Closed+=(_,_)=>wheel=null; wheel.OpenAtPointer();
        }
        wasDown=down;
    }
    public void Dispose(){timer.Stop();wheel?.Close();}
}
internal static class Native
{
    [StructLayout(LayoutKind.Sequential)] public struct POINT {public int X;public int Y;}
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    public static bool Down(int key)=>(GetAsyncKeyState(key)&0x8000)!=0;
}
