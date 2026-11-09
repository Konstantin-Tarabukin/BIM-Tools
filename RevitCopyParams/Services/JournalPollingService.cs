using Autodesk.Revit.DB;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using RevitCopyParams.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace RevitCopyParams.Services
{
    // Фоновый polling внешнего JSON-журнала (ADR-014, этап A.10).
    //
    // Цикл опроса:
    //   Timer (background) -> текущий журнал проекта -> датные каталоги
    //   -> новые JSON -> десериализация -> проверка EventId -> уведомление.
    //
    // Принципы:
    //   * файл I/O выполняется вне UI-потока Revit (A.7);
    //   * Revit API не вызывается из background-потока: контекст
    //     (журнал, модель, имя пользователя) фиксируется заранее
    //     на Revit API-потоке через UpdateContext;
    //   * уведомление показывается через WPF Dispatcher;
    //   * дедупликация "при показе": событие помечается обработанным
    //     в локальном состоянии до показа окна (A.14);
    //   * собственные события и события, обработанные по каналу
    //     Extensible Storage, не показываются повторно;
    //   * ошибки цикла не останавливают polling.
    public static class JournalPollingService
    {
        private static readonly JsonJournalReader Reader =
            new JsonJournalReader();

        private static System.Timers.Timer timer;

        private static int busy;

        private static JournalLocation currentLocation;

        private static string currentModelName;

        private static string currentUsername;


        public static void Start()
        {
            if (timer != null)
                return;

            int intervalSeconds =
                Math.Max(2, JournalSettings.PollingIntervalSeconds);

            timer = new System.Timers.Timer(intervalSeconds * 1000);

            timer.AutoReset = false;

            timer.Elapsed += OnTimerElapsed;

            timer.Start();

            Log(
                "JOURNAL POLLING START | Interval=" +
                intervalSeconds +
                "s | LookbackDays=" +
                JournalSettings.LookbackDays);
        }


        public static void Stop()
        {
            if (timer == null)
                return;

            timer.Stop();

            timer.Dispose();

            timer = null;

            Log("JOURNAL POLLING STOP");
        }


        // Фиксирует контекст опроса. Вызывается ТОЛЬКО из Revit API-потока
        // (DocumentOpened / DocumentSynchronized / DocumentReloadedLatest /
        // DocumentActivated). Ни Revit API, ни сеть в background не трогаем.
        public static void UpdateContext(Document document)
        {
            try
            {
                if (document == null)
                {
                    ResetContext();

                    return;
                }

                JournalLocation location =
                    JournalPathResolver.Resolve(document);

                string modelName =
                    RevitLinkService.GetModelName(document);

                string username =
                    document.Application.Username;

                currentLocation = location;

                currentModelName = modelName;

                currentUsername = username;

                Log(
                    "JOURNAL POLLING CONTEXT | Model=" +
                    modelName +
                    " | JournalRoot=" +
                    (location != null
                        ? location.JournalRoot
                        : "<не определён>"));
            }
            catch (Exception ex)
            {
                Log(
                    "JOURNAL POLLING CONTEXT ERROR | " +
                    ex.GetType().FullName +
                    " | " +
                    ex.Message);
            }
        }


        private static void ResetContext()
        {
            currentLocation = null;

            currentModelName = null;

            currentUsername = null;
        }


        private static void OnTimerElapsed(
            object sender,
            System.Timers.ElapsedEventArgs e)
        {
            // Защита от наложения циклов: предыдущий цикл ещё не завершён
            // (например, медленная сеть) — пропускаем тик.
            if (Interlocked.Exchange(ref busy, 1) == 1)
            {
                RestartTimer();

                return;
            }

            try
            {
                RunPollCycle();
            }
            catch (Exception ex)
            {
                Log(
                    "JOURNAL POLL CYCLE ERROR | " +
                    ex.GetType().FullName +
                    " | " +
                    ex.Message);
            }
            finally
            {
                busy = 0;

                RestartTimer();
            }
        }


        private static void RestartTimer()
        {
            System.Timers.Timer currentTimer = timer;

            if (currentTimer != null)
                currentTimer.Start();
        }


        private static void RunPollCycle()
        {
            if (!JournalSettings.JsonJournalEnabled)
                return;

            // Снимок контекста: пока цикл выполняется, контекст
            // может быть обновлён другим документом.
            JournalLocation location = currentLocation;

            string modelName = currentModelName;

            string username = currentUsername;

            if (location == null ||
                string.IsNullOrEmpty(modelName))
            {
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            DateTime utcFrom =
                DateTime.UtcNow.AddDays(-JournalSettings.LookbackDays);

            List<JournalEvent> events =
                Reader.ReadEvents(location, utcFrom, modelName);

            List<JournalEvent> fresh =
                new List<JournalEvent>();

            foreach (JournalEvent journalEvent in events)
            {
                // Собственная запись не показывается как чужая (A.14).
                if (!string.IsNullOrEmpty(journalEvent.Author) &&
                    string.Equals(
                        journalEvent.Author,
                        username,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (JournalStateService.IsEventProcessed(
                        journalEvent.EventId))
                {
                    continue;
                }

                // Запись уже показана по каналу Extensible Storage —
                // не дублируем уведомление между каналами.
                if (journalEvent.RecordId.HasValue &&
                    JournalStateService.IsRecordProcessed(
                        journalEvent.RecordId.Value))
                {
                    continue;
                }

                fresh.Add(journalEvent);
            }

            if (fresh.Count == 0)
            {
                // Замер длительности опроса пишем в лог только
                // если он заметный (A.17: Polling duration).
                if (stopwatch.ElapsedMilliseconds > 200)
                {
                    Log(
                        "JOURNAL POLL SLOW | DurationMs=" +
                        stopwatch.ElapsedMilliseconds +
                        " | Events=" +
                        events.Count);
                }

                return;
            }

            // Дедупликация "при показе": помечаем обработанными
            // до показа окна — повторных уведомлений не будет,
            // даже если Revit перезапустится до подтверждения.
            foreach (JournalEvent journalEvent in fresh)
            {
                JournalStateService.MarkProcessed(
                    journalEvent.EventId,
                    journalEvent.RecordId);
            }

            List<ChangeRecord> records = fresh
                .Select(JournalEventMapper.ToChangeRecord)
                .ToList();

            foreach (ChangeRecord record in records)
            {
                record.Status = ChangeStatus.New;
            }

            Log(
                "JOURNAL POLL | Fresh=" +
                fresh.Count +
                " | Total=" +
                events.Count +
                " | DurationMs=" +
                stopwatch.ElapsedMilliseconds);

            DispatchNotification(records);
        }


        private static void DispatchNotification(List<ChangeRecord> records)
        {
            Application application = Application.Current;

            if (application == null || application.Dispatcher == null)
            {
                Log("JOURNAL NOTIFICATION SKIP | Reason=NoDispatcher");

                return;
            }

            application.Dispatcher.BeginInvoke(
                new Action(() => ShowNotifications(records)));
        }


        private static void ShowNotifications(List<ChangeRecord> records)
        {
            try
            {
                NotificationWindow window =
                    new NotificationWindow(records);

                WindowInteropHelper helper =
                    new WindowInteropHelper(window);

                helper.Owner =
                    Process.GetCurrentProcess()
                        .MainWindowHandle;

                bool? userConfirmed =
                    window.ShowDialog();

                Log(
                    "JOURNAL NOTIFICATION SHOWN | Count=" +
                    records.Count +
                    " | Result=" +
                    userConfirmed);
            }
            catch (Exception ex)
            {
                Log(
                    "JOURNAL NOTIFICATION ERROR | " +
                    ex.GetType().FullName +
                    " | " +
                    ex.Message);
            }
        }


        private static void Log(string message)
        {
            try
            {
                UpdateStartupService.LogDiagnostic(message);
            }
            catch
            {
                // Ошибка логирования не должна влиять на работу.
            }
        }
    }
}
