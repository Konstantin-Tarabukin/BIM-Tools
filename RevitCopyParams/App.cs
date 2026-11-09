using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using RevitCopyParams.Services;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace RevitCopyParams
{
    public class App : IExternalApplication
    {
        private NotificationService notificationService =
            new NotificationService();

        private void OnDocumentOpened(
            object sender,
            Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
        {
            try
            {
                string modelName =
                    RevitLinkService.GetModelName(e.Document);

                UpdateStartupService.LogDiagnostic(
                    "DOCUMENT OPENED START | Model=" +
                    modelName);

                JournalPollingService.UpdateContext(
                    e.Document);

                notificationService.CheckNotifications(
                    e.Document,
                    "Открытие файла");

                UpdateStartupService.LogDiagnostic(
                    "DOCUMENT OPENED END | Model=" +
                    modelName);
            }
            catch (Exception ex)
            {
                UpdateStartupService.LogDiagnostic(
                    "DOCUMENT OPENED EXCEPTION | Type=" +
                    ex.GetType().FullName +
                    " | Message=" +
                    ex.Message +
                    " | StackTrace=" +
                    ex.StackTrace);
            }
        }

        private void OnDocumentSynchronized(
            object sender,
            Autodesk.Revit.DB.Events.DocumentSynchronizedWithCentralEventArgs e)
        {
            string modelName = "UNKNOWN";

            try
            {
                modelName =
                    RevitLinkService.GetModelName(
                        e.Document);

                UpdateStartupService.LogDiagnostic(
                    "SYNC START | Model=" +
                    modelName +
                    " | Status=" +
                    e.Status);

                if (e.Status ==
                    Autodesk.Revit.DB.Events.RevitAPIEventStatus.Succeeded)
                {
                    // ADR-014 / A.19: уведомления доставляются внешним
                    // JSON-журналом и не зависят от Reload связей.
                    // RevitLinkReloadService сохранён как fallback-механизм
                    // и включается настройкой reloadLinksAfterSync = true.
                    if (JournalSettings.ReloadLinksAfterSync)
                    {
                        UpdateStartupService.LogDiagnostic(
                            "LINK RELOAD START | Model=" +
                            modelName);

                        RevitLinkReloadService
                            .ReloadLoadedTopLevelLinks(
                                e.Document);

                        UpdateStartupService.LogDiagnostic(
                            "LINK RELOAD END | Model=" +
                            modelName);
                    }
                    else
                    {
                        UpdateStartupService.LogDiagnostic(
                            "LINK RELOAD SKIPPED | Model=" +
                            modelName +
                            " | Reason=DisabledBySettings");
                    }
                }
                else
                {
                    UpdateStartupService.LogDiagnostic(
                        "LINK RELOAD SKIPPED | Model=" +
                        modelName +
                        " | Reason=SyncStatusNotSucceeded");
                }

                JournalPollingService.UpdateContext(
                    e.Document);

                notificationService.CheckNotifications(
                    e.Document,
                    "Синхронизация");

                UpdateStartupService.LogDiagnostic(
                    "SYNC END | Model=" +
                    modelName);
            }
            catch (Exception ex)
            {
                UpdateStartupService.LogDiagnostic(
                    "SYNC EXCEPTION | Model=" +
                    modelName +
                    " | Type=" +
                    ex.GetType().FullName +
                    " | Message=" +
                    ex.Message +
                    " | StackTrace=" +
                    ex.StackTrace);
            }
        }

        private void OnReloadLatest(
            object sender,
            Autodesk.Revit.DB.Events.DocumentReloadedLatestEventArgs e)
        {
            string modelName = "UNKNOWN";

            try
            {
                modelName =
                    RevitLinkService.GetModelName(
                        e.Document);

                UpdateStartupService.LogDiagnostic(
                    "RELOAD LATEST START | Model=" +
                    modelName);

                JournalPollingService.UpdateContext(
                    e.Document);

                notificationService.CheckNotifications(
                    e.Document,
                    "Обновить до последней версии");

                UpdateStartupService.LogDiagnostic(
                    "RELOAD LATEST END | Model=" +
                    modelName);
            }
            catch (Exception ex)
            {
                UpdateStartupService.LogDiagnostic(
                    "RELOAD LATEST EXCEPTION | Model=" +
                    modelName +
                    " | Type=" +
                    ex.GetType().FullName +
                    " | Message=" +
                    ex.Message +
                    " | StackTrace=" +
                    ex.StackTrace);
            }
        }

        // Смена активного документа: polling должен читать журнал
        // именно того проекта, в котором пользователь работает.
        private void OnDocumentActivated(
            object sender,
            Autodesk.Revit.DB.Events.DocumentActivatedEventArgs e)
        {
            try
            {
                JournalPollingService.UpdateContext(
                    e.Document);
            }
            catch (Exception ex)
            {
                UpdateStartupService.LogDiagnostic(
                    "DOCUMENT ACTIVATED EXCEPTION | Type=" +
                    ex.GetType().FullName +
                    " | Message=" +
                    ex.Message);
            }
        }

        public Result OnStartup(
            UIControlledApplication application)
        {
            UpdateStartupService.Start();

            string tabName = "BIM Tools";

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch
            {
                // Вкладка уже существует
            }

            RibbonPanel panel =
                application.CreateRibbonPanel(
                    tabName,
                    "Параметры");

            application.ControlledApplication.DocumentOpened +=
                OnDocumentOpened;

            application.ControlledApplication.DocumentSynchronizedWithCentral +=
                OnDocumentSynchronized;

            application.ControlledApplication.DocumentReloadedLatest +=
                OnReloadLatest;

            application.ControlledApplication.DocumentActivated +=
                OnDocumentActivated;

            // Внешний JSON-журнал (ADR-014): фоновый опрос новых событий.
            JournalPollingService.Start();

            string assemblyPath =
                Assembly.GetExecutingAssembly().Location;

            string assemblyFolder =
                Path.GetDirectoryName(assemblyPath);

            CreateButton(
                panel,
                assemblyPath,
                assemblyFolder,
                "CopyParams",
                "Копировать",
                "RevitCopyParams.CopyParamsToInsulation",
                "Копирует параметры ADSK с воздуховодов/труб на изоляцию",
                "CopyParams");

            CreateButton(
                panel,
                assemblyPath,
                assemblyFolder,
                "CreatePipes",
                "Трубы",
                "RevitCopyParams.CreatePipesCommand",
                "Создание теплого пола по заданной области",
                "CreatePipes");

            CreateButton(
                panel,
                assemblyPath,
                assemblyFolder,
                "ChangeJournal",
                "Журнал",
                "RevitCopyParams.ChangeJournalCommand",
                "Создание записи об изменениях",
                "Journal");

            CreateButton(
                panel,
                assemblyPath,
                assemblyFolder,
                "SheetNumber",
                "Нумератор\nлистов",
                "RevitCopyParams.SheetNumberCommand",
                "Перенумерация листов в проекте",
                "SheetNumber");

            return Result.Succeeded;
        }

        private void CreateButton(
            RibbonPanel panel,
            string assemblyPath,
            string assemblyFolder,
            string internalName,
            string buttonText,
            string commandClass,
            string toolTip,
            string iconName)
        {
            PushButtonData buttonData =
                new PushButtonData(
                    internalName,
                    buttonText,
                    assemblyPath,
                    commandClass);

            PushButton button =
                panel.AddItem(buttonData) as PushButton;

            if (button == null)
                return;

            button.ToolTip = toolTip;

            string resourcesFolder =
                Path.Combine(
                    assemblyFolder,
                    "Resources");

            string icon32 =
                Path.Combine(
                    resourcesFolder,
                    iconName + "32.png");

            string icon16 =
                Path.Combine(
                    resourcesFolder,
                    iconName + "16.png");

            SetButtonIcons(
                button,
                icon16,
                icon32);
        }

        private void SetButtonIcons(
            PushButton button,
            string smallIcon,
            string largeIcon)
        {
            if (button == null)
                return;

            if (File.Exists(largeIcon))
            {
                button.LargeImage =
                    new BitmapImage(
                        new Uri(largeIcon));
            }

            if (File.Exists(smallIcon))
            {
                button.Image =
                    new BitmapImage(
                        new Uri(smallIcon));
            }
        }

        public Result OnShutdown(
            UIControlledApplication application)
        {
            JournalPollingService.Stop();

            application.ControlledApplication.DocumentOpened -=
                OnDocumentOpened;

            application.ControlledApplication.DocumentSynchronizedWithCentral -=
                OnDocumentSynchronized;

            application.ControlledApplication.DocumentReloadedLatest -=
                OnReloadLatest;

            application.ControlledApplication.DocumentActivated -=
                OnDocumentActivated;

            return Result.Succeeded;
        }
    }
}