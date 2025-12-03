using Asset.Extension;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Asset.UI
{
    /// <summary>
    /// Interaction logic for AssetUI.xaml
    /// </summary>
    public partial class AssetUI : Window
    {
        Document doc;
        private UIDocument uidoc;
        public AssetUI(Document doc)
        {
            InitializeComponent();
            this.doc = doc;
            LV_Room.SelectionChanged += LV_Room_SelectionChanged;
            Revitinfo();
            info();
        }
        //Class for General Information:

        public void info()
        {
            LV_Room.ItemsSource = Extension.DataLab.GetRooms(doc);
            LV_Room.DisplayMemberPath = "Name";
        }
            




        //Button for Assign_ABS 

        private void Assign_ABS(object sender, RoutedEventArgs e)
        {
            var assetInfo = TB_Asset.Text;
            TaskDialog.Show("Message", assetInfo);
        }

        //Button for Highlight_Model

        private void Highlight_Model(object sender, RoutedEventArgs e)
        {
            //This Area Will be for the HighLight the Model In Revit:
            var selectedRoom = LV_Room.SelectedItem as Room;
            var ele = DataLab.GetElementsInRoom(doc, selectedRoom);
            foreach(var element in ele)
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

        public void Revitinfo()
        {
            var selectedElement = LV_Element.SelectedItem;
            if(selectedElement is FamilyInstance fi)
            {
                TaskDialog.Show("Message", fi.Name.ToString());
            }
        }
    }
}
