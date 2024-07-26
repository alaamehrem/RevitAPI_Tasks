using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Architecture;
using Task2;

namespace RevitAPI_Tasks.Task3
{
    [Transaction(TransactionMode.Manual)]
    public class ThresholdAddin : IExternalCommand
    {
        /**
         * Get all Rooms in the document
         * for each room, get the room's boundary segments and the room's doors
         * create a seperate function that creates the threshold boundary
         * get the room's threshold boundaries
         * union between the threshold boundaries and the room's boundary segments
         * create a new floor with the unioned boundaries
         * */
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Get all Rooms in the document
            var allRooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToList()
                .Select(r => r as Room).ToList();

            foreach (var room in allRooms)
            {
                // Get the room's boundary segments
                var roomSegments = room.GetBoundarySegments(new SpatialElementBoundaryOptions()).ToList(); // List<List<BoundarySegment>>
                
                //Get the room doors
                var roomDoors = Helpers.GetDoors(doc, room, roomSegments);
                foreach (var door in roomDoors)
                {
                    Double doorWidth = doc.GetElement(door.GetTypeId()).ParametersMap.get_Item("Width").AsDouble();
                    
                    Wall wallHost = doc.GetElement(door.Host.GetTypeId()) as Wall;
                    Double ThersholdThickness = wallHost.ParametersMap.get_Item("Width").AsDouble() / 2;
                    
                    XYZ doorLocation = (door.Location as LocationPoint).Point;
                    XYZ orientation = wallHost.Orientation;

                    // Create the threshold boundary
                    if (orientation.X == 0) // Horizontal Door
                    {
                        List<Curve> thresholdBoundary = new List<Curve>()
                        {
                            Line.CreateBound(new XYZ(doorLocation.X - doorWidth / 2, doorLocation.Y, 0), new XYZ(doorLocation.X + doorWidth / 2, doorLocation.Y, 0)),
                            Line.CreateBound(new XYZ(doorLocation.X + doorWidth / 2, doorLocation.Y, 0), new XYZ(doorLocation.X + doorWidth / 2, doorLocation.Y + ThersholdThickness, 0)),
                            Line.CreateBound(new XYZ(doorLocation.X + doorWidth / 2, doorLocation.Y + ThersholdThickness, 0), new XYZ(doorLocation.X - doorWidth / 2, doorLocation.Y + ThersholdThickness, 0)),
                            Line.CreateBound(new XYZ(doorLocation.X - doorWidth / 2, doorLocation.Y + ThersholdThickness, 0), new XYZ(doorLocation.X - doorWidth / 2, doorLocation.Y, 0))
                        };
                    }
                    else // Vertical Door
                    {
                        List<Curve> curves = new List<Curve>()
                        {
                            Line.CreateBound(new XYZ(doorLocation.X, doorLocation.Y - doorWidth / 2, 0), new XYZ(doorLocation.X, doorLocation.Y + doorWidth / 2, 0)),
                            Line.CreateBound(new XYZ(doorLocation.X, doorLocation.Y + doorWidth / 2, 0), new XYZ(doorLocation.X + ThersholdThickness, doorLocation.Y + doorWidth / 2, 0)),
                            Line.CreateBound(new XYZ(doorLocation.X + ThersholdThickness, doorLocation.Y + doorWidth / 2, 0), new XYZ(doorLocation.X + ThersholdThickness, doorLocation.Y - doorWidth / 2, 0)),
                            Line.CreateBound(new XYZ(doorLocation.X + ThersholdThickness, doorLocation.Y - doorWidth / 2, 0), new XYZ(doorLocation.X, doorLocation.Y - doorWidth / 2, 0))
                        };
                    }

                }

            }
            return Result.Succeeded;
        }
    }
}
