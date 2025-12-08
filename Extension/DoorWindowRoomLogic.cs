using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System.Collections.Generic;
using System.Linq;

namespace Asset.Extension
{
    public static class DoorWindowRoomLogic
    {
        /// <summary>
        /// Get the primary room for a door or window
        /// Priority: ToRoom > FromRoom > Null (exterior)
        /// </summary>
        public static Room GetPrimaryRoomForDoorWindow(FamilyInstance doorOrWindow)
        {
            if (doorOrWindow == null)
                return null;

            // Check if it's a door or window
            var category = doorOrWindow.Category;
            if (category == null)
                return null;

            bool isDoor = category.Id.Value == (int)BuiltInCategory.OST_Doors;
            bool isWindow = category.Id.Value == (int)BuiltInCategory.OST_Windows;

            if (!isDoor && !isWindow)
                return null;

            // Get ToRoom and FromRoom
            Room toRoom = doorOrWindow.ToRoom;
            Room fromRoom = doorOrWindow.FromRoom;

            // Priority logic:
            // 1. If ToRoom exists, use it (room the door opens INTO)
            // 2. If only FromRoom exists, use it
            // 3. If neither exists, it's an exterior door/window
            if (toRoom != null)
                return toRoom;
            else if (fromRoom != null)
                return fromRoom;
            else
                return null; // Exterior door/window
        }

        /// <summary>
        /// Get both rooms connected by a door or window
        /// </summary>
        public static (Room fromRoom, Room toRoom) GetBothRoomsForDoorWindow(FamilyInstance doorOrWindow)
        {
            if (doorOrWindow == null)
                return (null, null);

            Room toRoom = doorOrWindow.ToRoom;
            Room fromRoom = doorOrWindow.FromRoom;

            return (fromRoom, toRoom);
        }

        /// <summary>
        /// Check if door/window is interior (connects two rooms) or exterior
        /// </summary>
        public static bool IsInteriorDoorWindow(FamilyInstance doorOrWindow)
        {
            if (doorOrWindow == null)
                return false;

            Room toRoom = doorOrWindow.ToRoom;
            Room fromRoom = doorOrWindow.FromRoom;

            // Interior if both rooms exist
            return toRoom != null && fromRoom != null;
        }

        /// <summary>
        /// Check if door/window is exterior (one or both sides are exterior)
        /// </summary>
        public static bool IsExteriorDoorWindow(FamilyInstance doorOrWindow)
        {
            if (doorOrWindow == null)
                return false;

            Room toRoom = doorOrWindow.ToRoom;
            Room fromRoom = doorOrWindow.FromRoom;

            // Exterior if at least one side is null
            return toRoom == null || fromRoom == null;
        }

        /// <summary>
        /// Get door/window assignment description
        /// </summary>
        public static string GetDoorWindowDescription(FamilyInstance doorOrWindow)
        {
            if (doorOrWindow == null)
                return "Unknown";

            Room toRoom = doorOrWindow.ToRoom;
            Room fromRoom = doorOrWindow.FromRoom;

            if (toRoom != null && fromRoom != null)
            {
                return $"Between '{fromRoom.Name}' and '{toRoom.Name}'";
            }
            else if (toRoom != null)
            {
                return $"From Exterior to '{toRoom.Name}'";
            }
            else if (fromRoom != null)
            {
                return $"From '{fromRoom.Name}' to Exterior";
            }
            else
            {
                return "Exterior (not associated with any room)";
            }
        }

        /// <summary>
        /// Get all doors and windows for a specific room
        /// Includes doors/windows where the room is either ToRoom or FromRoom
        /// </summary>
        public static List<FamilyInstance> GetDoorsAndWindowsForRoom(Document doc, Room room)
        {
            if (room == null || doc == null)
                return new List<FamilyInstance>();

            var result = new List<FamilyInstance>();

            // Get all doors
            var doors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>();

            foreach (var door in doors)
            {
                if (door.ToRoom?.Id == room.Id || door.FromRoom?.Id == room.Id)
                {
                    result.Add(door);
                }
            }

            // Get all windows
            var windows = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Windows)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>();

            foreach (var window in windows)
            {
                if (window.ToRoom?.Id == room.Id || window.FromRoom?.Id == room.Id)
                {
                    result.Add(window);
                }
            }

            return result;
        }

        /// <summary>
        /// Get only doors and windows that belong primarily to this room
        /// (where this room is the ToRoom, or FromRoom if ToRoom is null)
        /// </summary>
        public static List<FamilyInstance> GetPrimaryDoorsAndWindowsForRoom(Document doc, Room room)
        {
            if (room == null || doc == null)
                return new List<FamilyInstance>();

            var result = new List<FamilyInstance>();

            // Get all doors
            var doors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>();

            foreach (var door in doors)
            {
                var primaryRoom = GetPrimaryRoomForDoorWindow(door);
                if (primaryRoom?.Id == room.Id)
                {
                    result.Add(door);
                }
            }

            // Get all windows
            var windows = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Windows)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>();

            foreach (var window in windows)
            {
                var primaryRoom = GetPrimaryRoomForDoorWindow(window);
                if (primaryRoom?.Id == room.Id)
                {
                    result.Add(window);
                }
            }

            return result;
        }
    }
}