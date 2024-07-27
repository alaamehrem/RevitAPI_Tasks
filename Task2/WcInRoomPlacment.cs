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
using RevitAPI_Tasks.Task2;

namespace RevitAPI_Tasks.Task2
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
                try
                {
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
                            Wall wall = doc.GetElement(wallSegment.ElementId) as Wall;

                            //Get the room door if it exists
                            var roomDoors = Helpers.GetDoors(doc, room, roomSegments);

                            if (roomDoors.Any())
                            {
                                XYZ doorCenter = (roomDoors[0].Location as LocationPoint).Point;

                                View view = uidoc.ActiveView;

                                FamilySymbol ToiletSymbol = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_GenericModel).WhereElementIsElementType().Where(f => f is FamilySymbol && f.Name == "ADA").FirstOrDefault() as FamilySymbol;

                                XYZ wallCorner1 = wallCurve.GetEndPoint(0);
                                XYZ wallCorner2 = wallCurve.GetEndPoint(1);

                                if (ToiletSymbol == null)
                                {
                                    TaskDialog.Show("Error", "The ADA family is not loaded in the project");
                                    return Result.Failed;
                                }
                                using (Transaction t = new Transaction(doc, "Place Toilet"))
                                {
                                    t.Start();
                                    if (!ToiletSymbol.IsActive)
                                    {
                                        ToiletSymbol.Activate();
                                        doc.Regenerate();
                                    }
                                    XYZ placementPoint = wallCorner1.DistanceTo(doorCenter) > wallCorner2.DistanceTo(doorCenter) ? wallCorner1 : wallCorner2;

                                    try
                                    {
                                        var familyInstance = doc.Create.NewFamilyInstance(placementPoint, ToiletSymbol, wall, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                                        if (Math.Abs(wall.Orientation.Y) == 1) //Horizontal wall
                                        {
                                            if ((doorCenter.X - placementPoint.X) < 0)
                                            {
                                                familyInstance.flipHand();
                                                // Calculate the translation vector
                                                XYZ translation = new XYZ(-1.5, 0, 0);
                                                // Move the family instance
                                                ElementTransformUtils.MoveElement(doc, familyInstance.Id, translation);
                                            }
                                            else
                                            {
                                                // Calculate the translation vector
                                                XYZ translation = new XYZ(1.5, 0, 0);
                                                // Move the family instance
                                                ElementTransformUtils.MoveElement(doc, familyInstance.Id, translation);
                                            }
                                            
                                            if ((doorCenter.Y - placementPoint.Y) < 0)
                                            {
                                                familyInstance.flipHand();
                                                // Calculate the translation vector
                                                XYZ translation = new XYZ(-1.5, 0, 0);

                                            }

                                        }
                                        else
                                        {
                                            //Determine the facing direction of the family for vertical Host
                                            if ((doorCenter.X - placementPoint.X) < 0)
                                            {
                                                familyInstance.flipFacing();
                                            }

                                            if ((doorCenter.Y - placementPoint.Y) > 0)
                                            {
                                                familyInstance.flipHand();
                                                // Calculate the translation vector
                                                XYZ translation = new XYZ(0, 1.5, 0);
                                                // Move the family instance
                                                ElementTransformUtils.MoveElement(doc, familyInstance.Id, translation);
                                            }
                                            else
                                            {
                                                // Calculate the translation vector
                                                XYZ translation = new XYZ(0, -1.5, 0);
                                                // Move the family instance
                                                ElementTransformUtils.MoveElement(doc, familyInstance.Id, translation);
                                            }
                                        }
                                        t.Commit();

                                    }
                                    catch
                                    {
                                        TaskDialog.Show("Error", "The family could not be placed");
                                        t.RollBack();
                                    }
                                }
                                break;
                            }
                            else
                            {
                                TaskDialog.Show("Error", "The bathroom has no door");
                            }
                        }
                    }
                }
                catch
                {
                    return Result.Failed;
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
