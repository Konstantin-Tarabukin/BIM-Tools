using System;
using System.Reflection;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;
using System.IO;

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
            catch { }

            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Параметры");

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            // КНОПКА 1
            PushButtonData buttonData = new PushButtonData(
                "CopyParams",
                "Копировать",
                assemblyPath,
                "RevitCopyParams.CopyParamsToInsulation"
            );

            PushButton button = panel.AddItem(buttonData) as PushButton;

            // КНОПКА 2
            PushButtonData button2 = new PushButtonData(
                "CreatePipes",
                "Трубы",
                assemblyPath,
                "RevitCopyParams.CreatePipesCommand"
            );

            PushButton pipeButton = panel.AddItem(button2) as PushButton;

            pipeButton.ToolTip = "Создание теплого пола по заданной области";

            // ИКОНКИ
            string folder = Path.GetDirectoryName(assemblyPath);

            string icon32 = Path.Combine(folder, "icon32.png");
            string icon16 = Path.Combine(folder, "icon16.png");

            if (File.Exists(icon32))
            {
                button.LargeImage = new BitmapImage(new Uri(icon32));
            }

            if (File.Exists(icon16))
            {
                button.Image = new BitmapImage(new Uri(icon16));
            }

            button.ToolTip = "Копирует параметры с воздуховодов/труб на изоляцию";

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}