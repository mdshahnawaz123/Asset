using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;
using System.Collections.Generic;
using System.Linq;

namespace Asset.Extension
{
    public class RoomInfo
    {
        public Room Room { get; set; }
        public string RoomName { get; set; }
        public string RoomNumber { get; set; }
        public bool IsLinked { get; set; }
        public string LinkName { get; set; }
        public Document SourceDocument { get; set; }

        public override string ToString()
        {
            if (IsLinked)
                return $"{RoomNumber} - {RoomName} [Linked: {LinkName}]";
            else
                return $"{RoomNumber} - {RoomName} [Host]";
        }
    }

    public static class DataLab
    {
        /// <summary>
        /// Get rooms from host model only
        /// </summary>
        public static List<Room> GetRooms(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>()
                .Where(r => r.Area > 0)
                .ToList();
        }

        /// <summary>
        /// Get rooms from both host and linked models with metadata
        /// </summary>
        public static List<RoomInfo> GetAllRooms(Document doc, bool includeLinkedModels = true)
        {
            var allRooms = new List<RoomInfo>();

            try
            {
                // 1. Get rooms from host model
                var hostRooms = GetRooms(doc);
                foreach (var room in hostRooms)
                {
                    try
                    {
                        allRooms.Add(new RoomInfo
                        {
                            Room = room,
                            RoomName = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "",
                            RoomNumber = room.Number ?? "",
                            IsLinked = false,
                            LinkName = "Host Model",
                            SourceDocument = doc
                        });
                    }
                    catch
                    {
                        // Skip individual room if it has issues
                        continue;
                    }
                }

                // 2. Get rooms from linked models if requested
                if (includeLinkedModels)
                {
                    try
                    {
                        var linkedRooms = GetRoomsFromLinkedModels(doc);
                        allRooms.AddRange(linkedRooms);
                    }
                    catch
                    {
                        // Continue even if linked models fail to load
                    }
                }
            }
            catch
            {
                // Return whatever rooms we could collect
            }

            return allRooms;
        }

        /// <summary>
        /// Get all rooms from linked Revit models
        /// </summary>
        private static List<RoomInfo> GetRoomsFromLinkedModels(Document doc)
        {
            var linkedRooms = new List<RoomInfo>();

            try
            {
                // Get all RevitLinkInstances
                var linkInstances = new FilteredElementCollector(doc)
                    .OfClass(typeof(RevitLinkInstance))
                    .Cast<RevitLinkInstance>()
                    .ToList();

                foreach (var linkInstance in linkInstances)
                {
                    try
                    {
                        // Check if link is loaded
                        if (linkInstance.GetLinkDocument() == null)
                        {
                            // Link is not loaded, skip
                            continue;
                        }

                        Document linkedDoc = linkInstance.GetLinkDocument();

                        // Double-check linkedDoc is valid
                        if (linkedDoc == null || linkedDoc.IsFamilyDocument)
                            continue;

                        string linkName = GetLinkDisplayName(linkInstance);

                        // Get rooms from linked document with validation
                        var rooms = new FilteredElementCollector(linkedDoc)
                            .OfCategory(BuiltInCategory.OST_Rooms)
                            .WhereElementIsNotElementType()
                            .Cast<Room>()
                            .Where(r => r != null && r.Area > 0)
                            .ToList();

                        foreach (var room in rooms)
                        {
                            try
                            {
                                linkedRooms.Add(new RoomInfo
                                {
                                    Room = room,
                                    RoomName = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "",
                                    RoomNumber = room.Number ?? "",
                                    IsLinked = true,
                                    LinkName = linkName,
                                    SourceDocument = linkedDoc
                                });
                            }
                            catch
                            {
                                // Skip individual room if it has issues
                                continue;
                            }
                        }
                    }
                    catch
                    {
                        // Skip if linked document cannot be accessed
                        continue;
                    }
                }
            }
            catch
            {
                // Return empty list if collector fails
                return linkedRooms;
            }

            return linkedRooms;
        }

        /// <summary>
        /// Get a friendly display name for a link instance
        /// </summary>
        private static string GetLinkDisplayName(RevitLinkInstance linkInstance)
        {
            try
            {
                // Try to get the link type name
                RevitLinkType linkType = linkInstance.Document.GetElement(linkInstance.GetTypeId()) as RevitLinkType;
                if (linkType != null)
                {
                    string name = linkType.Name;
                    // Remove .rvt extension if present
                    if (name.EndsWith(".rvt", System.StringComparison.OrdinalIgnoreCase))
                        name = name.Substring(0, name.Length - 4);
                    return name;
                }
                return linkInstance.Name;
            }
            catch
            {
                return linkInstance.Name;
            }
        }

