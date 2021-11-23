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
        AccessListEntryView mru;
        public MainPage()
        {
            this.InitializeComponent();
            mru = StorageApplicationPermissions.MostRecentlyUsedList.Entries;
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

    }
}
