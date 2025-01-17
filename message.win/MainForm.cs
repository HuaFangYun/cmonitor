using System.Runtime.InteropServices;

namespace message.win
{
    public partial class MainForm : Form
    {
        private readonly int times = 10;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_APPWINDOW = 0x40000;
                const int WS_EX_TOOLWINDOW = 0x80;
                CreateParams cp = base.CreateParams;
                cp.ExStyle &= (~WS_EX_APPWINDOW);
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        public MainForm(string msg, int times)
        {
            InitializeComponent();
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            label2.Text = msg;
            this.times = times;


        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        public const uint SWP_NOMOVE = 0x2;
        public const uint SWP_NOSIZE = 0x1;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        private const uint SWP_FRAMECHANGED = 0x20;

        bool autoClose = false;
        private void Countdown()
        {
            new Thread(() =>
            {
                for (int i = times; i > 0; i--)
                {
                    this.Invoke(new EventHandler(delegate
                    {
                        label3.Text = i.ToString() + "s";
                    }));
                    System.Threading.Thread.Sleep(1000);
                }
                autoClose = true;
                this.Close();
            }).Start();
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
#if RELEASE
            if(autoClose == false)
            {
                e.Cancel = true;
            }
#endif
        }

        private void OnLoad(object sender, EventArgs e)
        {
            Countdown();

            //�������ö�
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            int style = GetWindowLong(this.Handle, GWL_STYLE);
            SetWindowLong(this.Handle, GWL_STYLE, style & ~WS_SYSMENU);

            // ���Ӵ�����Ӱ
            SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOSIZE | SWP_NOMOVE);

            //�������õ�
            //SetWindowPos(this.Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }
    }
}
