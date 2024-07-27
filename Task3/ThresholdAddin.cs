using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Linq;
using Autodesk.Revit.DB.Architecture;
using RevitAPI_Tasks.Task3;

namespace RevitAPI_Tasks.Task3
{
    [Transaction(TransactionMode.Manual)]

    /**
     * Creats a floor with threshold for each room in the document
     */
    public class ThresholdAddin : IExternalCommand
    {
        /**
         * Get all Rooms in the document
         * for each room, get the room's boundary segments and the room's doors
         * creates the threshold boundary
         * get the room's boundaries points
         * draw a polygon of the room's boundary points and the threshold boundary points
         * create a new floor with the new polygon
         * */
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Get all Rooms in the document
            var allRooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToList()
                .Select(r => r as Room).ToList();

            using (TransactionGroup tg = new TransactionGroup(doc, "Create Floors"))
            {
                tg.Start();
                FloorBoundaryCreation.CreateFloors(doc, allRooms);
                tg.Assimilate();
            }
            return Result.Succeeded;
        }
    }
}
