using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OP.WebWidget
{
    public partial class FormBrowser : Form
    {

        ChromiumWebBrowser _browser;
        Options _options;

        [DllImport( "user32.dll", SetLastError = true)]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpWindowClass, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOACTIVATE = 0x0010;
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        const int GWL_HWNDPARENT = -8;

        public FormBrowser(Options options)
        {
            InitializeComponent();
            btnBack.Text = "";
            btnForward.Text = "";
            _options = options;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            IntPtr hprog = FindWindowEx(
                FindWindowEx(
                    FindWindow("Progman", "Program Manager"),
                    IntPtr.Zero, "SHELLDLL_DefView", ""
                ),
                IntPtr.Zero, "SysListView32", "FolderView"
            );

            // Set the explorer as parent window
            SetWindowLong(this.Handle, GWL_HWNDPARENT, hprog);

            if (_options.Size != null)
            {
                this.Size = _options.Size.Value;
            }

            if (_options.Position != null)
            {
                Screen screen = Screen.PrimaryScreen;
                if (_options.Screen != null && Screen.AllScreens.Length >= _options.Screen.Value)
                    screen = Screen.AllScreens[_options.Screen.Value - 1];

                if (_options.Position.Value.X < 0)
                    this.Left = screen.Bounds.Right - this.Width + _options.Position.Value.X;
                else
                    this.Left = _options.Position.Value.X + screen.Bounds.Left;

                if (_options.Position.Value.Y < 0)
                    this.Top = screen.Bounds.Bottom - this.Height + _options.Position.Value.Y;
                else
                    this.Top = _options.Position.Value.Y + screen.Bounds.Top;
            }
            if(_options.BorderStyle == "Fixed")
            {
                this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            }
            else if (_options.BorderStyle == "Sizable")
            {
                this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.None;
            }


            this.panelToolbar.Visible = !_options.NoToolbar;
            this.btnBack.Visible = !_options.NoToolbar;
            this.btnForward.Visible = !_options.NoToolbarNext;
            this.btnRefresh.Visible = !_options.NoToolbarPrev;
            this.btnHome.Visible = !_options.NoToolbarHome;
            this.btnClose.Visible = !_options.NoToolbarClose && this.FormBorderStyle == FormBorderStyle.None;

            if (!_options.NoSendToBack)
                MoveToBack();
            InitChromium();

        }

    
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            if (!_options.NoSendToBack)
                MoveToBack();
        }

        private void InitChromium()
        {
            _browser = new ChromiumWebBrowser(_options.URL);
            _browser.IsBrowserInitializedChanged += _browser_IsBrowserInitializedChanged;
            _browser.TitleChanged += _browser_TitleChanged;
            _browser.LoadingStateChanged += _browser_LoadingStateChanged;
            _browser.Dock = DockStyle.Fill;
            _browser.MenuHandler = new MenuHandler();
            panelBrowser.Controls.Add(_browser);
            _browser.Focus();
        }

        private void OrganizeLayout()
        {
            this.btnBack.Enabled = _browser.CanGoBack ;
            this.btnForward.Enabled = _browser.CanGoForward ;
        }

        private void _browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            this.BeginInvoke(new Action(() => OrganizeLayout()));
        }

        private void _browser_TitleChanged(object sender, TitleChangedEventArgs e)
        {
            String title = e.Title;
            this.BeginInvoke(new Action(() => this.Text = title));
        }


        private void MoveToBack()
        {
            this.BeginInvoke(new Action(() => SetWindowPos(this.Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE)));
        }

        private void _browser_IsBrowserInitializedChanged(object sender, IsBrowserInitializedChangedEventArgs e)
        {
            //if (e.IsBrowserInitialized)
            //{
            //    _browser.SetZoomLevel(0);
            //    //this.BeginInvoke(new Action(() => _browser.Focus());
            //}
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if(_browser.CanGoBack)
                _browser.Back();
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            if (_browser.CanGoForward)
                _browser.Forward();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            _browser.Refresh();
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            _browser.Load(_options.URL);
        }

        private class MenuHandler : IContextMenuHandler
        {
            public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
            {
                model.Clear();
            }

            public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
            {
                return true;
            }

            public void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame)
            {
            }

            public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
            {
                return true;
            }
        }
    }
}
