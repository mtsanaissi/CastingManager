using System;
using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;

namespace CastingManager.UWP.Services
{
    internal static class Direct3D11Helpers
    {
        [DllImport("d3d11.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int D3D11CreateDevice(
            IntPtr pAdapter,
            int DriverType,
            IntPtr Software,
            int Flags,
            IntPtr pFeatureLevels,
            int FeatureLevels,
            int SDKVersion,
            out IDirect3DDevice ppDevice,
            out int pFeatureLevel,
            IntPtr ppImmediateContext);

        public static IDirect3DDevice CreateDevice()
        {
            D3D11CreateDevice(IntPtr.Zero, 2, IntPtr.Zero, 0, IntPtr.Zero, 0, 7, out var device, out _, IntPtr.Zero);
            return device;
        }
    }
}
