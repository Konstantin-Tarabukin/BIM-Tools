using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System;
using System.Threading;

namespace RevitCopyParams.Services
{
    public static class JsonJournalRuntime
    {
        private static readonly TimeSpan PollInterval =
            TimeSpan.FromSeconds(2.5);

        private static readonly JsonJournalWriter Writer =
            new JsonJournalWriter();

        private static readonly JsonJournalPublisher Publisher =
            new JsonJournalPublisher(Writer);

        private static readonly JsonJournalNotificationService
            Notifications =
                new JsonJournalNotificationService();

        private static UIControlledApplication _application;
        private static DateTime _lastPollUtc = DateTime.MinValue;
        private static int _started;
        private static int _checkInProgress;

        public static void Start(
            UIControlledApplication application)
        {
            if (application == null)
                return;

            if (Interlocked.Exchange(ref _started, 1) == 1)
                return;

            _application = application;
            _application.Idling += OnIdling;
        }

        public static void Stop(
            UIControlledApplication application)
        {
            if (Interlocked.Exchange(ref _started, 0) == 0)
                return;

            try
            {
                if (_application != null)
                    _application.Idling -= OnIdling;
            }
            catch
            {
            }

            _application = null;

            try
            {
                Notifications.SaveState();
            }
            catch
            {
            }

            try
            {
                Writer.Dispose();
            }
            catch
            {
            }
        }

        public static bool HandleDocumentOpened(
            Document document)
        {
            if (!JsonJournalPathService.IsEnabled(document))
                return false;

            SafeCheck(
                document,
                "Открытие файла");

            return true;
        }

        public static bool HandleDocumentSynchronized(
            Document document,
            bool syncSucceeded)
        {
            if (!JsonJournalPathService.IsEnabled(document))
                return false;

            if (syncSucceeded)
            {
                try
                {
                    Publisher.PublishForDocument(document);
                }
                catch (Exception ex)
                {
                    UpdateStartupService.LogDiagnostic(
                        "JSON JOURNAL PUBLISH EXCEPTION | Type=" +
                        ex.GetType().FullName +
                        " | Message=" +
                        ex.Message);
                }
            }

            SafeCheck(
                document,
                "Синхронизация");

            return true;
        }

        public static bool HandleDocumentReloadedLatest(
            Document document)
        {
            if (!JsonJournalPathService.IsEnabled(document))
                return false;

            SafeCheck(
                document,
                "Обновить до последней версии");

            return true;
        }

        private static void OnIdling(
            object sender,
            IdlingEventArgs e)
        {
            UIApplication uiApplication =
                sender as UIApplication;

            if (uiApplication == null ||
                uiApplication.ActiveUIDocument == null)
            {
                return;
            }

            Document document =
                uiApplication.ActiveUIDocument.Document;

            if (!JsonJournalPathService.IsEnabled(document))
                return;

            DateTime nowUtc =
                DateTime.UtcNow;

            if ((nowUtc - _lastPollUtc) < PollInterval)
                return;

            _lastPollUtc = nowUtc;

            SafeCheck(
                document,
                "Фоновая проверка");
        }

        private static void SafeCheck(
            Document document,
            string eventName)
        {
            if (document == null)
                return;

            if (Interlocked.Exchange(
                    ref _checkInProgress,
                    1) == 1)
            {
                return;
            }

            try
            {
                Notifications.CheckNotifications(
                    document,
                    eventName);
            }
            catch (Exception ex)
            {
                try
                {
                    UpdateStartupService.LogDiagnostic(
                        "JSON JOURNAL CHECK EXCEPTION | Event=" +
                        eventName +
                        " | Type=" +
                        ex.GetType().FullName +
                        " | Message=" +
                        ex.Message +
                        " | StackTrace=" +
                        ex.StackTrace);
                }
                catch
                {
                }
            }
            finally
            {
                Interlocked.Exchange(
                    ref _checkInProgress,
                    0);
            }
        }
    }
}
