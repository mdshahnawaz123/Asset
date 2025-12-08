using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class AssetExternal : IExternalEventHandler
{
    public string AssetValue { get; set; }
    public string AssetLevel { get; set; }
    public string AssetRoom { get; set; }
    public IList<ElementId> TargetElementIds { get; set; } = new List<ElementId>();

    public void Execute(UIApplication app)
    {
        try
        {
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (TargetElementIds == null || TargetElementIds.Count == 0)
            {
                TaskDialog.Show("ABS Parameter", "No elements to process.");
                return;
            }

            using (Transaction t = new Transaction(doc, "Assign ABS Parameter"))
            {
                t.Start();

                int successCount = 0;
                int skippedCount = 0;

                foreach (ElementId id in TargetElementIds)
                {
                    Element el = doc.GetElement(id);

                    // Skip if element is null
                    if (el == null)
                    {
                        skippedCount++;
                        continue;
                    }

                    // Check if element is annotation (skip these)
                    if (IsAnnotationElement(el))
                    {
                        skippedCount++;
                        continue;
                    }

                    // Process different element types
                    bool processed = false;

                    // FamilyInstance (Doors, Windows, Furniture, MEP Equipment, etc.)
                    if (el is FamilyInstance fi)
                    {
                        processed = SetParametersOnElement(fi);
                    }
                    // Walls
                    //else if (el is Wall wall)
                    //{
                    //    processed = SetParametersOnElement(wall);
                    //}
                    // Floors
                    else if (el is Floor floor)
                    {
                        processed = SetParametersOnElement(floor);
                    }
                    // Ceilings
                    else if (el is Ceiling ceiling)
                    {
                        processed = SetParametersOnElement(ceiling);
                    }
                    // Roofs
                    else if (el is RoofBase roof)
                    {
                        processed = SetParametersOnElement(roof);
                    }
                    // Pipes
                    else if (el is Pipe pipe)
                    {
                        processed = SetParametersOnElement(pipe);
                    }
                    // Ducts
                    else if (el is Duct duct)
                    {
                        processed = SetParametersOnElement(duct);
                    }
                    // Cable Trays
                    else if (el is CableTray cableTray)
                    {
                        processed = SetParametersOnElement(cableTray);
                    }
                    // Conduits
                    else if (el is Conduit conduit)
                    {
                        processed = SetParametersOnElement(conduit);
                    }
                    // Flex Ducts
                    else if (el is FlexDuct flexDuct)
                    {
                        processed = SetParametersOnElement(flexDuct);
                    }
                    // Flex Pipes
                    else if (el is FlexPipe flexPipe)
                    {
                        processed = SetParametersOnElement(flexPipe);
                    }
                    // Add more element types as needed

                    if (processed)
                        successCount++;
                    else
                        skippedCount++;
                }

                t.Commit();

                // Show result message
                string message = $"ABS parameters assigned successfully.\n\n" +
                                $"Processed: {successCount} elements\n" +
                                $"Skipped: {skippedCount} elements";
                TaskDialog.Show("ABS Parameter", message);
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", ex.Message);
        }
    }

    // Method to set parameters on any element
    private bool SetParametersOnElement(Element element)
    {
        try
        {
            Parameter assetParam = element.LookupParameter("(01)ECD_ABS_L1_Asset");
            Parameter levelParam = element.LookupParameter("(04)ECD_ABS_L2_Level");
            Parameter roomParam = element.LookupParameter("(05)ECD_ABS_L3_Room");

            // Check if all parameters exist and are writable
            bool hasAsset = assetParam != null && !assetParam.IsReadOnly;
            bool hasLevel = levelParam != null && !levelParam.IsReadOnly;
            bool hasRoom = roomParam != null && !roomParam.IsReadOnly;

            // Set parameters if they exist
            if (hasAsset)
                assetParam.Set(AssetValue ?? string.Empty);

            if (hasLevel)
                levelParam.Set(AssetLevel ?? string.Empty);

            if (hasRoom)
                roomParam.Set(AssetRoom ?? string.Empty);

            // Return true if at least one parameter was set
            return hasAsset || hasLevel || hasRoom;
        }
        catch
        {
            return false;
        }
    }

    // Method to check if element is an annotation
    private bool IsAnnotationElement(Element element)
    {
        if (element == null || element.Category == null)
            return false;

        var category = element.Category;

        // Check common annotation categories
        var annotationCategories = new[]
        {
            BuiltInCategory.OST_TextNotes,
            BuiltInCategory.OST_Dimensions,
            BuiltInCategory.OST_GenericAnnotation,
            BuiltInCategory.OST_Tags,
            BuiltInCategory.OST_RoomTags,
            BuiltInCategory.OST_DoorTags,
            BuiltInCategory.OST_WindowTags,
            BuiltInCategory.OST_DetailComponents,
            BuiltInCategory.OST_Lines,
            BuiltInCategory.OST_SketchLines
        };

        foreach (var annotCat in annotationCategories)
        {
            if (category.Id.Value == (int)annotCat)
                return true;
        }

        // Additional check: if element has no 3D geometry
        try
        {
            Options options = new Options();
            options.ComputeReferences = false;
            options.DetailLevel = ViewDetailLevel.Coarse;

            GeometryElement geom = element.get_Geometry(options);

            if (geom == null || !geom.Any())
                return true;
        }
        catch
        {
            // If we can't get geometry, assume it's not 3D
            return true;
        }

        return false;
    }

    public string GetName()
    {
        return "ABS Parameter Assigned";
    }
}