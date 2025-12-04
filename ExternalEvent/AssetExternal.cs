using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;

namespace Asset.ExternalEvent
{
    public class AssetExternal : IExternalEventHandler
    {
        // Data coming from the UI
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

                    foreach (ElementId id in TargetElementIds)
                    {
                        Element el = doc.GetElement(id);
                        if (el is FamilyInstance fi)
                        {
                            Parameter p = fi.LookupParameter("(01)ECD_ABS_L1_Asset");
                            var levelpram = fi.LookupParameter("(04)ECD_ABS_L2_Level");
                            var AssetRoomParam = fi.LookupParameter("(05)ECD_ABS_L3_Room");
                            if (p != null && !p.IsReadOnly && levelpram!=null && AssetRoomParam !=null)
                            {
                                p.Set(AssetValue ?? string.Empty);
                                levelpram.Set(AssetLevel ?? string.Empty);
                                AssetRoomParam.Set(AssetRoom ?? string.Empty);
                            }
                        }
                    }

                    t.Commit();
                }

                TaskDialog.Show("ABS Parameter", "ABS parameter assigned successfully.");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
            }
        }

        public string GetName()
        {
            return "ABS Parameter Assigned";
        }
    }
}
