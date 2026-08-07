using Autodesk.Revit.DB.Events;// используем для сокращения имени класса "Autodesk.Revit.DB.Events.DocumentOpenedEventArgs"
using Autodesk.Revit.UI;
using RevitCopyParams.Models;
using RevitCopyParams.Services;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Imaging;


namespace RevitCopyParams
{
    public class App : IExternalApplication
    {
        private void OnIdling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {
            if (pendingUpdateInfo == null)
                return;

            UpdateInfo updateInfo = pendingUpdateInfo;
            pendingUpdateInfo = null;

            TaskDialog.Show(
                "BIM Tools",
                $"CurrentVersion: {updateInfo.CurrentVersion}\n" +
                $"LatestVersion: {updateInfo.LatestVersion}\n" +
                $"UpdateAvailable: {updateInfo.UpdateAvailable}");
        }



        private NotificationService notificationService =
    new NotificationService();
        private UpdateInfo pendingUpdateInfo;



        private void OnDocumentOpened(
    object sender,
    Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
        {

            try
            {

                notificationService.CheckNotifications(e.Document, "Открытие файла");
            }
            catch (Exception ex)
            {

            }
        }

        private void OnDocumentSynchronized(
            object sender,
            Autodesk.Revit.DB.Events.DocumentSynchronizedWithCentralEventArgs e)
        {
            notificationService.CheckNotifications(e.Document, "Синхронизация");
        }

        private void OnReloadLatest(
            object sender,
            Autodesk.Revit.DB.Events.DocumentReloadedLatestEventArgs e)
        {
            notificationService.CheckNotifications(e.Document,  "Обновить до последней версии");
        }




        public Result OnStartup(UIControlledApplication application)
        {
            application.Idling += OnIdling;

            Task.Run(() => TestUpdateService());

            string tabName = "BIM Tools";




            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch
            {
                // Вкладка уже существует
            }

            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Параметры");
            // Подписываемся на события Revit
            application.ControlledApplication.DocumentOpened += OnDocumentOpened;
            application.ControlledApplication.DocumentSynchronizedWithCentral += OnDocumentSynchronized;
            application.ControlledApplication.DocumentReloadedLatest += OnReloadLatest;


            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyFolder = Path.GetDirectoryName(assemblyPath);
            //Кнопка1
            CreateButton(
                panel,
                assemblyPath,
                assemblyFolder,
                "CopyParams",
                "Копировать",
                "RevitCopyParams.CopyParamsToInsulation",
                "Копирует параметры ADSK с воздуховодов/труб на изоляцию",
                "CopyParams");
            //Кнопка 2
            CreateButton(
                panel,
                assemblyPath,
                assemblyFolder,
                "CreatePipes",
                "Трубы",
                "RevitCopyParams.CreatePipesCommand",
                "Создание теплого пола по заданной области",
                "CreatePipes");

            // Кнопка 3
            CreateButton(
                panel,
                assemblyPath,
                assemblyFolder,
                "ChangeJournal",
                "Журнал",
                "RevitCopyParams.ChangeJournalCommand",
                "Создание записи об изменениях",
               "Journal");

            // Кнопка 4
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
        //метод вызова кнопок
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
            PushButtonData buttonData = new PushButtonData(
                internalName,
                buttonText,
                assemblyPath,
                commandClass);

            PushButton button = panel.AddItem(buttonData) as PushButton;

            if (button == null)
                return;

            button.ToolTip = toolTip;

            string resourcesFolder = Path.Combine(
    assemblyFolder,
    "Resources");

            string icon32 = Path.Combine(
                resourcesFolder,
                iconName + "32.png");

            string icon16 = Path.Combine(
                resourcesFolder,
                iconName + "16.png");

            SetButtonIcons(button, icon16, icon32);
        }
        //метод иконок кнопок
        private void SetButtonIcons(PushButton button, string smallIcon, string largeIcon)
        {
            if (button == null)
                return;

            if (File.Exists(largeIcon))
                button.LargeImage = new BitmapImage(new Uri(largeIcon));

            if (File.Exists(smallIcon))
                button.Image = new BitmapImage(new Uri(smallIcon));
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            application.Idling -= OnIdling;
            application.ControlledApplication.DocumentOpened -= OnDocumentOpened;
            application.ControlledApplication.DocumentSynchronizedWithCentral -= OnDocumentSynchronized;
            application.ControlledApplication.DocumentReloadedLatest -= OnReloadLatest;

            return Result.Succeeded;
        }

        private async System.Threading.Tasks.Task TestUpdateService()
        {
            try
            {
                var updateInfo = await UpdateService.GetLatestRelease();

                pendingUpdateInfo = updateInfo;
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(
                    @"C:\Temp\BIMToolsUpdateError.txt",
                    ex.ToString());
            }
        }


    }

}
