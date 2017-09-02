using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The data model defined by this file serves as a representative example of a strongly-typed
// model.  The property names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app 
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app 
// is first launched.

namespace EcommerceJewel.Data
{
    public enum JewelryType
    {
        Necklace,
        Ring
    }

    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class JewelryDataItem
    {
        private static Uri _baseUri = new Uri("ms-appx:///");
        public JewelryDataItem(String uniqueId, String title, String subtitle, String imagePath, String description, String content)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Content = content;
        }

        public JewelryDataItem(String uniqueId, String title, String subtitle, String imagePath, String description, String content, string colSpan, string rowSpan, JewelryDataGroup group)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Content = content;
            this.ColSpan = Convert.ToInt32(colSpan);
            this.RowSpan = Convert.ToInt32(rowSpan);
            this.Group = group;
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public string Content { get; private set; }
        public int ColSpan { get; private set; }
        public int RowSpan { get; private set; }
        public JewelryDataGroup Group { get; private set; }

        private ImageSource _image = null;
        public ImageSource Image
        {
            get
            {
                if (this._image == null && this.ImagePath != null)
                {
                    this._image = new BitmapImage(new Uri(_baseUri, this.ImagePath));
                }
                return this._image;
            }
            set
            {
                this._image = value;
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class JewelryDataGroup : JewelryDataItem
    {
        public JewelryDataGroup(String uniqueId, string jewelryType, String title, String subtitle, String imagePath, String description) : base(uniqueId,  title, subtitle, imagePath, description, string.Empty)
        {
            this.Type = (JewelryType)Enum.Parse(typeof(JewelryType), jewelryType);
            this.Items = new ObservableCollection<JewelryDataItem>();
            this.TopItems = new ObservableCollection<JewelryDataItem>();
            Items.CollectionChanged += ItemsCollectionChanged;
        }

        public ObservableCollection<JewelryDataItem> Items { get; private set; }
        public ObservableCollection<JewelryDataItem> TopItems { get; private set; }
        public JewelryType Type { get; set; }

        private void ItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Provides a subset of the full items collection to bind to from a GroupedItemsPage
            // for two reasons: GridView will not virtualize large items collections, and it
            // improves the user experience when browsing through groups with large numbers of
            // items.
            //
            // A maximum of 12 items are displayed because it results in filled grid columns
            // whether there are 1, 2, 3, 4, or 6 rows displayed

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex < 12)
                    {
                        TopItems.Insert(e.NewStartingIndex, Items[e.NewStartingIndex]);
                        if (TopItems.Count > 12)
                        {
                            TopItems.RemoveAt(12);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex < 12 && e.NewStartingIndex < 12)
                    {
                        TopItems.Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    else if (e.OldStartingIndex < 12)
                    {
                        TopItems.RemoveAt(e.OldStartingIndex);
                        TopItems.Add(Items[11]);
                    }
                    else if (e.NewStartingIndex < 12)
                    {
                        TopItems.Insert(e.NewStartingIndex, Items[e.NewStartingIndex]);
                        TopItems.RemoveAt(12);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex < 12)
                    {
                        TopItems.RemoveAt(e.OldStartingIndex);
                        if (Items.Count >= 12)
                        {
                            TopItems.Add(Items[11]);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex < 12)
                    {
                        TopItems[e.OldStartingIndex] = Items[e.OldStartingIndex];
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    TopItems.Clear();
                    while (TopItems.Count < Items.Count && TopItems.Count < 12)
                    {
                        TopItems.Add(Items[TopItems.Count]);
                    }
                    break;
            }
        }

    }

    public class CartItem
    {
        public string UniqueId { get; set; }
        public string Title { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
    }

    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// JewelryDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public sealed class JewelryDataSource
    {
        public static ObservableCollection<CartItem> MyCartItems = new ObservableCollection<CartItem>();

        private static JewelryDataSource _jewelryDataSource = new JewelryDataSource();

        private ObservableCollection<JewelryDataGroup> _groups = new ObservableCollection<JewelryDataGroup>();
        public ObservableCollection<JewelryDataGroup> Groups
        {
            get { return this._groups; }
        }

        public static async Task<IEnumerable<JewelryDataGroup>> GetGroupsAsync()
        {
            await _jewelryDataSource.GetDataAsync();

            return _jewelryDataSource.Groups;
        }

        public static async Task<IEnumerable<JewelryDataGroup>> GetGroupAsync(string uniqueId)
        {
            await _jewelryDataSource.GetDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _jewelryDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches;
            return null;
        }

        public static async Task<IEnumerable<JewelryDataGroup>> GetGroupByTypeAsync(JewelryType jewelryType)
        {
            await _jewelryDataSource.GetDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _jewelryDataSource.Groups.Where((group) => group.Type.Equals(jewelryType));
            if (matches.Count() > 0) return matches;
            return null;
        }

        public static async Task<JewelryDataItem> GetItemAsync(string uniqueId)
        {
            await _jewelryDataSource.GetDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _jewelryDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        private async Task GetDataAsync()
        {
            if (this._groups.Count != 0)
                return;

            Uri dataUri = new Uri("ms-appx:///DataModel/JewelryData.json");

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            string jsonText = await FileIO.ReadTextAsync(file);
            JsonObject jsonObject = JsonObject.Parse(jsonText);
            JsonArray jsonArray = jsonObject["Groups"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();
                JewelryDataGroup group = new JewelryDataGroup(groupObject["UniqueId"].GetString(),
                                                            groupObject["Type"].GetString(),
                                                            groupObject["Title"].GetString(),
                                                            groupObject["Subtitle"].GetString(),
                                                            groupObject["ImagePath"].GetString(),
                                                            groupObject["Description"].GetString());

                foreach (JsonValue itemValue in groupObject["Items"].GetArray())
                {
                    JsonObject itemObject = itemValue.GetObject();
                    group.Items.Add(new JewelryDataItem(itemObject["UniqueId"].GetString(),
                                                       itemObject["Title"].GetString(),
                                                       itemObject["Subtitle"].GetString(),
                                                       itemObject["ImagePath"].GetString(),
                                                       itemObject["Description"].GetString(),
                                                       itemObject["Content"].GetString(),
                                                       itemObject["ColSpan"].GetString(),
                                                       itemObject["RowSpan"].GetString(),
                                                       group
                                                       ));
                }
                this.Groups.Add(group);
            }
        }
    }
}