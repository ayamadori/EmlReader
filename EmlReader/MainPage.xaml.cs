using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
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
        public MainPage()
        {
            this.InitializeComponent();

            // https://docs.microsoft.com/en-us/windows/uwp/monetize/launch-feedback-hub-from-your-app
            if (Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.IsSupported())
            {
                this.FeedbackButton.Visibility = Visibility.Visible;
            }
        }

        private async void RateButton_Click(object sender, RoutedEventArgs e)
        {
            Uri uriReview = new Uri(@"ms-windows-store://review/?ProductId=9nblggh5k5vp");
            bool success = await Launcher.LaunchUriAsync(uriReview);
        }

        private async void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            //Uri uriFeedback = new Uri($"feedback-hub://?tabid=2&appid={Package.Current.Id.FamilyName}!App");
            //bool success = await Launcher.LaunchUriAsync(uriFeedback);

            //// https://docs.microsoft.com/en-us/windows/uwp/monetize/launch-feedback-hub-from-your-app
            var launcher = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.GetDefault();
            await launcher.LaunchAsync();
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
                    var storageFile = items[0] as StorageFile;
                    string filetype = storageFile.FileType.ToLower();
                    if (filetype.Equals(".eml"))
                    {
                        Frame.Navigate(typeof(MailPage), storageFile);
                    }
                }
            }
        }

        private async void FileOpenButton_Click(object sender, RoutedEventArgs e)
        {
            // FileOpenPicker
            // https://msdn.microsoft.com/ja-jp/library/windows/apps/mt186456.aspx

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            //picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            //picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".eml");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                Frame.Navigate(typeof(MailPage), file);
            }
        }

        private async void AssociationButton_Click(object sender, RoutedEventArgs e)
        {
            // https://msdn.microsoft.com/en-us/library/windows/apps/mt299102.asp

            // FileOpenPicker
            // https://msdn.microsoft.com/ja-jp/library/windows/apps/mt186456.aspx

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".eml");
            picker.FileTypeFilter.Add(".msg");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Set the option to show the picker
                var options = new LauncherOptions();
                options.DisplayApplicationPicker = true;
                options.TargetApplicationPackageFamilyName = "22164ayamadori.EMLReader_rtpjcevvdcnva";

                file = await file.CopyAsync(ApplicationData.Current.TemporaryFolder, file.Name, NameCollisionOption.ReplaceExisting);

                var a = await Launcher.QueryFileSupportAsync(file);
                var b = await Launcher.FindFileHandlersAsync(".eml");
                string res = "FindFileHandlersAsync: ";
                foreach(AppInfo info in b)
                {
                    res += info.PackageFamilyName + "; ";
                }
                Debug.WriteLine(res);
                var c = await Launcher.FindFileHandlersAsync(".msg");
                string res2 = "FindFileHandlersAsync: ";
                foreach (AppInfo info in c)
                {
                    res2 += info.PackageFamilyName + "; ";
                }
                Debug.WriteLine(res2);

                // Launch the retrieved file
                bool success = await Launcher.LaunchFileAsync(file, options);
                if (success)
                {
                    // File launched
                    await new MessageDialog("File launched").ShowAsync();
                }
                else
                {
                    // File launch failed
                    await new MessageDialog("File launch failed").ShowAsync();
                }
            }
        }
    }
}
