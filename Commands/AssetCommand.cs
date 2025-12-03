using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace Asset.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AssetCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            try
            {
                //This Area Will be for Command Execution Code
                var frm = new UI.AssetUI(doc);
                frm.Show();

            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Result.Succeeded;
        }
    }
}

