using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
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
            _keyboardHook.KeyPressed += ShowMenu;
            _keyboardHook.RegisterHotKey(ModifierKeys.Shift | ModifierKeys.Control, Keys.G);
        }

        private void _contextMenuStrip_Opened(object? sender, EventArgs e)
        {
            MessageBox.Show("Opened", "Opened", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void _contextMenuStrip_Closed(object? sender, ToolStripDropDownClosedEventArgs e)
        {
            MessageBox.Show("Closed", "Closed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    /// <summary>
    /// Represents the window that is used internally to get the messages.
    /// </summary>
    class Window : NativeWindow, IDisposable
    {
        private static int WM_HOTKEY = 0x0312;

        private HotKeyManager _keyboardHook;

        public Window(HotKeyManager keyboardHook)
        {
            _keyboardHook = keyboardHook;

            // create the handle for the window.
            CreateHandle(new CreateParams());
        }

        /// <summary>
        /// Overridden to get the notifications.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // check if we got a hot key pressed.
            if (m.Msg == WM_HOTKEY)
            {
                // get the keys.
                int id = (int)m.WParam;
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                ModifierKeys modifier = (ModifierKeys)((int)m.LParam & 0xFFFF);

                // invoke the event to notify the parent.
                _keyboardHook.KeyPressed?.Invoke(this, new KeyPressedEventArgs(id, modifier, key));
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
        private Window _window;

        private SortedSet<int> _ids = [];

        /// <summary>
        /// A hot key has been pressed.
        /// </summary>
        public EventHandler<KeyPressedEventArgs>? KeyPressed;

        public HotKeyManager()
        {
            _window = new Window(this);
        }

        /// <summary>
        /// Registers a hot key in the system.
        /// </summary>
        /// <param name="modifier">The modifiers that are associated with the hot key.</param>
        /// <param name="key">The key itself that is associated with the hot key.</param>
        public void RegisterHotKey(ModifierKeys modifier, Keys key)
        {
            // An application must specify an id value in the range 0x0000 through 0xBFFF.
            // see https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey
            int id = -1;
            for (int i = _ids.LastOrDefault(); i <= 0xbfff; i++)
            {
                if (!Win32.RegisterHotKey(_window.Handle, i, (uint)modifier, (uint)key))
                {
                    id = i;
                    _ids.Add(id);
                    break;
                }
            }

            if (id == -1)
            {
                throw new InvalidOperationException("Couldn't register the hot key.");
            }
        }


        #region IDisposable Members

        public void Dispose()
        {
            // unregister all the registered hot keys.
            foreach (int id in _ids)
            {
                Win32.UnregisterHotKey(_window.Handle, id);
            }

            // dispose the inner native window.
            _window.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Event Args for the event that is fired after the hot key has been pressed.
    /// </summary>
    public class KeyPressedEventArgs : EventArgs
    {
        private int _id;

        private ModifierKeys _modifier;

        private Keys _key;

        internal KeyPressedEventArgs(int id, ModifierKeys modifier, Keys key)
        {
            _id = id;
            _modifier = modifier;
            _key = key;
        }

        public int Id
        {
            get { return _id; }
        }

        public ModifierKeys Modifier
        {
            get { return _modifier; }
        }

        public Keys Key
        {
            get { return _key; }
        }
    }

    /// <summary>
    /// The enumeration of possible modifiers.
    /// </summary>
    [Flags]
    public enum ModifierKeys : uint
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }
}