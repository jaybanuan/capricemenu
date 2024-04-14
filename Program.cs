namespace capricemenu
{
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

        public CapriceMenuApplicationContext()
        {
            _contextMenuStrip = new ContextMenuStrip();

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
        }

        private void menuItem1_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}