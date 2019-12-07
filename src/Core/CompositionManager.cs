﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;

namespace Sidebar.Core
{
    public class CompositionManager
    {
        public static event EventHandler ColorizationColorChanged;

        public static bool IsBlurAvailable
        {
            get
            {
                Version OSVersion = Environment.OSVersion.Version;
                // Windows 8 and above have crippled support for Aero Blur
                if ((OSVersion.Major == 6 && OSVersion.Minor > 1))
                {
                    return false;
                }
                return true;
            }
        }

        public static bool IsAcrylicBlurAvailable
        {
            get
            {
                Version OSVersion = Environment.OSVersion.Version;
                // RS4 acrylic blur is the only supported implementation
                if (OSVersion.Major == 10 && OSVersion.Build >= 17134)
                {
                    return true;
                }
                return false;
            }
        }
        public static bool EnableBlurBehindWindow(ref IntPtr handle)
        {
            return SetBlurBehindWindow(ref handle, true);
        }

        public static bool DisableBlurBehindWindow(ref IntPtr handle)
        {
            return SetBlurBehindWindow(ref handle, false);
        }

        private static bool SetBlurBehindWindow(ref IntPtr handle, bool enabled)
        {
            if (IsAcrylicBlurAvailable)
            {
                var accent = new AccentPolicy();
                accent.AccentState = AccentState.ACCENT_DISABLED;
                if (enabled)
                {
                    accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
                }
                accent.GradientColor = (0 << 24) | (0xFFFFFF);

                var accentStructSize = Marshal.SizeOf(accent);

                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData();
                data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.SizeOfData = accentStructSize;
                data.Data = accentPtr;

                NativeMethods.SetWindowCompositionAttribute(handle, ref data);

                Marshal.FreeHGlobal(accentPtr);
            }

            if (IsBlurAvailable)
            {
                BlurBehind bb = new BlurBehind()
                {
                    Enabled = enabled,
                    Flags = BlurBehindFlags.DWM_BB_ENABLE | BlurBehindFlags.DWM_BB_BLURREGION,
                    Region = IntPtr.Zero
                };
                if (NativeMethods.DwmEnableBlurBehindWindow(handle, ref bb) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static void ExcludeFromPeek(IntPtr handle)
        {
            int attributeValue = 1;
            NativeMethods.DwmSetWindowAttribute(
                handle, DwmWindowAttribute.ExcludedFromPeek, ref attributeValue, sizeof(uint));
        }

        public static void ExcludeFromFlip3D(IntPtr handle)
        {
            int attributeValue = (int)Flip3DPolicy.ExcludeBelow;
            NativeMethods.DwmSetWindowAttribute(
                handle, DwmWindowAttribute.Flip3DPolicy, ref attributeValue, sizeof(uint));
        }

        public static System.Windows.Media.Color ColorizationColor
        {
            get
            {
                int color;
                bool opaque;
                NativeMethods.DwmGetColorizationColor(out color, out opaque);
                Color DrawingColor = Color.FromArgb(color);
                return System.Windows.Media.Color.FromArgb(
                    DrawingColor.A, DrawingColor.R, DrawingColor.G, DrawingColor.B);
            }
        }

        internal static IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // WM_DWMCOMPOSITIONCHANGED
            if (msg == 0x0000031E)
            {
                if (IsBlurAvailable)
                {
                    EnableBlurBehindWindow(ref hWnd);
                }
                else
                {
                    DisableBlurBehindWindow(ref hWnd);
                }
            }

            // WM_DWMCOLORIZATIONCOLORCHANGED
            if (msg == 0x0320)
            {
                if (ColorizationColorChanged != null)
                {
                    ColorizationColorChanged(null, EventArgs.Empty);
                }
            }

            return IntPtr.Zero;
        }
    }
}