using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using System.Collections.Generic;

namespace Asset.Extension
{
    /// <summary>
    /// Helper class to display element information in UI
    /// </summary>
    public class ElementDisplayInfo
    {
        public ElementId Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string RoomInfo { get; set; }
        public Element Element { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(RoomInfo))
                return $"{Name} - {Type} ({RoomInfo})";
            else
                return $"{Name} - {Type}";
        }
    }

    public static class ElementDisplayHelper
    {
        /// <summary>
        /// Convert elements to display-friendly format with room information for doors/windows
        /// </summary>
        public static List<ElementDisplayInfo> GetElementDisplayInfo(List<Element> elements)
        {
            var displayList = new List<ElementDisplayInfo>();

            foreach (var element in elements)
            {
                var info = new ElementDisplayInfo
                {
                    Id = element.Id,
                    Element = element,
                    Name = GetElementDisplayName(element),
                    Type = GetElementTypeName(element),
                    RoomInfo = GetRoomInfo(element)
                };

                displayList.Add(info);
            }

            return displayList;
        }

        private static string GetElementDisplayName(Element element)
        {
            // Try to get name parameter
            var nameParam = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM);
            if (nameParam != null && nameParam.HasValue)
                return nameParam.AsValueString();

            // Fallback to element name
            if (!string.IsNullOrEmpty(element.Name))
                return element.Name;

            // Last resort: category name + ID
            var category = element.Category;
            if (category != null)
                return $"{category.Name} [{element.Id.Value}]";

            return $"Element [{element.Id.Value}]";
        }

        private static string GetElementTypeName(Element element)
        {
            if (element is Wall)
                return "Wall";
            else if (element is Floor)
                return "Floor";
            else if (element is Ceiling)
                return "Ceiling";
            else if (element is Pipe)
                return "Pipe";
            else if (element is Duct)
                return "Duct";
            else if (element is FamilyInstance fi)
            {
                var category = fi.Category;
                if (category != null)
                {
                    if (category.Id.Value == (int)BuiltInCategory.OST_Doors)
                        return "Door";
                    else if (category.Id.Value == (int)BuiltInCategory.OST_Windows)
                        return "Window";
                    else
                        return category.Name;
                }
                return "Family Instance";
            }
            else
                return element.GetType().Name;
        }

        private static string GetRoomInfo(Element element)
        {
            if (element is FamilyInstance fi)
            {
                var category = fi.Category;
                if (category != null)
                {
                    bool isDoor = category.Id.Value == (int)BuiltInCategory.OST_Doors;
                    bool isWindow = category.Id.Value == (int)BuiltInCategory.OST_Windows;

                    if (isDoor || isWindow)
                    {
                        var toRoom = fi.ToRoom;
                        var fromRoom = fi.FromRoom;

                        if (toRoom != null && fromRoom != null)
                            return $"Between: {fromRoom.Name} → {toRoom.Name}";
                        else if (toRoom != null)
                            return $"To: {toRoom.Name}";
                        else if (fromRoom != null)
                            return $"From: {fromRoom.Name}";
                        else
                            return "Exterior";
                    }
                }
            }

            return string.Empty;
        }
    }
}