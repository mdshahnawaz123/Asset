using Asset.Extension;
using Asset.ExternalEvent;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Asset.UI
{
    /// <summary>
    /// Interaction logic for AssetUI.xaml
    /// </summary>
    public partial class AssetUI : Window
    {
        private Document doc;
        private UIDocument uidoc;
        private Autodesk.Revit.UI.ExternalEvent externalEvent;  // Changed type
        private AssetExternal handler;

        // Constructor now accepts ExternalEvent (not IExternalEventHandler)
        public AssetUI(Document doc, UIDocument uidoc, Autodesk.Revit.UI.ExternalEvent externalEvent, AssetExternal handler)
        {
            InitializeComponent();
            this.doc = doc;
            this.uidoc = uidoc;
            this.externalEvent = externalEvent;
            this.handler = handler;
            LV_Room.SelectionChanged += LV_Room_SelectionChanged;
            info();
        }

        // Fill Room list
        public void info()
        {
            LV_Room.ItemsSource = Extension.DataLab.GetRooms(doc);
            LV_Room.DisplayMemberPath = "Name";
        }

        // Button for Assign_ABS 
        private void Assign_ABS(object sender, RoutedEventArgs e)
        {
            var selectedRoom = LV_Room.SelectedItem as Room;
            if (selectedRoom == null)
            {
                TaskDialog.Show("Assign ABS", "Please select a room first.");
                return;
            }

            var elementsInRoom = Extension.DataLab.GetElementsInRoom(doc, selectedRoom);

            // Pass data from UI to handler
            handler.AssetValue = TB_Asset.Text;
            handler.AssetLevel = TB_Level.Text;
            handler.AssetRoom = selectedRoom.Number;
            handler.TargetElementIds = elementsInRoom.Select(el => el.Id).ToList();

            // Ask Revit to execute the external event
            externalEvent.Raise();  // Now calling on ExternalEvent, not IExternalEventHandler
        }

        // Button for Highlight_Model
        private void Highlight_Model(object sender, RoutedEventArgs e)
        {
            var selectedRoom = LV_Room.SelectedItem as Room;
            if (selectedRoom == null)
            {
                TaskDialog.Show("Highlight", "Please select a room first.");
                return;
            }

            var ele = Extension.DataLab.GetElementsInRoom(doc, selectedRoom);
            foreach (var element in ele)
            {
                uidoc.ShowElements(element);
            }
        }

        private void LV_Room_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedRoom = LV_Room.SelectedItem as Room;
            if (selectedRoom != null)
            {
                LV_Element.ItemsSource = Extension.DataLab.GetElementsInRoom(doc, selectedRoom);
                LV_Element.DisplayMemberPath = "Name";
            }
            
        }

        
    }
}