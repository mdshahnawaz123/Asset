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
        private const string ButtonInternalName = "Asset_Assign";
        private const string ButtonText = "Asset Tool";
        private const string ButtonTooltip = "Tool is use for Assign Asset information Based on ECD Asset Parameter";
        private const string CommandClass = "Asset.Commands.PramCheck";

        private string AssemblyPath => Assembly.GetExecutingAssembly().Location;

        public Result OnStartup(UIControlledApplication application)
        {
            // use RAW GitHub URL so JSON is returned, not HTML
            var source = "https://raw.githubusercontent.com/mdshahnawaz123/plugin-access-control/main/users.json";

            // ---- Auth service init ------------------------------------------------
            try
            {
                var auth = new AuthService(source);
                PramCheck.Auth = auth;
            }
            catch
            {
                // log but don't block startup
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

                if (!PanelHasButton(panel, ButtonInternalName))
                {
                    var pushData = new PushButtonData(ButtonInternalName, ButtonText, AssemblyPath, CommandClass)
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

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TryLog("startup.log", "OnStartup exception: " + ex);
                // still allow Revit to continue
                return Result.Succeeded;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            PramCheck.Auth = null;
            return Result.Succeeded;
        }

        // ---------------------------------------------------------------------
        // Helper methods (same pattern as your CostAnalysis code)
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
