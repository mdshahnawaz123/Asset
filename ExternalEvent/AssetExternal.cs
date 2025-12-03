using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Asset.ExternalEvent
{
    public class AssetExternal : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            try
            {
                //This Area Will be for External Event Execution Code

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
