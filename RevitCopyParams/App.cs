using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace RevitCopyParams
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
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
                "Копирует параметры с воздуховодов/труб на изоляцию");
            //Кнопка 2
            CreateButton(
                panel,
                assemblyPath,
                assemblyFolder,
                "CreatePipes",
                "Трубы",
                "RevitCopyParams.CreatePipesCommand",
                "Создание теплого пола по заданной области");

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
            string toolTip)
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

            string icon32 = Path.Combine(assemblyFolder, "icon32.png");
            string icon16 = Path.Combine(assemblyFolder, "icon16.png");

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
            return Result.Succeeded;
        }
    }

}
