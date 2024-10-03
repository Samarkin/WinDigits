using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinDigits
{
    internal static class Program
    {
        private static readonly DigitForm _digitForm = new();
        private static IntPtr _hookID = IntPtr.Zero;

        private static void ShowDigits()
        {
            if (_digitForm.Visible)
            {
                // Already visible, bring to foreground
                Win32.SetForegroundWindow(_digitForm.Handle);
                return;
            }
            _digitForm.AppButtonRectangles = UIAutomation.GetTaskbarApplications()
                .Take(10)
                .Select(app => app.Buttons[0].BoundingRectangle)
                .ToArray();
            _digitForm.Show();
            Win32.SetForegroundWindow(_digitForm.Handle);
        }

        private static void HideDigits()
        {
            if (!_digitForm.Visible)
            {
                // Already hidden, nothing to do
                return;
            }
            _digitForm.Hide();
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Win32.LowLevelKeyboardProc proc = (nCode, wParam, lParam) =>
            {
                if (nCode >= 0 && wParam == Win32.WM_KEYDOWN && Marshal.ReadInt32(lParam) == 91) ShowDigits();
                else if (nCode >= 0 && wParam == Win32.WM_KEYUP && Marshal.ReadInt32(lParam) == 91) HideDigits();
                return Win32.CallNextHookEx(_hookID, nCode, wParam, lParam);
            };
            _hookID = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, proc, Win32.GetModuleHandle(IntPtr.Zero), 0);
            Application.Run();
            Win32.UnhookWindowsHookEx(_hookID);
        }

        class DigitForm : Form
        {
            public Rectangle[] AppButtonRectangles { get; set; } = [];

            public DigitForm()
            {
                var taskbarRectangle = UIAutomation.GetTaskbarRectangle()
                    ?? throw new InvalidOperationException("Failed to locate taskbar");
                StartPosition = FormStartPosition.Manual;
                ClientSize = taskbarRectangle.Size;
                Left = taskbarRectangle.Left;
                Top = taskbarRectangle.Top;
                BackColor = Color.Black;
                ShowInTaskbar = false;
                DoubleBuffered = true;
                FormBorderStyle = FormBorderStyle.None;
                TopLevel = true;
                TransparencyKey = Color.Black;
                SizeGripStyle = SizeGripStyle.Hide;
                TopMost = true;
                Click += (o, e) => Application.Exit();
                Font = new("Tahoma", 14, FontStyle.Bold);
                Paint += OnPaint;
            }

            private void OnPaint(object? sender, PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(TransparencyKey);
                int i = 1;
                foreach (var rect in AppButtonRectangles)
                {
                    var text = (i % 10).ToString();
                    var size = g.MeasureString(text, Font).ToSize();
                    Debug.Assert(size.Height > size.Width);
                    g.FillEllipse(Brushes.Red, rect.X - Left, rect.Y - Top, size.Height, size.Height);
                    g.DrawString(text, Font, Brushes.White, rect.X - Left + (size.Height - size.Width) / 2, rect.Y - Top);
                    i++;
                }
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= Win32.WS_EX_NOACTIVATE; // hide from Alt+Tab
                    return cp;
                }
            }
        }
    }
}