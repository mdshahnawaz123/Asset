using Asset.Extension;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asset.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ViewCreation3D : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;
            try
            {
                using(Transaction t = new Transaction(doc,"Create New 3D Views"))
                {
                    t.Start();

                    //Lets Create the 3D views 

                    doc.CreateView3D();

                    t.Commit();
                }

            }
            catch(Exception ex)
            {
                TaskDialog.Show("error",ex.Message);
            }
            return Result.Succeeded;
        }
    }
}
