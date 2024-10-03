using System.Text.RegularExpressions;
using UIAutomationClient;

namespace WinDigits
{
    public record TaskbarButton(string Name, Rectangle BoundingRectangle);
    public record TaskbarApplication(List<TaskbarButton> Buttons);

    internal static partial class UIAutomation
    {
        private const int UIA_ButtonControlTypeId = 50000;
        private const int UIA_PaneControlTypeId = 50033;

        private const int UIA_BoundingRectanglePropertyId = 30001;
        private const int UIA_ControlTypePropertyId = 30003;
        private const int UIA_AutomationIdPropertyId = 30011;
        private const int UIA_ClassNamePropertyId = 30012;
        private const int UIA_HelpTextPropertyId = 30013;

        [GeneratedRegex("- (\\d+) ")]
        private static partial Regex NumberAfterDash();

        public static Rectangle? GetTaskbarRectangle()
        {
            var uia = new CUIAutomation() ?? throw new InvalidOperationException("Cannot initialize UI Automation framework");
            var root = uia.GetRootElement() ?? throw new InvalidOperationException("Empty response from UI Automation framework");
            var trayWnd = root.FindFirst(TreeScope.TreeScope_Children,
                uia.CreateAndCondition(
                    uia.CreatePropertyCondition(UIA_ControlTypePropertyId, UIA_PaneControlTypeId),
                    uia.CreatePropertyCondition(UIA_ClassNamePropertyId, "Shell_TrayWnd")));
            return trayWnd.CurrentBoundingRectangle.ToRectangle();
        }

        public static IEnumerable<TaskbarApplication> GetTaskbarApplications()
        {
            var uia = new CUIAutomation() ?? throw new InvalidOperationException("Cannot initialize UI Automation framework");
            var root = uia.GetRootElement() ?? throw new InvalidOperationException("Empty response from UI Automation framework");
            var trayWnd = root.FindFirst(TreeScope.TreeScope_Children,
                uia.CreateAndCondition(
                    uia.CreatePropertyCondition(UIA_ControlTypePropertyId, UIA_PaneControlTypeId),
                    uia.CreatePropertyCondition(UIA_ClassNamePropertyId, "Shell_TrayWnd")));
            if (trayWnd == null)
            {
                // No taskbar found - maybe an unsupported version of Windows, or explorer.exe crashed
                yield break;
            }
            var taskbarFrame = trayWnd.FindFirst(TreeScope.TreeScope_Descendants,
                uia.CreateAndCondition(
                    uia.CreatePropertyCondition(UIA_ControlTypePropertyId, UIA_PaneControlTypeId),
                    uia.CreatePropertyCondition(UIA_ClassNamePropertyId, "Taskbar.TaskbarFrameAutomationPeer")));
            if (taskbarFrame == null)
            {
                // No taskbar frame found - maybe an unsupported version of Windows, or explorer.exe crashed
                yield break;
            }
            var buttons = taskbarFrame.FindAll(TreeScope.TreeScope_Descendants,
                uia.CreateAndCondition(
                    uia.CreatePropertyCondition(UIA_ControlTypePropertyId, UIA_ButtonControlTypeId),
                    uia.CreatePropertyCondition(UIA_ClassNamePropertyId, "Taskbar.TaskListButtonAutomationPeer")));
            List<TaskbarButton> windowButtons = [];
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons.GetElement(i);
                var name = button.CurrentName;
                var automationId = button.CurrentAutomationId;
                if (automationId == null || name == null)
                {
                    // Unexpected button type - let's skip it
                    continue;
                }
                var rect = button.CurrentBoundingRectangle;
                if (automationId.StartsWith("Appid:"))
                {
                    // This is an application button
                    yield return new([new(name, rect.ToRectangle())]);
                }
                else if (automationId.StartsWith("Window:"))
                {
                    // This is a window button
                    var match = NumberAfterDash().Matches(name).LastOrDefault();
                    if (match == null || !match.Success)
                    {
                        // We expect the button name to contain the total number of windows
                        continue;
                    }
                    int expectedNumberOfWindows = int.Parse(match.Groups[1].Value);
                    windowButtons.Add(new(name, rect.ToRectangle()));
                    if (windowButtons.Count == expectedNumberOfWindows)
                    {
                        // We've collected the entire application
                        yield return new(windowButtons);
                        windowButtons = [];
                    }
                }
            }
        }

        private static Rectangle ToRectangle(this tagRECT rect) => new()
        {
            X = rect.left,
            Y = rect.top,
            Width = rect.right - rect.left,
            Height = rect.bottom - rect.top,
        };
    }
}
