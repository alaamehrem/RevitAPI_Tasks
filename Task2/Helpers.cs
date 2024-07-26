using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task2
{
    public class Helpers
    {
        public static List<FamilyInstance> GetDoors(Document doc,Room room, IList<IList<BoundarySegment>> roomSegments)
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
        
    }
}
