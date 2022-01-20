using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Services.Store;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace EmlReader
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        AccessListEntryView mruView;
        public MainPage()
        {
            this.InitializeComponent();
            mruView = StorageApplicationPermissions.MostRecentlyUsedList.Entries;
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        StorageFile storageFile = items[i] as StorageFile;
                        string filetype = storageFile.FileType.ToLower();
                        if (filetype.Equals(".eml"))
                        {
                            if (i == 0)
                                Frame.Navigate(typeof(MailPage), storageFile);
                            else
                            {
                                // Open file on this app
                                var options = new LauncherOptions();
                                options.TargetApplicationPackageFamilyName = Package.Current.Id.FamilyName;
                                await Launcher.LaunchFileAsync(storageFile, options);
                            }
                        }
                    }
                }
            }
        }

        private async void FileOpenButton_Click(object sender, RoutedEventArgs e)
        {
            // FileOpenPicker
            // https://msdn.microsoft.com/ja-jp/library/windows/apps/mt186456.aspx

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".eml");

            var items = await picker.PickMultipleFilesAsync();
            if (items.Count > 0)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    StorageFile storageFile = items[i] as StorageFile;

                    if (i == 0)
                        Frame.Navigate(typeof(MailPage), storageFile);
                    else
                    {
                        // Open file on this app
                        var options = new LauncherOptions();
                        options.TargetApplicationPackageFamilyName = Package.Current.Id.FamilyName;
                        await Launcher.LaunchFileAsync(storageFile, options);
                    }
                }
            }
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            // https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.controls.contentdialog.aspx
            var dlg = new AboutDialog();
            await dlg.ShowAsync();
        }

        private async void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            StoreContext storeContext = StoreContext.GetDefault();
            string StoreId = "9PNWXP9VHK05";
            StorePurchaseResult result = await storeContext.RequestPurchaseAsync(StoreId);
            if (result.ExtendedError != null)
            {
                Debug.WriteLine(result.ExtendedError);
                return;
            }

            switch (result.Status)
            {
                case StorePurchaseStatus.AlreadyPurchased: // should never get this for a managed consumable since they are stackable
                    Debug.WriteLine("You already bought this consumable.");
                    break;

                case StorePurchaseStatus.Succeeded:
                    Debug.WriteLine("You bought.");
                    break;

                case StorePurchaseStatus.NotPurchased:
                    Debug.WriteLine("Product was not purchased, it may have been canceled.");
                    break;

                case StorePurchaseStatus.NetworkError:
                    Debug.WriteLine("Product was not purchased due to a network error.");
                    break;

                case StorePurchaseStatus.ServerError:
                    Debug.WriteLine("Product was not purchased due to a server error.");
                    break;

                default:
                    Debug.WriteLine("Product was not purchased due to an unknown error.");
                    break;
            }
        }

        private async void MruList_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if ((sender as ListView).SelectedItem == null) return;
            AccessListEntry entry = (AccessListEntry)(sender as ListView).SelectedItem;
            string token = entry.Token;
            try
            {
                StorageFile file = await StorageApplicationPermissions.MostRecentlyUsedList.GetItemAsync(token) as StorageFile;

                if (file != null)
                {
                    Frame rootFrame = Window.Current.Content as Frame;
                    rootFrame.Navigate(typeof(MailPage), file);

                    // Add to MRU
                    // https://docs.microsoft.com/en-us/windows/uwp/files/how-to-track-recently-used-files-and-folders
                    var mru = StorageApplicationPermissions.MostRecentlyUsedList;
                    string mruToken = mru.Add(file, file.Name);
                    MruList.ItemsSource = null;
                    mruView = StorageApplicationPermissions.MostRecentlyUsedList.Entries;
                    MruList.ItemsSource = mruView;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                ContentDialog dlg = new ContentDialog() { Title = $"\"{entry.Metadata}\" is not available", Content = "Selected file may be moved/renamed/deleted.", CloseButtonText = "OK" };
                _ = dlg.ShowAsync();

                // Remove from MRU
                // https://docs.microsoft.com/en-us/windows/uwp/files/how-to-track-recently-used-files-and-folders
                var mru = StorageApplicationPermissions.MostRecentlyUsedList;
                mru.Remove(token);
                MruList.ItemsSource = null;
                mruView = StorageApplicationPermissions.MostRecentlyUsedList.Entries;
                MruList.ItemsSource = mruView;
            }
        }

        private async void OpenWindowButton_Click(object sender, RoutedEventArgs e)
        {
            // https://stackoverflow.com/questions/34445579/how-to-get-listview-item-content-on-righttapped-event-of-an-universal-windows-ap
            AccessListEntry entry = (AccessListEntry)((FrameworkElement)e.OriginalSource).DataContext;
            string token = entry.Token;
            try
            {
                StorageFile file = await StorageApplicationPermissions.MostRecentlyUsedList.GetItemAsync(token) as StorageFile;

                if (file != null)
                {
                    // Open file on this app
                    var options = new LauncherOptions();
                    options.TargetApplicationPackageFamilyName = Package.Current.Id.FamilyName;
                    await Launcher.LaunchFileAsync(file, options);

                    // Add to MRU
                    // https://docs.microsoft.com/en-us/windows/uwp/files/how-to-track-recently-used-files-and-folders
                    var mru = StorageApplicationPermissions.MostRecentlyUsedList;
                    string mruToken = mru.Add(file, file.Name);
                    MruList.ItemsSource = null;
                    mruView = StorageApplicationPermissions.MostRecentlyUsedList.Entries;
                    MruList.ItemsSource = mruView;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                ContentDialog dlg = new ContentDialog() { Title = $"\"{entry.Metadata}\" is not available", Content = "Selected file may be moved/renamed/deleted.", CloseButtonText = "OK" };
                _ = dlg.ShowAsync();

                // Remove from MRU
                // https://docs.microsoft.com/en-us/windows/uwp/files/how-to-track-recently-used-files-and-folders
                var mru = StorageApplicationPermissions.MostRecentlyUsedList;
                mru.Remove(token);
                MruList.ItemsSource = null;
                mruView = StorageApplicationPermissions.MostRecentlyUsedList.Entries;
                MruList.ItemsSource = mruView;
            }

        }
    }
}
