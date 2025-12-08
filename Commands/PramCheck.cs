using Asset.Services;
using Asset.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace Asset.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PramCheck : IExternalCommand
    {
        public static AuthService Auth { get; set; }

        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            try
            {
                if (Auth == null)
                {
                    message = "Authentication service not initialized.";
                    return Result.Failed;
                }

                var loaded = Auth.TryLoadRemote().GetAwaiter().GetResult();
                if (!loaded)
                {
                    TaskDialog.Show("Network required", "This addin requires internet to validate users. Please connect and try again.");
                    return Result.Cancelled;
                }

                var token = TokenService.LoadToken();
                var machineId = MachineHelper.GetMachineId();

                if (token != null && token.MachineId == machineId && token.ExpiresUtc.Date >= DateTime.UtcNow.Date)
                {
                    var remoteUser = Auth.GetUser(token.Username);
                    if (remoteUser != null && remoteUser.Active && remoteUser.Expires.Date >= DateTime.UtcNow.Date)
                    {
                        Auth.CurrentUser = remoteUser;
                        var revitHandle = GetRevitHandle(commandData);
                        return RunMainWindow(doc, uidoc, revitHandle);
                    }
                    else
                    {
                        TokenService.DeleteToken();
                    }
                }

                var login = new LoginWindow(Auth);
                var revitHandleLogin = GetRevitHandle(commandData);
                new WindowInteropHelper(login) { Owner = revitHandleLogin };

                var dlg = login.ShowDialog();
                if (dlg != true)
                {
                    return Result.Cancelled;
                }

                var user = Auth.CurrentUser;
                if (user == null)
                {
                    TaskDialog.Show("Login error", "Failed to retrieve user after login.");
                    return Result.Failed;
                }

                var localToken = new LocalAuthToken
                {
                    Username = user.Username,
                    MachineId = machineId,
                    ExpiresUtc = user.Expires.ToUniversalTime()
                };
                TokenService.SaveToken(localToken);

                var revitHandleAfterLogin = GetRevitHandle(commandData);
                return RunMainWindow(doc, uidoc, revitHandleAfterLogin);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private Result RunMainWindow(Autodesk.Revit.DB.Document doc, UIDocument uidoc, IntPtr revitHandle)
        {
            var user = Auth.CurrentUser;
            if (!IsUserAllowed(user))
            {
                TaskDialog.Show("Access denied", "You do not have permission to run this tool.");
                return Result.Failed;
            }

            // Create your external event handler and ExternalEvent
            // If your AssetExternal constructor takes parameters, adjust this line accordingly.
            var handler = new AssetExternal();
            var externalEvent = ExternalEvent.Create(handler);

            // Call the constructor that requires (Document, UIDocument, ExternalEvent, AssetExternal)
            var frm = new UI.AssetUI(doc, uidoc, externalEvent, handler);

            frm.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            new WindowInteropHelper(frm) { Owner = revitHandle };
            frm.Show();

            return Result.Succeeded;
        }

        private bool IsUserAllowed(UserRecord user)
        {
            if (user == null) return false;
            return user.Active && user.Expires.Date >= DateTime.UtcNow.Date;
        }

        private IntPtr GetRevitHandle(ExternalCommandData commandData)
        {
            try
            {
                var handle = new IntPtr((int)commandData.Application.MainWindowHandle);
                if (handle != IntPtr.Zero) return handle;
            }
            catch
            {
                // ignore
            }

            return System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        }
    }
}
