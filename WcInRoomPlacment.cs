using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;

namespace Task2
{
    [Transaction(TransactionMode.Manual)]
    public class WcInRoomPlacment : IExternalCommand
    {
        /**
         * The user is prompted to select a wall
         * Get all bathrooms
         * room -> ParamaterMap -> Get("Name") -> Value -> ToString() -> Contains("Bathroom")
         * Get the bathroom adjacent to the selected wall
         * room -> GetBoundarySegments() -> List<BoundarySegment> -> WallElementId -> compare with the selected wall Id
         * Check if the bathroom has a door (If false: return Failed)
         * wallSegment -> Wall ElementId -> GetDependentElements() -> Get the doors -> door.Room == room
         * place the family at the 2 corners of the room and calculate the distance from its center to the door
         * pick the bigger distance as the family location
         **/
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            #region Select a wall
            Wall selectedWall = null;
            try
            {
                var selectedWallRef = uidoc.Selection.PickObject(ObjectType.Element, new WallFilter(), "Select a wall");
                selectedWall = doc.GetElement(selectedWallRef.ElementId) as Wall;
            }
            catch
            { }
            #endregion

            #region Get all Bathrooms
            //Get all the bathrooms
            var allBathrooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToList()
                .Where(r => r.Name.Contains("Bathroom")).Select(r => r as Room);
            #endregion

            #region Place the family in the bathroom adjacent to the selected wall
            //Get the bathroom adjacent to the selected wall
            if (allBathrooms.Any())
            {
                BoundarySegment wallSegment = null;
                foreach (var room in allBathrooms)
                {
                    var roomSegments = room.GetBoundarySegments(new SpatialElementBoundaryOptions()).ToList();
                    foreach (var segList in roomSegments)
                    {
                        wallSegment = segList.ToList().Where(seg => seg.ElementId == selectedWall.Id).FirstOrDefault();
                    }
                    if (wallSegment != null)
                    {
                        //Get the adjacent wall curve if it exists
                        Curve wallCurve = wallSegment.GetCurve();

                        //Get the room door if it exists
                        var roomDoors = new List<FamilyInstance>();

                        foreach (var segList in roomSegments)
                        {
                            foreach(var seg in segList)
                            {
                                var doorList = doc.GetElement(seg.ElementId).GetDependentElements(new ElementCategoryFilter(BuiltInCategory.OST_Doors)).ToList()
                                                .Select(doorId => doc.GetElement(doorId) as FamilyInstance)
                                                .Where(d => d != null && d.Room.Id == room.Id).ToList();
                                doorList.ForEach(d => roomDoors.Add(d)); 
                            }
                        }
                        
                        if (roomDoors.Any())
                        {
                            XYZ wallCorner1 = wallCurve.GetEndPoint(0);
                            XYZ wallCorner2 = wallCurve.GetEndPoint(1);
                            XYZ doorCenter = (roomDoors[0].Location as LocationPoint).Point;
                            if (wallCorner1.DistanceTo(doorCenter) > wallCorner2.DistanceTo(doorCenter))
                            {
                                TaskDialog.Show("Result", "Place the family at the corner 1 " + wallCorner1.ToString() +wallCorner2.ToString());
                            }
                            else
                            {
                                TaskDialog.Show("Result", "Place the family at the corner 2 " + wallCorner2.ToString() + wallCorner1.ToString());
                            }
                            break;
                        }
                        else
                        {
                            TaskDialog.Show("Error", "The bathroom has no door");
                        }
                    }
                }
                if (wallSegment == null)
                {
                    TaskDialog.Show("Error", "The selected wall is not adjacent to any bathroom");
                }
            }
            else
            {
                TaskDialog.Show("Error", "No bathrooms found, Make sure the desired room name contains \"Bathroom\"");
            }
            #endregion

            return Result.Succeeded;
        }
    }
}
