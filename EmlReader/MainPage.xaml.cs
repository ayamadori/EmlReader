using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// �󔒃y�[�W�̃A�C�e�� �e���v���[�g�ɂ��ẮAhttp://go.microsoft.com/fwlink/?LinkId=234238 ���Q�Ƃ��Ă�������

namespace EmlReader
{
    /// <summary>
    /// ���ꎩ�̂Ŏg�p�ł���󔒃y�[�W�܂��̓t���[�����Ɉړ��ł���󔒃y�[�W�B
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
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
                        if (filetype.Equals(".eml") || filetype.Equals(".msg"))
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
            picker.FileTypeFilter.Add(".msg");

            var files = await picker.PickMultipleFilesAsync();
            if (files != null)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    StorageFile storageFile = files[i] as StorageFile;

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

}
}
