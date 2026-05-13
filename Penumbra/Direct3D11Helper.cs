using System;
using System.Runtime.InteropServices;

namespace Penumbra.LTCImport.Dds;

public static class Direct3D11Helper
{
    [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int D3D11CreateDevice(
        IntPtr pAdapter,
        int driverType,
        IntPtr software,
        uint flags,
        IntPtr pFeatureLevels,
        uint featureLevels,
        uint sdkVersion,
        out IntPtr ppDevice,
        out int pFeatureLevel,
        out IntPtr ppImmediateContext);

    private static IntPtr _device = IntPtr.Zero;
    private static IntPtr _context = IntPtr.Zero;
    private static bool _initialized = false;
    private static readonly object _lock = new object();

    public static IntPtr GetDevice()
    {
        if (_initialized) return _device;

        lock (_lock)
        {
            if (_initialized) return _device;

            int hr = D3D11CreateDevice(
                IntPtr.Zero,
                1, // D3D_DRIVER_TYPE_HARDWARE
                IntPtr.Zero,
                0, // flags
                IntPtr.Zero,
                0, // featureLevels array size
                7, // D3D11_SDK_VERSION
                out _device,
                out _,
                out _context);

            if (hr < 0)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create D3D11 device. HRESULT: {hr:X}");
                _device = IntPtr.Zero;
                _context = IntPtr.Zero;
            }

            _initialized = true;
            return _device;
        }
    }
}
