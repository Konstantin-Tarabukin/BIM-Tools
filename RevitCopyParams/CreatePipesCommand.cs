using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitCopyParams
{
    [Transaction(TransactionMode.Manual)]
    public class CreatePipesCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                var region = doc.GetElement(
                    uidoc.Selection.PickObject(ObjectType.Element, "Выбери область")
                ) as FilledRegion;

                Pipe startPipe = doc.GetElement(
                    uidoc.Selection.PickObject(ObjectType.Element, "Стартовая труба")
                ) as Pipe;

                Pipe endPipe = doc.GetElement(
                    uidoc.Selection.PickObject(ObjectType.Element, "Финишная труба")
                ) as Pipe;

                double stepMm = 250;
                double step = UnitUtils.ConvertToInternalUnits(stepMm, UnitTypeId.Millimeters);

                bool isSnake = TaskDialog.Show(
                    "Тип укладки",
                    "Да = змейка\nНет = улитка",
                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
                ) == TaskDialogResult.Yes;

                BoundingBoxXYZ bbox = region.get_BoundingBox(null);

                double minX = bbox.Min.X;
                double maxX = bbox.Max.X;
                double minY = bbox.Min.Y;
                double maxY = bbox.Max.Y;

                List<XYZ> pts = isSnake
                    ? GenerateSnake(minX, maxX, minY, maxY, step)
                    : GenerateDoubleSpiral(minX, maxX, minY, maxY, step);

                using (Transaction t = new Transaction(doc, "ТП"))
                {
                    t.Start();

                    var level = startPipe.ReferenceLevel;
                    var systemTypeId = startPipe.MEPSystem.GetTypeId();
                    var pipeTypeId = startPipe.PipeType.Id;
                    double diameter = startPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();

                    Pipe prevPipe = null;

                    for (int i = 0; i < pts.Count - 1; i++)
                    {
                        XYZ p1 = new XYZ(
    Math.Round(pts[i].X, 6),
    Math.Round(pts[i].Y, 6),
    Math.Round(pts[i].Z, 6));

                        XYZ p2 = new XYZ(
                            Math.Round(pts[i + 1].X, 6),
                            Math.Round(pts[i + 1].Y, 6),
                            Math.Round(pts[i + 1].Z, 6));

                        // ❗ КЛЮЧ: защита от коротких сегментов
                        double dist = p1.DistanceTo(p2);
                        if (dist < UnitUtils.ConvertToInternalUnits(2.54, UnitTypeId.Millimeters))
                            continue;

                        Pipe pipe = Pipe.Create(
                            doc,
                            systemTypeId,
                            pipeTypeId,
                            level.Id,
                            p1,
                            p2);
                        // 🔥 гарантированно задаём систему
                        if (startPipe.MEPSystem != null)
                        {
                            try
                            {
                                pipe.SetSystemType(startPipe.MEPSystem.GetTypeId());
                            }
                            catch { }
                        }

                        pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);

                        if (prevPipe != null)
                            TryConnect(doc, prevPipe, pipe);

                        prevPipe = pipe;
                    }
                    // 🔥 ДОЖИМАЕМ: добавляем все трубы в систему
                    var system = startPipe.MEPSystem;

                    if (system != null)
                    {
                        var newPipes = new FilteredElementCollector(doc)
                            .OfClass(typeof(Pipe))
                            .Cast<Pipe>()
                            .Where(p => p.ReferenceLevel.Id == level.Id) // только наши
                            .ToList();

                        foreach (var p in newPipes)
                        {
                            try
                            {

                            }
                            catch { }
                        }
                    }

                    t.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", ex.Message);
                return Result.Cancelled;
            }
        }

        private List<XYZ> GenerateSnake(double minX, double maxX, double minY, double maxY, double step)
        {
            List<XYZ> pts = new List<XYZ>();
            bool dir = true;

            for (double y = minY; y <= maxY; y += step)
            {
                if (dir)
                {
                    pts.Add(new XYZ(minX, y, 0));
                    pts.Add(new XYZ(maxX, y, 0));
                }
                else
                {
                    pts.Add(new XYZ(maxX, y, 0));
                    pts.Add(new XYZ(minX, y, 0));
                }

                dir = !dir;
            }

            return pts;
        }

        private List<XYZ> GenerateDoubleSpiral(double minX, double maxX, double minY, double maxY, double step)
        {
            var outer = GenerateSingleSpiral(minX, maxX, minY, maxY, step);

            double offset = step / 2;

            var inner = GenerateSingleSpiral(
                minX + offset,
                maxX - offset,
                minY + offset,
                maxY - offset,
                step
            );

            inner.Reverse();

            List<XYZ> path = new List<XYZ>();

            path.AddRange(outer);

            if (outer.Count > 0 && inner.Count > 0)
            {
                XYZ a = outer.Last();
                XYZ b = inner.First();

                path.Add(new XYZ(b.X, a.Y, 0));
                path.Add(b);
            }

            path.AddRange(inner);

            return path;
        }

        private List<XYZ> GenerateSingleSpiral(double minX, double maxX, double minY, double maxY, double step)
        {
            List<XYZ> pts = new List<XYZ>();

            double left = minX;
            double right = maxX;
            double bottom = minY;
            double top = maxY;

            pts.Add(new XYZ(left, bottom, 0));

            while ((right - left) > step && (top - bottom) > step)
            {
                pts.Add(new XYZ(right, bottom, 0));
                bottom += step;

                pts.Add(new XYZ(right, top, 0));
                right -= step;

                pts.Add(new XYZ(left, top, 0));
                top -= step;

                pts.Add(new XYZ(left, bottom, 0));
                left += step;
            }

            double x1 = left;
            double x2 = right;
            double y1 = bottom;
            double y2 = top;

            // если осталось место — делаем аккуратный финальный проход
            if ((x2 - x1) > step && (y2 - y1) > step)
            {
                pts.Add(new XYZ(x2, y1, 0));
                pts.Add(new XYZ(x2, y2, 0));
                // удаляем предпоследнюю точку, чтобы не было короткого сегмента
                if (pts.Count > 1)
                    pts.RemoveAt(pts.Count - 1);
                pts.Add(new XYZ(x1, y2, 0));
            }
            else
            {
                // 🔥 ВОТ КЛЮЧ: просто один финальный сегмент в центре
                pts.Add(new XYZ(x1, y1, 0));
                pts.Add(new XYZ(x2, y1, 0));
            }

            return pts;
        }

        private void TryConnect(Document doc, Pipe p1, Pipe p2)
        {
            var c1s = p1.ConnectorManager.Connectors.Cast<Connector>();
            var c2s = p2.ConnectorManager.Connectors.Cast<Connector>();

            foreach (var c1 in c1s)
            {
                foreach (var c2 in c2s)
                {
                    if (c1.Origin.DistanceTo(c2.Origin) < UnitUtils.ConvertToInternalUnits(2, UnitTypeId.Millimeters))
                    {
                        try
                        {
                            doc.Create.NewElbowFitting(c1, c2);
                            return; // 🔥 сразу выходим — соединение сделано
                        }
                        catch { }
                    }
                }
            }
        }


        private Connector GetClosest(Pipe from, Pipe to)
        {
            var cons = from.ConnectorManager.Connectors.Cast<Connector>();

            var curve = (to.Location as LocationCurve).Curve;
            XYZ p1 = curve.GetEndPoint(0);
            XYZ p2 = curve.GetEndPoint(1);

            // 🔥 берём ближайшую точку, а не только начало
            XYZ target = cons
                .SelectMany(c => new[] { p1, p2 }, (c, p) => new { c, p })
                .OrderBy(x => x.c.Origin.DistanceTo(x.p))
                .First()
                .p;

            return cons.OrderBy(c => c.Origin.DistanceTo(target)).FirstOrDefault();
        }
    }
}