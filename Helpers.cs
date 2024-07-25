using Autodesk.Revit.DB;
using System.Collections.Generic;


namespace RevitAPI_Tasks.Task1
{
    public class Helpers
    {
        public static bool IsLinesFlattened(List<Curve> lines)
        {
            foreach (var line in lines)
            {
                if (line.GetEndPoint(0).Z != 0 || line.GetEndPoint(1).Z != 0)
                {
                    return false;
                }
            }
            return true;
        }   
        public static List<Curve> FlattenList(List<Curve> lines)
        {
            List<Curve> flattenedList = new List<Curve>();
            foreach (var line in lines)
            {
                var startpt = line.GetEndPoint(0);
                var endpt = line.GetEndPoint(1);
                flattenedList.Add(Line.CreateBound(new XYZ(startpt.X, startpt.Y, 0), new XYZ(endpt.X, endpt.Y, 0)));
            }
            return flattenedList;
        }

        public static List<Curve> RearrangeLines(List<Curve> lines)
        {
            List<Curve> rearrangedList = new List<Curve> { lines[0] };
            lines.RemoveAt(0);
            while (lines.Count > 0)
            {
                bool isFound = false;
                var endpt1 = rearrangedList[rearrangedList.Count - 1].GetEndPoint(1);
                for (int j = 0; j < lines.Count; j++)
                {
                    var startpt = lines[j].GetEndPoint(0);
                    if (endpt1.DistanceTo(startpt) == 0)
                    {
                        rearrangedList.Add(lines[j]);
                        lines.RemoveAt(j);
                        isFound = true;
                        break;
                    }
                }
                if (!isFound)
                {
                    return new List<Curve>();
                }
            }
            return rearrangedList;
        }

    }
}
