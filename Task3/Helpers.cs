using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace RevitAPI_Tasks.Task3
{
    public class Helpers
    {
        //Gets the room doors
        public static List<FamilyInstance> GetDoors(Document doc, Room room, IList<IList<BoundarySegment>> roomSegments)
        {
            //Get the room door if it exists
            var roomDoors = new List<FamilyInstance>();

            foreach (var segList in roomSegments)
            {
                foreach (var seg in segList)
                {
                    var doorList = doc.GetElement(seg.ElementId).GetDependentElements(new ElementCategoryFilter(BuiltInCategory.OST_Doors)).ToList()
                                    .Select(doorId => doc.GetElement(doorId) as FamilyInstance)
                                    .Where(d => d != null && d.Room.Id == room.Id).ToList();
                    doorList.ForEach(d => roomDoors.Add(d));
                }
            }
            return roomDoors;
        }

        //Visualize the shape for debugging
        public static void VisualizeShape(Document doc, List<GeometryObject> shapes)
        {
            using (Transaction t = new Transaction(doc, "Visualize"))
            {
                t.Start();
                DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel)).SetShape(shapes);
                t.Commit();
            }
        }

        public static bool IsAlmostEqual(XYZ point1, XYZ point2, double tolerance)
        {
            return point1.DistanceTo(point2) <= tolerance;
        }

    }
}
