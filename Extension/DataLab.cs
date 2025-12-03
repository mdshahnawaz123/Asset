using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asset.Extension
{
    public static class DataLab
    {
        public static IList<Room> GetRooms(this Document doc)
        {
            IList<Room> rooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>()
                .ToList();
            return rooms;
        }

        //Lets Get the List of Elements Based on Room
        public static IList<Element> GetElementsInRoom(this Document doc, Room room)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (room == null) throw new ArgumentNullException(nameof(room));

            // 1) Get room bounding box
            BoundingBoxXYZ roomBox = room.get_BoundingBox(null);
            if (roomBox == null)
                return new List<Element>();

            // 2) Build outline from Min / Max
            Outline outline = new Outline(roomBox.Min, roomBox.Max);

            // 3) Filter elements whose bounding boxes intersect this outline
            BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(outline);

            // Get only model elements (3D geometry), skip Rooms and Room Separation Lines
            IList<Element> candidates = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .WherePasses(bbFilter)
                .Where(e =>
                {
                    if (e is Room) return false;                       // skip rooms
                    if (e.Category == null) return false;

                    // only model categories (no annotation, etc.)
                    if (e.Category.CategoryType != CategoryType.Model)
                        return false;

                    // skip room separation lines explicitly
                    if (e.Category.Id.IntegerValue ==
                        (int)BuiltInCategory.OST_RoomSeparationLines)
                        return false;

                    return true;
                })
                .ToList();

            // 4) Keep only elements whose location point/curve is actually inside the room
            List<Element> elementsInRoom = new List<Element>();

            foreach (Element e in candidates)
            {
                LocationPoint lp = e.Location as LocationPoint;
                if (lp != null && room.IsPointInRoom(lp.Point))
                {
                    elementsInRoom.Add(e);
                    continue;
                }

                LocationCurve lc = e.Location as LocationCurve;
                if (lc != null)
                {
                    XYZ mid = (lc.Curve.GetEndPoint(0) + lc.Curve.GetEndPoint(1)) * 0.5;
                    if (room.IsPointInRoom(mid))
                        elementsInRoom.Add(e);
                }
            }

            return elementsInRoom;
        }
    }
}
