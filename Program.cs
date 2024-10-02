using System.Diagnostics;
using System.Runtime.InteropServices;
using UIAutomationClient;

namespace WinDigits
{
    internal static class Program
    {
        private static readonly List<Form> _digitForms = [];
        private static IntPtr _hookID = IntPtr.Zero;

        private static void ShowDigits()
        {
            if (_digitForms.Any())
            {
                return;
            }
            var uia = new CUIAutomation();
            var root = uia.GetRootElement();
            var trayWnd = root.FindFirst(TreeScope.TreeScope_Children,
                uia.CreateAndCondition(
                    uia.CreatePropertyCondition(UIA_PropertyIdConstants.UIA_ControlTypePropertyId, UIA_ControlTypeIdConstants.UIA_PaneControlTypeId),
                    uia.CreatePropertyCondition(UIA_PropertyIdConstants.UIA_ClassNamePropertyId, "Shell_TrayWnd")));
            var taskbarFrame = trayWnd.FindFirst(TreeScope.TreeScope_Descendants,
                uia.CreateAndCondition(
                    uia.CreatePropertyCondition(UIA_PropertyIdConstants.UIA_ControlTypePropertyId, UIA_ControlTypeIdConstants.UIA_PaneControlTypeId),
                    uia.CreatePropertyCondition(UIA_PropertyIdConstants.UIA_ClassNamePropertyId, "Taskbar.TaskbarFrameAutomationPeer")));
            var buttons = taskbarFrame.FindAll(TreeScope.TreeScope_Descendants,
                uia.CreateAndCondition(
                    uia.CreatePropertyCondition(UIA_PropertyIdConstants.UIA_ControlTypePropertyId, UIA_ControlTypeIdConstants.UIA_ButtonControlTypeId),
                    uia.CreatePropertyCondition(UIA_PropertyIdConstants.UIA_ClassNamePropertyId, "Taskbar.TaskListButtonAutomationPeer")));
            int buttonCount = Math.Min(buttons.Length, 10);
            if (buttonCount == 0)
            {
                return;
            }
            for (int i = 0; i < buttonCount; i++)
            {
                var button = buttons.GetElement(i);
                var name = button.CurrentName;
                var automationId = button.CurrentAutomationId;
                var helpText = button.CurrentHelpText;
                var rect = button.CurrentBoundingRectangle;
                var form = new DigitForm((i + 1) % 10, rect.left, rect.top);
                _digitForms.Add(form);
                form.Show();
            }
        }

        private static void HideDigits()
        {
            foreach (var form in _digitForms)
            {
                form.Close();
            }
            _digitForms.Clear();
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
            var _dummyForm = new Form();
            _dummyForm.Show();
            _dummyForm.Hide();
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
            public DigitForm(int digit, int x, int y)
            {
                ClientSize = new Size(20, 20);
                StartPosition = FormStartPosition.Manual;
                Left = x;
                Top = y;
                BackColor = Color.Black;
                ShowInTaskbar = false;
                DoubleBuffered = true;
                FormBorderStyle = FormBorderStyle.None;
                TopLevel = true;
                TransparencyKey = Color.Black;
                SizeGripStyle = SizeGripStyle.Hide;
                TopMost = true;
                Click += (o, e) => Application.Exit();
                string text = digit.ToString();
                var font = new Font("Tahoma", 14, FontStyle.Bold);
                Paint += (o, e) =>
                {
                    var g = e.Graphics;
                    var size = g.MeasureString(text, font).ToSize();
                    Debug.Assert(size.Height > size.Width);
                    g.FillEllipse(Brushes.Red, 0, 0, size.Height, size.Height);
                    g.DrawString(text, font, Brushes.White, (size.Height - size.Width) / 2, 0);
                };
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