using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace Desktop_navigation
{
    internal class Clicking
    {

        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy,
                 int dwData, int dwExtraInfo);

        [Flags]
        public enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }

        public static void scrollOn(ref DateTime prScroll)
        {
            prScroll = DateTime.Now;
            mouse_event((int)MouseEventFlags.MIDDLEDOWN, 0, 0, 0, 0);
        }

        public static void scrollOff(ref DateTime prScroll)
        {
            prScroll = DateTime.Now;
            mouse_event((int)MouseEventFlags.MIDDLEUP, 0, 0, 0, 0);
        }

        public static void leftClick(Point p)
        {
            Cursor.Position = p;
            mouse_event((int)(MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            mouse_event((int)(MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            //Thread.Sleep(tempo);
        }

        public static void rightClick(Point p)
        {
            Cursor.Position = p;
            mouse_event((int)(MouseEventFlags.RIGHTDOWN), 0, 0, 0, 0);
            mouse_event((int)(MouseEventFlags.RIGHTUP), 0, 0, 0, 0);
            //Thread.Sleep(tempo);
        }

        public void doubleRightClick(Point p)
        {
            Cursor.Position = p;
            mouse_event((int)(MouseEventFlags.RIGHTDOWN), 0, 0, 0, 0);
            mouse_event((int)(MouseEventFlags.RIGHTUP), 0, 0, 0, 0);
            mouse_event((int)(MouseEventFlags.RIGHTDOWN), 0, 0, 0, 0);
            mouse_event((int)(MouseEventFlags.RIGHTUP), 0, 0, 0, 0);
        }

        public void holDownLeft(Point p, int tempo)
        {
            Cursor.Position = p;
            mouse_event((int)(MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            Thread.Sleep(tempo);
            mouse_event((int)(MouseEventFlags.LEFTUP), 0, 0, 0, 0);
        }

        public void holDownRight(Point p, int tempo)
        {
            Cursor.Position = p;
            mouse_event((int)(MouseEventFlags.RIGHTDOWN), 0, 0, 0, 0);
            Thread.Sleep(tempo);
            mouse_event((int)(MouseEventFlags.RIGHTUP), 0, 0, 0, 0);
        }

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        public static void movePointerPosition(int x, int y)
        {
            Point po = new Point(x, y);
            SetCursorPos(po.X, po.Y);
        }

    }
}
