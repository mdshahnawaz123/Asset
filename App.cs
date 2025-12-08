using Asset.Commands;
using Asset.Services;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;

namespace Asset
{
    public class App : IExternalApplication
    {
        private const string TargetTabName = "BIM Digital Design";
        private const string PanelName = "Asset Panel";

        // ---- Asset Tool button (existing) ---------------------------
        private const string ButtonInternalName = "Asset_Assign";
        private const string ButtonText = "Asset Tool";
        private const string ButtonTooltip = "Tool is used for assigning asset information based on ECD Asset parameters.";
        private const string CommandClass = "Asset.Commands.PramCheck";

        // ---- View 3D button (new) -----------------------------------
        private const string View3DButtonInternalName = "Asset_View3D";
        private const string View3DButtonText = "3D Views";
        private const string View3DButtonTooltip = "Create 3D views for each level.";
        private const string View3DCommandClass = "Asset.Commands.ViewCreation3D";

        private string AssemblyPath => Assembly.GetExecutingAssembly().Location;

        public Result OnStartup(UIControlledApplication application)
        {
            // Remote auth JSON
            var source = "https://raw.githubusercontent.com/mdshahnawaz123/plugin-access-control/main/users.json";

            // ---- Auth service init ------------------------------------------------
            try
            {
                var auth = new AuthService(source);
                PramCheck.Auth = auth;
            }
            catch
            {
                TryLog("startup_auth.log", "AuthService init failed.");
            }

            // ---- Ribbon UI --------------------------------------------------------
            try
            {
                RibbonPanel panel = FindExistingPanel(application, PanelName);
                if (panel == null)
                {
                    try
                    {
                        if (!TabExists(application, TargetTabName))
                        {
                            try { application.CreateRibbonTab(TargetTabName); }
                            catch { /* tab may already exist – ignore */ }
                        }

                        panel = application.CreateRibbonPanel(TargetTabName, PanelName);
                    }
                    catch
                    {
                        // fallback: default tab
                        panel = application.CreateRibbonPanel(PanelName);
                    }
                }

                // ----- Asset Tool button -----------------------------------------
                if (!PanelHasButton(panel, ButtonInternalName))
                {
                    var pushData = new PushButtonData(
                        ButtonInternalName,
                        ButtonText,
                        AssemblyPath,
                        CommandClass)
                    {
                        ToolTip = ButtonTooltip
                    };

                    var item = panel.AddItem(pushData);
                    var push = item as PushButton;

                    try
                    {
                        var large = LoadImageFromResource("Resources/Asset.png");
                        if (large != null && push != null) push.LargeImage = large;

                        var small = LoadImageFromResource("Resources/Asset.png");
                        if (small != null && push != null) push.Image = small;
                    }
                    catch { }
                }

                // ----- View Creation 3D button -----------------------------------
                if (!PanelHasButton(panel, View3DButtonInternalName))
                {
                    var view3DPushData = new PushButtonData(
                        View3DButtonInternalName,
                        View3DButtonText,
                        AssemblyPath,
                        View3DCommandClass)
                    {
                        ToolTip = View3DButtonTooltip
                    };

                    var view3DItem = panel.AddItem(view3DPushData);
                    var view3DPush = view3DItem as PushButton;

                    try
                    {
                        // reuse same icon (change path if you have a different PNG)
                        var large = LoadImageFromResource("Resources/3D.png");
                        if (large != null && view3DPush != null) view3DPush.LargeImage = large;

                        var small = LoadImageFromResource("Resources/3D.png");
                        if (small != null && view3DPush != null) view3DPush.Image = small;
                    }
                    catch { }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TryLog("startup.log", "OnStartup exception: " + ex);
                return Result.Succeeded;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            PramCheck.Auth = null;
            return Result.Succeeded;
        }

        // ---------------------------------------------------------------------
        // Helper methods
        // ---------------------------------------------------------------------
        private void TryLog(string fileName, string message)
        {
            try
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Asset");
                Directory.CreateDirectory(folder);
                var file = Path.Combine(folder, fileName);
                File.AppendAllText(file, DateTime.UtcNow.ToString("s") + " " + message + Environment.NewLine);
            }
            catch { }
        }

        private bool TabExists(UIControlledApplication app, string tabName)
        {
            try
            {
                var panels = app.GetRibbonPanels(tabName);
                return panels != null && panels.Count > 0;
            }
            catch { return false; }
        }

        private RibbonPanel FindExistingPanel(UIControlledApplication app, string panelName)
        {
            try
            {
                var allPanels = app.GetRibbonPanels();
                foreach (var p in allPanels)
                {
                    if (string.Equals(p.Name, panelName, StringComparison.OrdinalIgnoreCase))
                        return p;
                }
            }
            catch { }
            return null;
        }

        private bool PanelHasButton(RibbonPanel panel, string internalName)
        {
            if (panel == null) return false;
            try
            {
                var items = panel.GetItems();
                foreach (var it in items)
                {
                    if (string.Equals(it.Name, internalName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        private System.Windows.Media.ImageSource LoadImageFromResource(string relativeResourcePath)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var asmName = asm.GetName().Name;
                var packUri = $"pack://application:,,,/{asmName};component/{relativeResourcePath}";
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(packUri, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                try
                {
                    var asm = Assembly.GetExecutingAssembly();
                    var names = asm.GetManifestResourceNames();
                    string match = Array.Find(
                        names,
                        n => n.EndsWith(
                            relativeResourcePath.Replace('/', '.').Replace('\\', '.'),
                            StringComparison.OrdinalIgnoreCase));

                    if (string.IsNullOrEmpty(match)) return null;

                    using (var s = asm.GetManifestResourceStream(match))
                    {
                        if (s == null) return null;
                        var bmp = new System.Windows.Media.Imaging.BitmapImage();
                        bmp.BeginInit();
                        bmp.StreamSource = s;
                        bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                }
                catch { return null; }
            }
        }
    }
}
