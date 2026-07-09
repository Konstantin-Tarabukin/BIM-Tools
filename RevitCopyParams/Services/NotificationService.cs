using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCopyParams.UI;
using System.Diagnostics;
using System.Windows.Interop;



namespace RevitCopyParams
{
    public class NotificationService
    {

        public void CheckNotifications(
            Document document,
            string eventName)
        {
            NotificationWindow window =
                new NotificationWindow(
                    document,
                    eventName);

            WindowInteropHelper helper =
                new WindowInteropHelper(window);

            helper.Owner =
                Process.GetCurrentProcess().MainWindowHandle;

            window.ShowDialog();
        }
    }
}