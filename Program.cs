using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace capricemenu
{
    static class Win32
    {
        #region Win32 API Constants
        public const int WH_KEYBOARD_LL = 0x000D;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        #endregion

        #region Win32 API Structures
        [StructLayout(LayoutKind.Sequential)]
        public class KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public KBDLLHOOKSTRUCTFlags flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [Flags]
        public enum KBDLLHOOKSTRUCTFlags : uint
        {
            KEYEVENTF_EXTENDEDKEY = 0x0001,
            KEYEVENTF_KEYUP = 0x0002,
            KEYEVENTF_SCANCODE = 0x0008,
            KEYEVENTF_UNICODE = 0x0004,
        }
        #endregion

        #region Win32 API Functions
        // Registers a hot key with Windows.
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        // Unregisters the hot key with Windows.
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hmod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetForegroundWindow(HandleRef hWnd);
        #endregion

        #region Delegate
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        #endregion

        #region Fields
//        private KeyboardProc proc;
//        private IntPtr hookId = IntPtr.Zero;
        #endregion
    }


    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new CapriceMenuApplicationContext());
        }
    }


    class CapriceMenuApplicationContext : ApplicationContext
    {
        private NotifyIcon _notifyIcon;

        private ContextMenuStrip _contextMenuStrip;

        private HotKeyManager _keyboardHook;

        public CapriceMenuApplicationContext()
        {
            _contextMenuStrip = new ContextMenuStrip();
            _contextMenuStrip.Opened += _contextMenuStrip_Opened;
            _contextMenuStrip.Closed += _contextMenuStrip_Closed;

            ToolStripMenuItem item;

            item = new ToolStripMenuItem();
            item.Text = "hello";
            item.Click += menuItem1_Click;
            _contextMenuStrip.Items.Add(item);

            item = new ToolStripMenuItem();
            item.Text = "world!";
            item.Click += menuItem1_Click;
            _contextMenuStrip.Items.Add(item);

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = new Icon(SystemIcons.Exclamation, 40, 40);
            _notifyIcon.ContextMenuStrip = _contextMenuStrip;
            _notifyIcon.Text = "NotifyIcon Tooltip";
            _notifyIcon.Visible = true;

            _keyboardHook = new HotKeyManager();
            _keyboardHook.RegisterHotKey(new HotKeyMenu(ModifierKeys.Shift | ModifierKeys.Control, Keys.G));
            _keyboardHook.RegisterHotKey(new HotKeyMenu(ModifierKeys.Shift | ModifierKeys.Control, Keys.J));
        }

        private void _contextMenuStrip_Opened(object? sender, EventArgs e)
        {
            // MessageBox.Show("Opened", "Opened", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void _contextMenuStrip_Closed(object? sender, ToolStripDropDownClosedEventArgs e)
        {
            // MessageBox.Show("Closed", "Closed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void menuItem1_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ShowMenu(object? sender, EventArgs e)
        {
            if (sender != null)
            {
                _contextMenuStrip.Show(Cursor.Position);
            }
        }
    }
    public class HotKeyMenu
    {
        private ModifierKeys _modifierKeys;

        private Keys _keys;

        private int _keyPressPeriod;

        private ContextMenuStrip _contextMenuStrip;

        private ContextMenuStrip CreateContextMenuStrip()
        {
            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();

            ToolStripMenuItem item;

            item = new ToolStripMenuItem();
            item.Text = "hello";
            item.Click += Click;
            contextMenuStrip.Items.Add(item);

            item = new ToolStripMenuItem();
            item.Text = "world!";
            item.Click += Click;
            contextMenuStrip.Items.Add(item);

            return contextMenuStrip;
        }

        public HotKeyMenu(ModifierKeys modifierKeys, Keys keys)
        {
            _modifierKeys = modifierKeys;
            _keys = keys;
            _keyPressPeriod = 400;
            _contextMenuStrip = CreateContextMenuStrip();
        }

        public ModifierKeys ModifierKeys
        {
            get { return _modifierKeys; }
        }

        public Keys Keys
        {
            get { return _keys; }
        }

        public int KeyPressPeriod
        {
            get { return _keyPressPeriod; }
        }

        public ContextMenuStrip ContextMenuStrip
        {
            get { return _contextMenuStrip; }
        }

        private void Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }
    }

    class DummyWindow : NativeWindow, IDisposable
    {
        private static int WM_HOTKEY = 0x0312;

        public EventHandler<KeyPressedEventArgs>? HotKeyPressed;

        public DummyWindow()
        {
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // check if we got a hot key pressed.
            if (m.Msg == WM_HOTKEY)
            {
                // get the keys.
                int id = (int)m.WParam;
                Keys keys = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                ModifierKeys modifierKeys = (ModifierKeys)((int)m.LParam & 0xFFFF);

                // invoke the event
                HotKeyPressed?.Invoke(this, new KeyPressedEventArgs(id, modifierKeys, keys));
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            DestroyHandle();
        }

        #endregion
    }


    public sealed class HotKeyManager : IDisposable
    {
        private DummyWindow _dummyWindow;

        private SortedDictionary<int, HotKeyMenu> _hotKeyMenus = [];

        private int _lastId = -1;

        private DateTime _lastDateTime = DateTime.Now;

        public HotKeyManager()
        {
            _dummyWindow = new DummyWindow();
            _dummyWindow.HotKeyPressed += HotKeyPressed;

        }

        public void RegisterHotKey(HotKeyMenu hotKeyMenu)
        {
            // An application must specify an id value in the range 0x0000 through 0xBFFF.
            // see https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey
            bool registered = false;
            for (int id = _hotKeyMenus.Keys.LastOrDefault(-1) + 1; id <= 0xbfff; id++)
            {
                if (Win32.RegisterHotKey(_dummyWindow.Handle, id, (uint)hotKeyMenu.ModifierKeys, (uint)hotKeyMenu.Keys))
                {
                    _hotKeyMenus.Add(id, hotKeyMenu);
                    registered = true;
                    break;
                }
            }

            if (!registered)
            {
                throw new InvalidOperationException("Couldn't register the hot key.");
            }
        }


        #region IDisposable Members

        public void Dispose()
        {
            // unregister all the registered hot keys.
            foreach (int id in _hotKeyMenus.Keys)
            {
                Win32.UnregisterHotKey(_dummyWindow.Handle, id);
            }

            _hotKeyMenus.Clear();

            // dispose the inner native window.
            _dummyWindow.Dispose();
        }

        #endregion

        private void HotKeyPressed(object? sender, EventArgs eventArgs)
        {
            if (sender == _dummyWindow)
            {
                KeyPressedEventArgs args = (KeyPressedEventArgs)eventArgs;
                if (_hotKeyMenus.TryGetValue(args.Id, out HotKeyMenu? hotKeyMenu))
                {
                    if (CheckSpan(args.Id, hotKeyMenu))
                    {
                        // MessageBox.Show("HotKeyPressed", "HotKeyPressed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        hotKeyMenu.ContextMenuStrip.Show(Cursor.Position);
                    }
                }
            }
        }

        private bool CheckSpan(int id, HotKeyMenu hotKeyMenu)
        {
            bool result = false;

            if ((id == _lastId) && ((DateTime.Now - _lastDateTime).Milliseconds < hotKeyMenu.KeyPressPeriod))
            {
                result = true;
                _lastId = -1;
            }
            else
            {
                _lastId = id;
            }

            _lastDateTime = DateTime.Now;

            return result;
        }
    }

    public class KeyPressedEventArgs : EventArgs
    {
        private int _id;

        private ModifierKeys _modifierKeys;

        private Keys _keys;

        internal KeyPressedEventArgs(int id, ModifierKeys modifierKeys, Keys keys)
        {
            _id = id;
            _modifierKeys = modifierKeys;
            _keys = keys;
        }

        public int Id
        {
            get { return _id; }
        }

        public ModifierKeys ModifierKeys
        {
            get { return _modifierKeys; }
        }

        public Keys Keys
        {
            get { return _keys; }
        }
    }


    [Flags]
    public enum ModifierKeys : uint
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }
}