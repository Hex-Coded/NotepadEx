using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Point = System.Windows.Point;

namespace NotepadEx.Util
{
    public class WindowChrome
    {
        private readonly Window _window;
        private HwndSource _hwndSource;

        private const int WM_NCHITTEST = 0x0084;
        private const int WM_GETMINMAXINFO = 0x0024;

        private readonly int _resizeBorderWidth = (int)UIConstants.ResizeBorderWidth;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        public WindowChrome(Window window)
        {
            _window = window;
        }

        public void Enable()
        {
            if(PresentationSource.FromVisual(_window) is HwndSource source)
            {
                _hwndSource = source;
                _hwndSource.AddHook(WndProc);
            }
        }

        public void Detach()
        {
            if(_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch(msg)
            {
                case WM_NCHITTEST:
                    return HandleHitTest(lParam, ref handled);

                case WM_GETMINMAXINFO:
                    HandleGetMinMaxInfo(lParam);
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }

        private IntPtr HandleHitTest(IntPtr lParam, ref bool handled)
        {
            if(_window.WindowState == WindowState.Maximized)
            {
                return IntPtr.Zero;
            }

            int x = lParam.ToInt32() & 0xFFFF;
            int y = lParam.ToInt32() >> 16;
            var screenPoint = new Point(x, y);

            var windowPoint = _window.PointFromScreen(screenPoint);

            var result = HitTest(windowPoint);

            if(result != HitTestValues.HTCLIENT)
            {
                handled = true;
                return new IntPtr((int)result);
            }

            return IntPtr.Zero;
        }

        private void HandleGetMinMaxInfo(IntPtr lParam)
        {
            var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            IntPtr hMonitor = MonitorFromWindow(_hwndSource.Handle, MONITOR_DEFAULTTONEAREST);

            if(hMonitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                GetMonitorInfo(hMonitor, ref monitorInfo);

                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;

                mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
                mmi.ptMaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        private HitTestValues HitTest(Point point)
        {
            // 1. Check for resize handles in the border area
            bool onLeft = point.X <= _resizeBorderWidth;
            bool onRight = point.X >= _window.ActualWidth - _resizeBorderWidth;
            bool onTop = point.Y <= _resizeBorderWidth;
            bool onBottom = point.Y >= _window.ActualHeight - _resizeBorderWidth;

            if(onTop && onLeft) return HitTestValues.HTTOPLEFT;
            if(onTop && onRight) return HitTestValues.HTTOPRIGHT;
            if(onBottom && onLeft) return HitTestValues.HTBOTTOMLEFT;
            if(onBottom && onRight) return HitTestValues.HTBOTTOMRIGHT;
            if(onLeft) return HitTestValues.HTLEFT;
            if(onRight) return HitTestValues.HTRIGHT;
            if(onTop) return HitTestValues.HTTOP;
            if(onBottom) return HitTestValues.HTBOTTOM;

            // 2. Check if the point is within the title bar's height
            if(point.Y <= 24)
            {
                double windowWidth = _window.ActualWidth;
                // The total width of the three system buttons (Minimize, Maximize, Close are 30px each)
                double systemButtonsWidth = 90;

                // 3. Check if the cursor is over the area of the system buttons on the right
                if(point.X >= windowWidth - systemButtonsWidth)
                {
                    // 4. If so, let WPF handle the click. The buttons will now be hoverable and clickable.
                    return HitTestValues.HTCLIENT;
                }

                // 5. If not over the buttons but still in the title bar, it's the caption for dragging.
                return HitTestValues.HTCAPTION;
            }

            // 6. If it's not a resize handle or the caption, it's the main client area.
            return HitTestValues.HTCLIENT;
        }

        private enum HitTestValues
        {
            HTERROR = -2,
            HTTRANSPARENT = -1,
            HTNOWHERE = 0,
            HTCLIENT = 1,
            HTCAPTION = 2,
            HTSYSMENU = 3,
            HTGROWBOX = 4,
            HTMENU = 5,
            HTHSCROLL = 6,
            HTVSCROLL = 7,
            HTMINBUTTON = 8,
            HTMAXBUTTON = 9,
            HTLEFT = 10,
            HTRIGHT = 11,
            HTTOP = 12,
            HTTOPLEFT = 13,
            HTTOPRIGHT = 14,
            HTBOTTOM = 15,
            HTBOTTOMLEFT = 16,
            HTBOTTOMRIGHT = 17,
            HTBORDER = 18,
            HTOBJECT = 19,
            HTCLOSE = 20,
            HTHELP = 21
        }
    }
}