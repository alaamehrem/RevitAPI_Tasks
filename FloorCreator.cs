using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace RevitAPI_Tasks.Task1
{
    [Transaction(TransactionMode.Manual)]
    public class FloorCreator : IExternalCommand
    {
        /**
         * Create a list of lines to define the floor boundary
         * Check if there is more than 2 lines
         * Flatten the list of lines if needed
         * Rearrange the points to form a closed curve if needed
         * Get the Floor Type Id and Level Id
         * Create the floor
         * */
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Create a list of lines to define the floor boundary
            List<Curve> lines = new List<Curve>()
            {
                Line.CreateBound(new XYZ(0, 0, 0), new XYZ(79, 0, 0)),
                Line.CreateBound(new XYZ(44, 25, 0), new XYZ(13, 25, 0)),
                Line.CreateBound(new XYZ(13, 40, 0), new XYZ(-8, 40, 0)),
                Line.CreateBound(new XYZ(55, 34, 0), new XYZ(55, 10, 0)),
                Line.CreateBound(new XYZ(79,34, 0), new XYZ(55,34, 0)),
                Line.CreateBound(new XYZ(0,20, 0), new XYZ(0,0, 0)),
                Line.CreateBound(new XYZ(55,10, 0), new XYZ(44,12, 0)),
                Line.CreateBound(new XYZ(-8,40, 0), new XYZ(-8,20, 0)),
                Line.CreateBound(new XYZ(79,0, 0), new XYZ(79,34, 0)),
                Line.CreateBound(new XYZ(44,12, 0), new XYZ(44,25, 0)),
                Line.CreateBound(new XYZ(-8,20, 0), new XYZ(0,20, 0)),
                Line.CreateBound(new XYZ(13,25, 0), new XYZ(13,40, 0))
            };

            // Check if there is more than 2 lines
            if (lines.Count < 2)
            {
                TaskDialog.Show("Error", "You need at least 2 lines to create a floor");
                return Result.Failed;
            }

            // Flatten the list of lines if needed
            if (!Helpers.IsLinesFlattened(lines))
            {
                lines = Helpers.FlattenList(lines);
            }

            // Rearrange the points to form a closed curve if needed
            lines = Helpers.RearrangeLines(lines);

            // Get the Floor Type Id and Level Id
            var floorId = new FilteredElementCollector(doc).OfClass(typeof(FloorType)).First().Id;
            var levelId = new FilteredElementCollector(doc).OfClass(typeof(Level)).First().Id;

            // Create the floor 
            if (lines.Any())
            {
                CurveLoop curveLoop = CurveLoop.Create(lines);
                List<CurveLoop> profiles = new List<CurveLoop>() { curveLoop };
                using (Transaction transaction = new Transaction(doc, "Create Floor"))
                {
                    transaction.Start();
                    Floor.Create(doc, profiles, floorId , levelId);
                    transaction.Commit();
                    return Result.Succeeded;
                }

            }
            else
            {
                TaskDialog.Show("Error", "The lines do not form a closed loop");
                return Result.Failed;
            }
        }
    }
}
