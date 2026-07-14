using Autodesk.Revit.DB;
using RevitCopyParams.Models;
using System.Collections.Generic;
using System.Linq;


namespace RevitCopyParams.Services
{
    public class SheetNumberService
    {
        private readonly Document _doc;


        public SheetNumberService(Document doc)
        {
            _doc = doc;
        }


        public List<SheetNode> GetSheetsTree()
        {
            List<ViewSheet> sheets =
                new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(x => !x.IsPlaceholder)
                .ToList();


            Dictionary<string, SheetNode> groups =
                new Dictionary<string, SheetNode>();


            foreach (ViewSheet sheet in sheets)
            {

                string section = "";


                Parameter param =
                    sheet.LookupParameter("ADSK_Штамп Раздел проекта");


                if (param != null)
                {
                    section = param.AsString();
                }


                if (string.IsNullOrEmpty(section))
                {
                    section = "(Без раздела)";
                }


                if (!groups.ContainsKey(section))
                {
                    groups.Add(
                        section,
                        new SheetNode
                        {
                            Name = section
                        });
                }


                groups[section].Children.Add(
                    new SheetNode
                    {
                        Name = $"{sheet.SheetNumber} - {sheet.Name}",
                        SheetNumber = sheet.SheetNumber,
                        SheetName = sheet.Name,
                        Sheet = sheet
                    });
            }


            return groups
                .OrderBy(x => x.Key)
                .Select(x => x.Value)
                .ToList();
        }
    }
}