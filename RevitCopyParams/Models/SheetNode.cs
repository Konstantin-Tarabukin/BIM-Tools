using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitCopyParams.Models
{
    public class SheetNode
    {
        /// <summary>
        /// Название группы (АР, ОВ, ВК...)
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// Номер листа Revit
        /// </summary>
        public string SheetNumber { get; set; }


        /// <summary>
        /// Имя листа Revit
        /// </summary>
        public string SheetName { get; set; }


        /// <summary>
        /// Связанный лист Revit
        /// </summary>
        public ViewSheet Sheet { get; set; }


        /// <summary>
        /// Дочерние элементы дерева
        /// </summary>
        public List<SheetNode> Children { get; set; }
            = new List<SheetNode>();


        /// <summary>
        /// Группа или лист
        /// </summary>
        public bool IsGroup => Sheet == null;


        /// <summary>
        /// Отображение группы с количеством листов
        /// </summary>
        public string DisplayCount
        {
            get
            {
                if (IsGroup)
                {
                    return $"{Name} ({Children.Count})";
                }

                return "";
            }
        }
    }
}