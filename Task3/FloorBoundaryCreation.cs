using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitAPI_Tasks.Task3
{
    public class FloorBoundaryCreation
    {
        public static void CreateFloors(Document doc, List<Room> allRooms)
        {
            foreach (var room in allRooms)
            {
                // Get the room's boundary segments points
                var roomSegments = room.GetBoundarySegments(new SpatialElementBoundaryOptions()).ToList(); // List<List<BoundarySegment>>
                List<XYZ> roomBoundaryPts = roomSegments.SelectMany(segList => segList.Select(seg => seg.GetCurve().GetEndPoint(0))).ToList();

                //Get the room doors
                var roomDoors = Helpers.GetDoors(doc, room, roomSegments);

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

        // Get the threshold boundary points from door width and wall thickness
        public static List<XYZ> GetThresholdBoundary(List<FamilyInstance> roomDoors, Document doc)
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

        // Merge two lists of points
        public static List<XYZ> MergePoints(List<XYZ> list1, List<XYZ> list2)
        {
            List<XYZ> points = new List<XYZ>();
            points.AddRange(list1);
            points.AddRange(list2);
            return points;
        }

        // Create a closed loop from a list of points
        public static List<XYZ> CreateClosedLoop(List<XYZ> points, double tolerance = 0.001)
        {
            if (points.Count < 3)
            {
                TaskDialog.Show("Error", "A minimum of three points is required to form a closed loop.");
            }

            List<XYZ> loop = new List<XYZ>();
            XYZ currentPoint = points[0];
            loop.Add(currentPoint);
            points.RemoveAt(0);

            while (points.Count > 0)
            {
                XYZ nextPoint = points.OrderBy(p => p.DistanceTo(currentPoint)).First();
                if (!Helpers.IsAlmostEqual(loop.Last(), nextPoint, tolerance))
                {
                    loop.Add(nextPoint);
                    currentPoint = nextPoint;
                }
                points.Remove(nextPoint);
            }
            // Close the loop by adding the first point at the end
            if (!Helpers.IsAlmostEqual(loop.Last(), loop[0], tolerance))
            {
                loop.Add(loop[0]);
            }
            return loop;
        }

        // Create curves from a list of points
        public static List<Curve> CreateCurvesFromPoints(List<XYZ> points, Document doc)
        {
            List<Curve> curves = new List<Curve>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                Curve curve = Line.CreateBound(points[i], points[i + 1]);
                curves.Add(curve);
            }

            return curves;
        }
        public static List<Curve> GenerateClosedLoopFromTwoPointLists(List<XYZ> list1, List<XYZ> list2, Document doc)
        {
            List<XYZ> mergedPoints = MergePoints(list1, list2);
            List<Point> points = mergedPoints.Select(coord => Point.Create(coord)).ToList();
            List<GeometryObject> shapes = new List<GeometryObject>();
            foreach (var point in points)
            {
                shapes.Add(point);
            }

            //Helpers.VisualizeShape(doc, shapes);
            List<XYZ> closedLoopPoints = CreateClosedLoop(mergedPoints, 0.001);
            List<Curve> closedLoopCurves = CreateCurvesFromPoints(closedLoopPoints, doc);

            return closedLoopCurves;
        }
    }
}
