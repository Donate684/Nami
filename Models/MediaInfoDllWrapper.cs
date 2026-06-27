using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nami.Models;

public class MediaInfoDllWrapper : IDisposable
{
    static MediaInfoDllWrapper()
    {
        try
        {
            NativeLibrary.SetDllImportResolver(typeof(MediaInfoDllWrapper).Assembly, ImportResolver);
        }
        catch
        {
            // Ignore if already set
        }
    }

    private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName.Equals("MediaInfo.dll", StringComparison.OrdinalIgnoreCase))
        {
            string arch = RuntimeInformation.ProcessArchitecture == Architecture.X86 ? "x86" : "x64";
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arch, "MediaInfo.dll");
            if (File.Exists(dllPath))
            {
                return NativeLibrary.Load(dllPath, assembly, searchPath);
            }
        }
        return IntPtr.Zero;
    }

    private IntPtr _handle;

    [DllImport("MediaInfo.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr MediaInfo_New();

    [DllImport("MediaInfo.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr MediaInfo_Open(IntPtr handle, string filePath);

    [DllImport("MediaInfo.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr MediaInfo_Inform(IntPtr handle, IntPtr reserved);

    [DllImport("MediaInfo.dll", CharSet = CharSet.Unicode)]
    private static extern void MediaInfo_Close(IntPtr handle);

    [DllImport("MediaInfo.dll", CharSet = CharSet.Unicode)]
    private static extern void MediaInfo_Delete(IntPtr handle);

    public MediaInfoDllWrapper()
    {
        _handle = MediaInfo_New();
    }

    public bool Open(string filePath)
    {
        if (_handle == IntPtr.Zero) return false;
        var res = MediaInfo_Open(_handle, filePath);
        return res != IntPtr.Zero;
    }

    public string Inform()
    {
        if (_handle == IntPtr.Zero) return string.Empty;
        var ptr = MediaInfo_Inform(_handle, IntPtr.Zero);
        return Marshal.PtrToStringUni(ptr) ?? string.Empty;
    }

    public void Close()
    {
        if (_handle != IntPtr.Zero)
        {
            MediaInfo_Close(_handle);
        }
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            MediaInfo_Close(_handle);
            MediaInfo_Delete(_handle);
            _handle = IntPtr.Zero;
        }
    }
}
