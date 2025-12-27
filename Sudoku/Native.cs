using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Sudoku
{
    internal static partial class Native
    {
        unsafe internal static bool UseImmersiveDarkMode(Window window, bool enabled)
        {
            if (IsWindows10OrGreater(18985))
            {
                HWND hwnd = new(new WindowInteropHelper(window).Handle);
                BOOL dark = new(enabled);

                return 0 == PInvoke.DwmSetWindowAttribute(hwnd,
                    DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                    &dark, sizeof(int));
            }

            return false;
        }

        unsafe internal static void CloakWindow(Window window, bool cloak)
        {
            HWND hwnd = new(new WindowInteropHelper(window).Handle);
            BOOL cloaked = new(cloak); // 1 to enable cloaking, 0 to disable
            PInvoke.DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
                &cloaked, sizeof(int));
        }

        private static bool IsWindows10OrGreater(int build = -1)
        {
            return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;
        }

        private static ImageSource ToImageSource(Icon icon)
        {
            HGDIOBJ hBitmap = new(IntPtr.Zero);
            try
            {
                hBitmap = new(icon.ToBitmap().GetHbitmap());

                ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(32, 32));

                return wpfBitmap;
            }
            finally
            {
                if (hBitmap != IntPtr.Zero)
                {
                    PInvoke.DeleteObject(hBitmap);
                }
            }
        }

        public static ImageSource? ToImageSource(Bitmap bitmap)
        {
            if (bitmap == null) return null;

            // Get a GDI bitmap handle
            HGDIOBJ hBitmap = new(bitmap.GetHbitmap());

            try
            {
                // Use the Windows Presentation Foundation (WPF) Imaging class
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                // Release the GDI handle to prevent memory leaks
                PInvoke.DeleteObject(hBitmap);
            }
        }

        public static void RemoveIcon(Window window)
        {
            var hwnd = new HWND(new WindowInteropHelper(window).Handle);

            // Change the extended window style to not show a window icon
            var extendedStyle = PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            _ = PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, extendedStyle | (int)WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME);

            PInvoke.SendMessage(hwnd, PInvoke.WM_SETICON, PInvoke.ICON_BIG, IntPtr.Zero);
            PInvoke.SendMessage(hwnd, PInvoke.WM_SETICON, PInvoke.ICON_SMALL, IntPtr.Zero);
        }
    }
}
