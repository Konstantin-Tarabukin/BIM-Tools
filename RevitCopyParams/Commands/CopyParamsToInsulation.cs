using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RevitCopyParams
{
    [Transaction(TransactionMode.Manual)]
    public class CopyParamsToInsulation : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            SettingsForm settings = new SettingsForm();

            if (settings.ShowDialog() != DialogResult.OK)
                return Result.Cancelled;

            bool useSelection = settings.UseSelection;

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            List<ParamItem> items = new List<ParamItem>();

            // Собираем всю изоляцию
            var insulations = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(e =>
                    e.Category != null &&
                    (e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctInsulations ||
                     e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeInsulations))
                .ToList();

            // === 1. Находим различающиеся параметры ===
            foreach (var insulation in insulations)
            {
                Element host = GetHost(doc, insulation);
                if (host == null) continue;

                foreach (Parameter p in host.Parameters)
                {
                    if (p.Definition == null) continue;

                    string name = p.Definition.Name;

                    if (!name.StartsWith("ADSK_")) continue;

                    if (name == "ADSK_Наименование" || name == "ADSK_Количество")
                        continue;

                    var insParam = insulation.LookupParameter(name);
                    if (insParam == null) continue;

                    string val1 = GetParamValue(p);
                    string val2 = GetParamValue(insParam);

                    if (val1 != val2)
                        items.Add(new ParamItem
                        {
                            IsChecked = true,
                            Name = name,
                            HostValue = val1,
                            InsValue = val2
                        });
                }
            }

            items = items
                .GroupBy(x => x.Name)
                .Select(g => g.First())
                .OrderBy(x => x.Name)
                .ToList();

            // 👉 если нет различий — сразу выходим
            if (items.Count == 0)
            {
                TaskDialog.Show("Результат", "Нет различающихся ADSK параметров");
                return Result.Succeeded;
            }

            // === 2. Выбор параметров ===
            List<string> selected;

            if (useSelection)
            {
                ParamForm form = new ParamForm(items);

                if (form.ShowDialog() != true)
                    return Result.Cancelled;

                selected = form.SelectedParams;

                if (selected == null || selected.Count == 0)
                {
                    TaskDialog.Show("Внимание", "Не выбрано ни одного параметра");
                    return Result.Cancelled;
                }
            }
            else
            {
                selected = items.Select(x => x.Name).ToList();
            }

            // === 3. КОПИРОВАНИЕ ===
            StringBuilder log = new StringBuilder();
            int success = 0;
            int skipped = 0;

            using (Transaction t = new Transaction(doc, "Копирование параметров"))
            {
                t.Start();

                foreach (var insulation in insulations)
                {
                    Element host = GetHost(doc, insulation);
                    if (host == null)
                    {
                        skipped++;
                        log.AppendLine("❌ Нет хоста");
                        continue;
                    }

                    foreach (string paramName in selected)
                    {
                        var hostParam = host.LookupParameter(paramName);
                        var insParam = insulation.LookupParameter(paramName);

                        if (hostParam == null || insParam == null)
                        {
                            skipped++;
                            log.AppendLine($"❌ {paramName} — нет параметра");
                            continue;
                        }

                        if (insParam.IsReadOnly)
                        {
                            skipped++;
                            log.AppendLine($"❌ {paramName} — только чтение");
                            continue;
                        }

                        string val1 = GetParamValue(hostParam);
                        string val2 = GetParamValue(insParam);

                        if (val1 == val2)
                            continue;

                        bool result = SetParamValue(insParam, hostParam);

                        if (result)
                        {
                            success++;
                            log.AppendLine($"✔ {paramName}");
                        }
                        else
                        {
                            skipped++;
                            log.AppendLine($"❌ {paramName} — ошибка записи");
                        }
                    }
                }

                t.Commit();
            }

            // === 4. ВЫВОД ===
            TaskDialog td = new TaskDialog("Результат");
            td.MainInstruction = "Копирование завершено";
            td.MainContent = $"Скопировано: {success}\nПропущено: {skipped}";

            if (log.Length > 0)
                td.ExpandedContent = log.ToString();
            else
                td.ExpandedContent = "Нет изменений";

            td.Show();

            return Result.Succeeded;
        }

        private Element GetHost(Document doc, Element insulation)
        {
            if (insulation is DuctInsulation ductIns)
                return doc.GetElement(ductIns.HostElementId);

            if (insulation is PipeInsulation pipeIns)
                return doc.GetElement(pipeIns.HostElementId);

            return null;
        }

        private string GetParamValue(Parameter p)
        {
            switch (p.StorageType)
            {
                case StorageType.String:
                    return p.AsString() ?? "";
                case StorageType.Double:
                    return p.AsDouble().ToString();
                case StorageType.Integer:
                    return p.AsInteger().ToString();
                case StorageType.ElementId:
                    return p.AsElementId().IntegerValue.ToString();
                default:
                    return "";
            }
        }

        private bool SetParamValue(Parameter target, Parameter source)
        {
            try
            {
                switch (source.StorageType)
                {
                    case StorageType.String:
                        target.Set(source.AsString());
                        return true;

                    case StorageType.Double:
                        target.Set(source.AsDouble());
                        return true;

                    case StorageType.Integer:
                        target.Set(source.AsInteger());
                        return true;

                    case StorageType.ElementId:
                        target.Set(source.AsElementId());
                        return true;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}