        /// <summary>
        /// Validate if a room number exists in host or linked models
        /// </summary>
        public static RoomInfo FindRoomByNumber(Document doc, string roomNumber, bool searchLinkedModels = true)
        {
            if (string.IsNullOrWhiteSpace(roomNumber))
                return null;

            try
            {
                var allRooms = GetAllRooms(doc, searchLinkedModels);
                return allRooms.FirstOrDefault(r =>
                    r != null &&
                    !string.IsNullOrEmpty(r.RoomNumber) &&
                    r.RoomNumber.Equals(roomNumber, System.StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get all 3D elements in a room (including doors/windows using room relationship)
        /// Works with both host and linked rooms
        /// </summary>
        public static List<Element> GetElementsInRoom(Document doc, Room room)
        {
            if (room == null)
                return new List<Element>();

            var elementsInRoom = new List<Element>();

            // 1. Get Doors and Windows using ToRoom/FromRoom logic
            var doorsAndWindows = DoorWindowRoomLogic.GetPrimaryDoorsAndWindowsForRoom(doc, room);
            elementsInRoom.AddRange(doorsAndWindows);

            // 2. Get other FamilyInstances (Furniture, Equipment, etc.) - EXCLUDING Doors and Windows
            var familyInstances = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(fi =>
                {
                    // Exclude doors and windows (already handled above)
                    var cat = fi.Category;
                    if (cat == null) return false;

                    bool isDoor = cat.Id.IntegerValue == (int)BuiltInCategory.OST_Doors;
                    bool isWindow = cat.Id.IntegerValue == (int)BuiltInCategory.OST_Windows;

                    if (isDoor || isWindow)
                        return false;

                    // Check if in room
                    return IsElementInRoom(fi, room);
                })
                .Cast<Element>();

            elementsInRoom.AddRange(familyInstances);

            // 3. Get Floors
            var floors = new FilteredElementCollector(doc)
                .OfClass(typeof(Floor))
                .Cast<Floor>()
                .Where(f => IsElementInRoom(f, room))
                .Cast<Element>();

            elementsInRoom.AddRange(floors);

            // 4. Get Ceilings
            var ceilings = new FilteredElementCollector(doc)
                .OfClass(typeof(Ceiling))
                .Cast<Ceiling>()
                .Where(c => IsElementInRoom(c, room))
                .Cast<Element>();

            elementsInRoom.AddRange(ceilings);

            // 5. Get Pipes
            var pipes = new FilteredElementCollector(doc)
                .OfClass(typeof(Pipe))
                .Cast<Pipe>()
                .Where(p => IsElementInRoom(p, room))
                .Cast<Element>();

            elementsInRoom.AddRange(pipes);

            // 6. Get Ducts
            var ducts = new FilteredElementCollector(doc)
                .OfClass(typeof(Duct))
                .Cast<Duct>()
                .Where(d => IsElementInRoom(d, room))
                .Cast<Element>();

            elementsInRoom.AddRange(ducts);

            // 7. Get Cable Trays
            var cableTrays = new FilteredElementCollector(doc)
                .OfClass(typeof(CableTray))
                .Cast<CableTray>()
                .Where(ct => IsElementInRoom(ct, room))
                .Cast<Element>();

            elementsInRoom.AddRange(cableTrays);

            // 8. Get Conduits
            var conduits = new FilteredElementCollector(doc)
                .OfClass(typeof(Conduit))
                .Cast<Conduit>()
                .Where(c => IsElementInRoom(c, room))
                .Cast<Element>();

            elementsInRoom.AddRange(conduits);

            // 9. Get Flex Ducts
            var flexDucts = new FilteredElementCollector(doc)
                .OfClass(typeof(FlexDuct))
                .Cast<FlexDuct>()
                .Where(fd => IsElementInRoom(fd, room))
                .Cast<Element>();

            elementsInRoom.AddRange(flexDucts);

            // 10. Get Flex Pipes
            var flexPipes = new FilteredElementCollector(doc)
                .OfClass(typeof(FlexPipe))
                .Cast<FlexPipe>()
                .Where(fp => IsElementInRoom(fp, room))
                .Cast<Element>();

            elementsInRoom.AddRange(flexPipes);

            return elementsInRoom.Distinct().ToList();
        }

        private static bool IsElementInRoom(Element element, Room room)
        {
            try
            {
                // For elements in host document
                Document hostDoc = room.Document;

                if (element is FamilyInstance fi)
                {
                    var locationPoint = fi.Location as LocationPoint;
                    if (locationPoint != null)
                    {
                        var point = locationPoint.Point;
                        var elemRoom = hostDoc.GetRoomAtPoint(point);
                        return elemRoom != null && elemRoom.Id == room.Id;
                    }
                }

                var bbox = element.get_BoundingBox(null);
                if (bbox != null)
                {
                    var center = (bbox.Min + bbox.Max) / 2;
                    var elemRoom = hostDoc.GetRoomAtPoint(center);
                    return elemRoom != null && elemRoom.Id == room.Id;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsWallInRoom(Wall wall, Room room)
        {
            try
            {
                var roomBoundaries = room.GetBoundarySegments(new SpatialElementBoundaryOptions());

                if (roomBoundaries != null)
                {
                    foreach (var boundaryList in roomBoundaries)
                    {
                        foreach (var segment in boundaryList)
                        {
                            var elemId = segment.ElementId;
                            if (elemId == wall.Id)
                                return true;
                        }
                    }
                }

                return IsElementInRoom(wall, room);
            }
            catch
            {
                return false;
            }
        }
    }
}