using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Architecture;
using System.Collections;

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
                CreateFloors(doc, allRooms);
                tg.Assimilate();
                }
            return Result.Succeeded;
        }

        public void CreateFloors(Document doc, List<Room> allRooms)
        {
            foreach (var room in allRooms)
            {
                // Get the room's boundary segments points
                var roomSegments = room.GetBoundarySegments(new SpatialElementBoundaryOptions()).ToList(); // List<List<BoundarySegment>>
                List<XYZ> roomBoundaryPts = roomSegments.SelectMany(segList => segList.Select(seg => seg.GetCurve().GetEndPoint(0))).ToList();

                //Get the room doors
                var roomDoors = GetDoors(doc, room, roomSegments);

                //Get the room's threshold boundary points
                List<XYZ> thresholdBoundaryPts = GetThresholdBoundary(roomDoors, doc);

                //Union between the threshold boundaries and the room's boundary segments
                List<Curve> closedLoopCurves = GenerateClosedLoopFromTwoPointLists(roomBoundaryPts, thresholdBoundaryPts, doc);

                // Get the Floor Type Id and Level Id
                var floorId = new FilteredElementCollector(doc).OfClass(typeof(FloorType)).First().Id;
                var levelId = room.LevelId;

                // Create the floor 
                if (closedLoopCurves.Any())
                {
                    CurveLoop curveLoop = CurveLoop.Create(closedLoopCurves);
                    List<CurveLoop> profiles = new List<CurveLoop>() { curveLoop };
                    using (Transaction transaction = new Transaction(doc, "Create Floor"))
                    {
                        transaction.Start();
                        try
                        {
                            Floor.Create(doc, profiles, floorId, levelId);
                        }
                        catch
                        { }
                        transaction.Commit();
                    }

                }
                else
                {
                    TaskDialog.Show("Error", "The lines do not form a closed loop");
                }
            }
        }

        public List<XYZ> GetThresholdBoundary(List<FamilyInstance> roomDoors, Document doc)
        {
            List<XYZ> thresholdBoundaryPts = new List<XYZ>();
            foreach (var door in roomDoors)
            {
                // Get the door width and the wall thickness
                Double doorWidth = doc.GetElement(door.GetTypeId()).ParametersMap.get_Item("Width").AsDouble();

                WallType wallHost = doc.GetElement(door.Host.GetTypeId()) as WallType;
                Wall wallElem = doc.GetElement(door.Host.Id) as Wall;
                Double ThersholdThickness = wallHost.ParametersMap.get_Item("Width").AsDouble() / 2;

                XYZ doorLocation = (door.Location as LocationPoint).Point;
                XYZ wallOrientation = wallElem.Orientation;


                // Create the threshold boundary

                if (wallOrientation.Y == 1) // Horizontal Door
                {
                    if (door.HandFlipped)
                    {
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X - doorWidth / 2, doorLocation.Y, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X + doorWidth / 2, doorLocation.Y, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X + doorWidth / 2, doorLocation.Y, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X + doorWidth / 2, doorLocation.Y + ThersholdThickness, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X + doorWidth / 2, doorLocation.Y + ThersholdThickness, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X - doorWidth / 2, doorLocation.Y + ThersholdThickness, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X - doorWidth / 2, doorLocation.Y + ThersholdThickness, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X - doorWidth / 2, doorLocation.Y, 0));
                    }
                    else
                    {
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X - doorWidth / 2, doorLocation.Y, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X + doorWidth / 2, doorLocation.Y, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X + doorWidth / 2, doorLocation.Y, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X + doorWidth / 2, doorLocation.Y - ThersholdThickness, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X + doorWidth / 2, doorLocation.Y - ThersholdThickness, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X - doorWidth / 2, doorLocation.Y - ThersholdThickness, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X - doorWidth / 2, doorLocation.Y - ThersholdThickness, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X - doorWidth / 2, doorLocation.Y, 0));
                    }

                }
                else // Vertical Door
                {
                    if (door.FacingFlipped == false)
                    {
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X, doorLocation.Y - doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X, doorLocation.Y + doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X, doorLocation.Y + doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X + ThersholdThickness, doorLocation.Y + doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X + ThersholdThickness, doorLocation.Y + doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X + ThersholdThickness, doorLocation.Y - doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X + ThersholdThickness, doorLocation.Y - doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X, doorLocation.Y - doorWidth / 2, 0));
                    }
                    else
                    {
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X, doorLocation.Y - doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X, doorLocation.Y + doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X, doorLocation.Y + doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X - ThersholdThickness, doorLocation.Y + doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X - ThersholdThickness, doorLocation.Y + doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X - ThersholdThickness, doorLocation.Y - doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X - ThersholdThickness, doorLocation.Y - doorWidth / 2, 0));
                        thresholdBoundaryPts.Add(new XYZ(doorLocation.X, doorLocation.Y - doorWidth / 2, 0));
                    }

                }

            }
            return thresholdBoundaryPts;
        }
        public List<XYZ> MergePoints(List<XYZ> list1, List<XYZ> list2)
        {
            List<XYZ> points = new List<XYZ>();
            points.AddRange(list1);
            points.AddRange(list2);
            return points;
        }
        public List<XYZ> CreateClosedLoop(List<XYZ> points, double tolerance = 0.001)
        {
            if (points.Count < 3)
            {
                TaskDialog.Show("Error","A minimum of three points is required to form a closed loop.");
            }

            List<XYZ> loop = new List<XYZ>();
            XYZ currentPoint = points[0];
            loop.Add(currentPoint);
            points.RemoveAt(0);

            while (points.Count > 0)
            {
                XYZ nextPoint = points.OrderBy(p => p.DistanceTo(currentPoint)).First();
                if (!IsAlmostEqual(loop.Last(), nextPoint, tolerance))
                {
                    loop.Add(nextPoint);
                    currentPoint = nextPoint;
                }
                points.Remove(nextPoint);
            }

            // Close the loop by adding the first point at the end
            if (!IsAlmostEqual(loop.Last(), loop[0], tolerance))
            {
                loop.Add(loop[0]);
            }
            return loop;
        }

        public bool IsAlmostEqual(XYZ point1, XYZ point2, double tolerance)
        {
            return point1.DistanceTo(point2) <= tolerance;
        }
        public List<Curve> CreateCurvesFromPoints(List<XYZ> points, Document doc)
        {
            List<Curve> curves = new List<Curve>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                Curve curve = Line.CreateBound(points[i], points[i + 1]);
                curves.Add(curve);
            }

            return curves;
        }
        public List<Curve> GenerateClosedLoopFromTwoPointLists(List<XYZ> list1, List<XYZ> list2, Document doc)
        {
            List<XYZ> mergedPoints = MergePoints(list1, list2);
            List<Point> points = mergedPoints.Select(coord => Point.Create(coord)).ToList();
            List<GeometryObject> shapes = new List<GeometryObject>();
            foreach (var point in points)
            {
                shapes.Add(point);
            }
            VisualizeShape(doc, shapes);
            List<XYZ> closedLoopPoints = CreateClosedLoop(mergedPoints, 0.001);
            List<Curve> closedLoopCurves = CreateCurvesFromPoints(closedLoopPoints, doc);

            return closedLoopCurves;
        }
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
        public static void VisualizeShape(Document doc , List<GeometryObject> shapes)
        {
            using(Transaction t = new Transaction(doc,"Visualize"))
            {
                t.Start();
                DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel)).SetShape(shapes);
                t.Commit();
            }
        }


    }
}
