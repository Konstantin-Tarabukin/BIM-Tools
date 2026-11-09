using Newtonsoft.Json;
using RevitCopyParams.Services;
using System;
using System.IO;

namespace RevitCopyParams.Config
{
    // Настройки внешнего JSON-журнала (ADR-014, этап A.4).
    //
    // Читаются один раз при запуске Revit из файла:
    //   %AppData%\BIMTools\RevitCopyParams\JournalSettings.json
    //
    // Отсутствие файла означает значения по умолчанию.
    // После изменения файла требуется перезапуск Revit.
    //
    // Пример файла:
    // {
    //   "jsonJournalEnabled": true,
    //   "pollingIntervalSeconds": 5,
    //   "lookbackDays": 3,
    //   "journalRootOverride": "",
    //   "reloadLinksAfterSync": false
    // }
    public static class JournalSettings
    {
        public const int DefaultPollingIntervalSeconds = 5;

        public const int DefaultLookbackDays = 3;

        // Внешний JSON-журнал включён (канал best-effort).
        public static bool JsonJournalEnabled { get; private set; }

        // Интервал опроса новых событий, секунды (A.10: 2–5 секунд).
        public static int PollingIntervalSeconds { get; private set; }

        // Глубина чтения журнала, дни (A.9: читать только актуальные каталоги).
        public static int LookbackDays { get; private set; }

        // Явное переопределение корня журнала, например:
        //   \\server\share\05091-ЭФ25\_BIMToolsJournal
        // Пустая строка — автодетект по central-пути модели.
        public static string JournalRootOverride { get; private set; }

        // Автоперезагрузка связей после синхронизации (A.19).
        // По умолчанию отключена: уведомления доставляются внешним журналом
        // без Reload; сервис RevitLinkReloadService остаётся fallback-механизмом.
        public static bool ReloadLinksAfterSync { get; private set; }

        static JournalSettings()
        {
            JsonJournalEnabled = true;

            PollingIntervalSeconds = DefaultPollingIntervalSeconds;

            LookbackDays = DefaultLookbackDays;

            JournalRootOverride = string.Empty;

            ReloadLinksAfterSync = false;

            Load();
        }

        private static void Load()
        {
            try
            {
                string path =
                    BIMToolsPaths.JournalSettingsJsonPath;

                if (!File.Exists(path))
                    return;

                JournalSettingsData data =
                    JsonConvert.DeserializeObject<JournalSettingsData>(
                        File.ReadAllText(path));

                if (data == null)
                    return;

                if (data.JsonJournalEnabled.HasValue)
                {
                    JsonJournalEnabled =
                        data.JsonJournalEnabled.Value;
                }

                if (data.PollingIntervalSeconds.HasValue)
                {
                    // Нижняя граница — 2 секунды (A.10).
                    PollingIntervalSeconds =
                        Math.Max(2, data.PollingIntervalSeconds.Value);
                }

                if (data.LookbackDays.HasValue)
                {
                    LookbackDays =
                        Math.Min(30, Math.Max(1, data.LookbackDays.Value));
                }

                if (!string.IsNullOrWhiteSpace(data.JournalRootOverride))
                {
                    JournalRootOverride =
                        data.JournalRootOverride;
                }

                if (data.ReloadLinksAfterSync.HasValue)
                {
                    ReloadLinksAfterSync =
                        data.ReloadLinksAfterSync.Value;
                }
            }
            catch (Exception ex)
            {
                // Повреждённый файл настроек не должен ломать запуск
                // плагина — работаем на значениях по умолчанию.
                UpdateStartupService.LogDiagnostic(
                    "JOURNAL SETTINGS ERROR | " +
                    ex.GetType().FullName +
                    " | " +
                    ex.Message);
            }
        }
    }

    // DTO файла настроек. Nullable-поля позволяют указывать
    // в файле только переопределяемые значения.
    public class JournalSettingsData
    {
        public bool? JsonJournalEnabled { get; set; }

        public int? PollingIntervalSeconds { get; set; }

        public int? LookbackDays { get; set; }

        public string JournalRootOverride { get; set; }

        public bool? ReloadLinksAfterSync { get; set; }
    }
}
