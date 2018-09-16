using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


internal struct LASTINPUTINFO
{
    public uint cbSize;
    public uint dwTime;
}

class MiscFunc
{
    [DllImport("User32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("Kernel32.dll")]
    private static extern uint GetLastError();

    public static uint GetIdleTime() // in seconds
    {
        LASTINPUTINFO lastInPut = new LASTINPUTINFO();
        lastInPut.cbSize = (uint)Marshal.SizeOf(lastInPut);
        if (!GetLastInputInfo(ref lastInPut))
        {
            throw new Exception(GetLastError().ToString());
        }
        return ((uint)Environment.TickCount - lastInPut.dwTime)/1000;
    }

    public static int parseInt(string str, int def = 0)
    {
        try
        {
            return int.Parse(str);
        }
        catch
        {
            return def;
        }
    }

    public static String fmt(string str, params object[] args)
    {
        return string.Format(str, args);
    }
}
