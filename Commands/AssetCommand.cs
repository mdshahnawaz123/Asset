using Asset.ExternalEvent;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

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

            // Create the handler
            var handler = new AssetExternal();

            // Create the ExternalEvent with the handler
            var exEvent = Autodesk.Revit.UI.ExternalEvent.Create(handler);

            try
            {
                // Pass the ExternalEvent (not the handler) to the UI
                var frm = new UI.AssetUI(doc, uidoc, exEvent, handler);
                frm.Show();
            }
            catch (System.Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}