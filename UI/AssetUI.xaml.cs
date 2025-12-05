using Asset.Extension;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private Autodesk.Revit.UI.ExternalEvent externalEvent;
        private AssetExternal handler;
        private List<RoomInfo> allRooms;

        public ObservableCollection<CategoryItem> CategoryItems { get; set; } = new ObservableCollection<CategoryItem>();

        public AssetUI(Document doc, UIDocument uidoc, Autodesk.Revit.UI.ExternalEvent externalEvent, AssetExternal handler)
        {
            InitializeComponent();

            // Validate inputs
            if (doc == null)
            {
                TaskDialog.Show("Error", "Document is null. Cannot initialize UI.");
                this.Close();
                return;
            }

            this.doc = doc;
            this.uidoc = uidoc;
            this.externalEvent = externalEvent;
            this.handler = handler;
            this.allRooms = new List<RoomInfo>(); // Initialize empty list
            LoadCategories();
            LoadGroupParameters();




            // Subscribe to events
            if (LV_Room != null)
                LV_Room.SelectionChanged += LV_Room_SelectionChanged;

            // Load rooms
            info();
        }

        // Fill Room list - now includes linked model rooms

        private void LoadCategories()
        {
            var catlist = Extension.DataLab.GetCategories(doc);
            if (catlist == null) return;

            CategoryItems.Clear();
            foreach (var cat in catlist.OrderBy(c => c.Name)) // optional sort
            {
                CategoryItems.Add(new CategoryItem(cat));
                LB_Category.Items.Add(cat);
            }
        }

        public class CategoryItem : INotifyPropertyChanged
        {
            private bool _isChecked;

            public CategoryItem(Category category)
            {
                Category = category;
            }

            public Category Category { get; }

            public string Name => Category?.Name;

            public bool IsChecked
            {
                get => _isChecked;
                set
                {
                    if (_isChecked == value) return;
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //Lets Load the other Data Types

        public static IList<BuiltInParameterGroup> GetParameterGroups()
        {
            return Enum.GetValues(typeof(BuiltInParameterGroup))
                       .Cast<BuiltInParameterGroup>()
                       .Where(g => g != BuiltInParameterGroup.INVALID)
                       .ToList();
        }

        public class ParameterGroupItem
        {
            public BuiltInParameterGroup Group { get; }
            public string Name { get; }

            public ParameterGroupItem(BuiltInParameterGroup group)
            {
                Group = group;
                Name = LabelUtils.GetLabelFor(group);  // "Dimensions", "Electrical", etc.
            }

            public override string ToString() => Name;   // optional
        }

        public class DataTypeItem
        {
            public ForgeTypeId Id { get; }
            public string Name { get; }

            public DataTypeItem(ForgeTypeId id)
            {
                Id = id;
                Name = LabelUtils.GetLabelForSpec(id); // "Length", "Text", etc.
            }

            public override string ToString() => Name;
        }

        public static IList<DataTypeItem> GetDataTypes()
        {
            // Only the common “shared parameter” style data types
            var ids = new ForgeTypeId[]
            {
                SpecTypeId.String.Text,           // Text
                SpecTypeId.Int.Integer,           // Integer
                SpecTypeId.Number,                // Number
                SpecTypeId.Length,                // Length
                SpecTypeId.Area,                  // Area
                SpecTypeId.Volume,                // Volume
                SpecTypeId.Angle,                 // Angle
                SpecTypeId.Slope,                 // Slope
                SpecTypeId.Currency,              // Currency
                SpecTypeId.Boolean.YesNo,         // Yes/No
                SpecTypeId.String.MultilineText  // Multiline Text
            };

            return ids
                .Select(id => new DataTypeItem(id))
                .OrderBy(x => x.Name)
                .ToList();
        }



        public void LoadGroupParameters()
        {
            var groups = GetParameterGroups();

            var items = groups
                .Select(g => new ParameterGroupItem(g))
                .OrderBy(x => x.Name)
                .ToList();

            CB_GroupParam.ItemsSource = items;
            CB_GroupParam.DisplayMemberPath = "Name";   // what user sees
            CB_GroupParam.SelectedValuePath = "Group";  // actual BuiltInParameterGroup

            CB_DataType.ItemsSource = GetDataTypes();
            CB_DataType.DisplayMemberPath = "Name"; // what user sees
            CB_DataType.SelectedValuePath = "Id";
        }


        public void info()
        {
            try
            {
                


                if (doc == null)
                {
                    LB_Status.Text = "Error: Document is null";
                    return;
                }

                // Initialize allRooms if null
                if (allRooms == null)
                    allRooms = new List<RoomInfo>();

                // Get rooms based on checkbox state (default is FALSE - host only)
                bool includeLinked = CB_IncludeLinkedModels?.IsChecked ?? false;

                LB_Status.Text = includeLinked ? "Loading rooms from host and linked models..." : "Loading rooms from host model...";

                allRooms = Extension.DataLab.GetAllRooms(doc, includeLinked);

                if (allRooms == null)
                {
                    allRooms = new List<RoomInfo>();
                    LV_Room.ItemsSource = null;
                    LB_Status.Text = "Failed to load rooms";
                    return;
                }

                if (allRooms.Count == 0)
                {
                    LV_Room.ItemsSource = null;

                    string message = includeLinked
                        ? "No rooms found in host model or linked models.\n\n" +
                          "Please check:\n" +
                          "1. Rooms are placed in your model\n" +
                          "2. Linked models are loaded (not unloaded)\n" +
                          "3. Rooms have area greater than 0"
                        : "No rooms found in host model.\n\n" +
                          "Options:\n" +
                          "1. Place rooms in your model\n" +
                          "2. Check 'Include Linked Models' if rooms are in linked files";

                    LB_Status.Text = "No rooms found";
                    TaskDialog.Show("No Rooms Found", message);
                    return;
                }

                LV_Room.ItemsSource = allRooms;

                // Update status bar
                int hostCount = allRooms.Count(r => r != null && !r.IsLinked);
                int linkedCount = allRooms.Count(r => r != null && r.IsLinked);

                if (includeLinked)
                {
                    if (linkedCount > 0)
                    {
                        LB_Status.Text = $"✓ Loaded {hostCount} host rooms + {linkedCount} linked rooms = {allRooms.Count} total";
                    }
                    else
                    {
                        LB_Status.Text = $"✓ Loaded {hostCount} host rooms (no linked rooms found)";
                    }
                }
                else
                {
                    LB_Status.Text = $"✓ Loaded {hostCount} host rooms only";
                }
            }
            catch (Exception ex)
            {
                if (allRooms == null)
                    allRooms = new List<RoomInfo>();

                string errorMsg = $"Failed to load rooms:\n\n{ex.Message}";

                if (ex.InnerException != null)
                    errorMsg += $"\n\nDetails: {ex.InnerException.Message}";

                errorMsg += "\n\nTip: Uncheck 'Include Linked Models' to load only host model rooms.";

                TaskDialog.Show("Error Loading Rooms", errorMsg);
                LB_Status.Text = "Error loading rooms";
                LV_Room.ItemsSource = null;
            }
        }

        // Button for Assign_ABS 
        private void Assign_ABS(object sender, RoutedEventArgs e)
        {
            var selectedRoomInfo = LV_Room.SelectedItem as RoomInfo;
            if (selectedRoomInfo == null)
            {
                TaskDialog.Show("Assign ABS", "Please select a room first.");
                return;
            }

            // Validation: Check if room is from linked model
            if (selectedRoomInfo.IsLinked)
            {
                var result = TaskDialog.Show("Linked Room Warning",
                    $"The selected room is from a linked model:\n{selectedRoomInfo.LinkName}\n\n" +
                    "Elements can only be modified in the host model.\n" +
                    "The ABS parameters will be assigned to host model elements\n" +
                    "that are located within this room's boundaries.\n\n" +
                    "Do you want to continue?",
                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                if (result == TaskDialogResult.No)
                    return;
            }

            var elementsInRoom = Extension.DataLab.GetElementsInRoom(doc, selectedRoomInfo.Room);

            if (elementsInRoom == null || !elementsInRoom.Any())
            {
                TaskDialog.Show("Assign ABS", "No elements found in the selected room.");
                return;
            }

            // Validate input fields
            if (string.IsNullOrWhiteSpace(TB_Asset.Text))
            {
                TaskDialog.Show("Validation", "Please enter an Asset Code.");
                return;
            }

            if (string.IsNullOrWhiteSpace(TB_Level.Text))
            {
                TaskDialog.Show("Validation", "Please enter an Asset Level.");
                return;
            }

            // Pass data from UI to handler
            handler.AssetValue = TB_Asset.Text.Trim();
            handler.AssetLevel = TB_Level.Text.Trim();
            handler.AssetRoom = selectedRoomInfo.RoomNumber;
            handler.TargetElementIds = elementsInRoom.Select(el => el.Id).ToList();

            // Update status
            LB_Status.Text = $"Processing {elementsInRoom.Count} elements...";

            // Ask Revit to execute the external event
            externalEvent.Raise();
        }

        // Button for Highlight_Model
        private void Highlight_Model(object sender, RoutedEventArgs e)
        {
            var selectedRoomInfo = LV_Room.SelectedItem as RoomInfo;
            if (selectedRoomInfo == null)
            {
                TaskDialog.Show("Highlight", "Please select a room first.");
                return;
            }

            var elementsInRoom = Extension.DataLab.GetElementsInRoom(doc, selectedRoomInfo.Room);

            if (elementsInRoom == null || !elementsInRoom.Any())
            {
                TaskDialog.Show("Highlight", "No elements found in the selected room.");
                return;
            }

            try
            {
                // Collect all element IDs at once
                var elementIds = elementsInRoom.Select(el => el.Id).ToList();

                // Set selection to all elements
                uidoc.Selection.SetElementIds(elementIds);

                // Show all elements (zoom to fit)
                uidoc.ShowElements(elementIds);

                // Isolate all elements temporarily in the active view
                uidoc.ActiveView.IsolateElementsTemporary(elementIds);

                // Update status
                string source = selectedRoomInfo.IsLinked ? $"Linked: {selectedRoomInfo.LinkName}" : "Host Model";
                LB_Status.Text = $"Highlighted {elementIds.Count} elements from {source}";
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to highlight elements: {ex.Message}");
            }
        }

        // Button to validate room number
        private void ValidateRoom_Click(object sender, RoutedEventArgs e)
        {
            string roomNumberToValidate = TB_RoomNumberValidation.Text?.Trim();

            if (string.IsNullOrWhiteSpace(roomNumberToValidate))
            {
                ValidationResultPanel.Visibility = System.Windows.Visibility.Collapsed;
                LB_ValidationResult.Text = "⚠ Please enter a room number to validate";
                LB_ValidationDetails.Text = "";
                return;
            }

            try
            {
                // Search based on current checkbox state (default is host only)
                bool searchLinked = CB_IncludeLinkedModels?.IsChecked ?? false;
                var roomInfo = Extension.DataLab.FindRoomByNumber(doc, roomNumberToValidate, searchLinked);

                ValidationResultPanel.Visibility = System.Windows.Visibility.Collapsed;

                if (roomInfo != null)
                {
                    LB_ValidationResult.Text = "✓ Room Found!";
                    LB_ValidationResult.Foreground = System.Windows.Media.Brushes.DarkGreen;

                    string source = roomInfo.IsLinked ? $"Linked Model: {roomInfo.LinkName}" : "Host Model";
                    LB_ValidationDetails.Text = $"Room Number: {roomInfo.RoomNumber}\n" +
                                                $"Room Name: {roomInfo.RoomName}\n" +
                                                $"Source: {source}";
                    LB_ValidationDetails.Foreground = System.Windows.Media.Brushes.DarkGreen;

                    // Select the room in the list
                    LV_Room.SelectedItem = roomInfo;
                    LV_Room.ScrollIntoView(roomInfo);
                }
                else
                {
                    LB_ValidationResult.Text = "✗ Room Not Found";
                    LB_ValidationResult.Foreground = System.Windows.Media.Brushes.DarkRed;

                    string searchScope = searchLinked ? "host and linked models" : "host model";
                    string suggestion = searchLinked ? "" : "\n\nTip: Check 'Include Linked Models' if room is in a linked file.";

                    LB_ValidationDetails.Text = $"Room number '{roomNumberToValidate}' was not found in {searchScope}.{suggestion}";
                    LB_ValidationDetails.Foreground = System.Windows.Media.Brushes.DarkRed;
                }
            }
            catch (Exception ex)
            {
                ValidationResultPanel.Visibility = System.Windows.Visibility.Collapsed;
                LB_ValidationResult.Text = "✗ Validation Error";
                LB_ValidationResult.Foreground = System.Windows.Media.Brushes.DarkRed;
                LB_ValidationDetails.Text = ex.Message;
                LB_ValidationDetails.Foreground = System.Windows.Media.Brushes.DarkRed;
            }
        }

        // Room selection changed event
        private void LV_Room_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedRoomInfo = LV_Room.SelectedItem as RoomInfo;
            if (selectedRoomInfo != null)
            {
                try
                {
                    var elementsInRoom = Extension.DataLab.GetElementsInRoom(doc, selectedRoomInfo.Room);
                    LV_Element.ItemsSource = elementsInRoom;

                    // Update element count
                    LB_ElementCount.Text = $"{elementsInRoom.Count} elements";

                    // Update room source label
                    string sourceText = selectedRoomInfo.IsLinked
                        ? $"📎 Linked Room from: {selectedRoomInfo.LinkName}"
                        : "🏠 Host Model Room";

                    LB_RoomSource.Text = $"{sourceText}\n" +
                                        $"Room: {selectedRoomInfo.RoomNumber} - {selectedRoomInfo.RoomName}\n" +
                                        $"Elements: {elementsInRoom.Count}";

                    // Auto-populate room number in validation field
                    TB_RoomNumberValidation.Text = selectedRoomInfo.RoomNumber;
                }
                catch (Exception ex)
                {
                    LB_RoomSource.Text = $"Error loading room elements: {ex.Message}";
                    LB_ElementCount.Text = "0 elements";
                }
            }
            else
            {
                LV_Element.ItemsSource = null;
                LB_ElementCount.Text = "0 elements";
                LB_RoomSource.Text = "Select a room to view details";
            }
        }

        // Checkbox to include/exclude linked models
        private void CB_IncludeLinkedModels_Changed(object sender, RoutedEventArgs e)
        {
            // Clear validation panel when toggling
            if (ValidationResultPanel != null)
                ValidationResultPanel.Visibility = System.Windows.Visibility.Collapsed;

            // Show user what's happening
            bool includeLinked = CB_IncludeLinkedModels?.IsChecked ?? false;

            if (includeLinked)
            {
                LB_Status.Text = "Switching to include linked models...";
            }
            else
            {
                LB_Status.Text = "Switching to host model only...";
            }

            // Reload room list
            info();
        }

        // Add this new method to show available room sources
        private void ShowRoomSources_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var hostRoomCount = Extension.DataLab.GetRooms(doc).Count;
                var linkedRooms = Extension.DataLab.GetAllRooms(doc, true)
                    .Where(r => r.IsLinked)
                    .ToList();

                var linkGroups = linkedRooms.GroupBy(r => r.LinkName)
                    .Select(g => new { LinkName = g.Key, Count = g.Count() })
                    .ToList();

                string message = $"Room Sources in Project:\n\n";
                message += $"📁 Host Model: {hostRoomCount} rooms\n\n";

                if (linkGroups.Any())
                {
                    message += "🔗 Linked Models:\n";
                    foreach (var link in linkGroups)
                    {
                        message += $"   • {link.LinkName}: {link.Count} rooms\n";
                    }
                }
                else
                {
                    message += "🔗 No linked models with rooms found\n";
                }

                message += $"\n📊 Total: {hostRoomCount + linkedRooms.Count} rooms";

                TaskDialog.Show("Room Sources", message);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to analyze room sources:\n{ex.Message}");
            }
        }

        // Search/Filter room list
        private void TB_SearchRoom_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (allRooms == null || allRooms.Count == 0)
                {
                    LV_Room.ItemsSource = null;
                    return;
                }

                string searchText = TB_SearchRoom?.Text?.ToLower() ?? "";

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    LV_Room.ItemsSource = allRooms;
                    LB_Status.Text = $"Showing all {allRooms.Count} rooms";
                }
                else
                {
                    var filtered = allRooms.Where(r =>
                        r != null && (
                            (!string.IsNullOrEmpty(r.RoomNumber) && r.RoomNumber.ToLower().Contains(searchText)) ||
                            (!string.IsNullOrEmpty(r.RoomName) && r.RoomName.ToLower().Contains(searchText)) ||
                            (!string.IsNullOrEmpty(r.LinkName) && r.LinkName.ToLower().Contains(searchText))
                        )
                    ).ToList();

                    LV_Room.ItemsSource = filtered;
                    LB_Status.Text = $"Found {filtered.Count} of {allRooms.Count} rooms matching '{searchText}'";
                }
            }
            catch (Exception ex)
            {
                LB_Status.Text = $"Search error: {ex.Message}";
            }
        }

        //This Will use for Create the Shared Parameter

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            TaskDialog.Show("Message", "Hello User Parameter Binding still in Process Just to Automate the ABS Parameter USE AS PER YOUTUBE SHARED PARAMETER FILE : YOUTUBE:-https://www.youtube.com/@BIMDigitalDesign");

        }
    }
}