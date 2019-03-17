using Microsoft.Toolkit.Uwp.Helpers;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 を参照してください

namespace EmlReader
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MailPage : Page
    {
        MimeMessage message;
        PrintHelper printHelper;
        StorageFile file;

        public MailPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Load the message
            file = e.Parameter as StorageFile;
            using (var _stream = await file.OpenReadAsync())
            {
                try
                {
                    Message = MimeMessage.Load(_stream.AsStreamForRead());
                }
                catch
                {
                    var dlg = new MessageDialog("The app can NOT open this file.", "Unsupported file");
                    await dlg.ShowAsync();

                    // Exit app
                    Windows.UI.Xaml.Application.Current.Exit();
                }
            }
        }

        public MimeMessage Message
        {
            get { return message; }
            set
            {
                message = value ?? throw new ArgumentNullException(nameof(value));

                var from = message.From.First() as MailboxAddress;
                if (string.IsNullOrEmpty(from.Name))
                {
                    fromPersonPicture.DisplayName = from.Address;
                    fromTextBlock.Text = from.Address;
                }
                else
                {
                    fromPersonPicture.DisplayName = from.Name;
                    fromTextBlock.Text = from.Name + " <" + from.Address + ">";
                }

                dateTextBlock.Text = message.Date.ToString(CultureInfo.CurrentCulture);

                subjectTextBlock.Text = message.Subject;

                InternetAddressList addressList = new InternetAddressList();
                AddressBlock.Text = "";

                // Set Address
                // http://blog.okazuki.jp/entry/2016/02/25/232252
                if (message.To.Count > 0)
                {
                    AddressBlock.Inlines.Add(new Run { Text = "TO: ", Foreground = new SolidColorBrush(Colors.Black) });
                    SetAddressList(message.To);
                }
                if (message.Cc.Count > 0)
                {
                    AddressBlock.Inlines.Add(new Run { Text = "CC: ", Foreground = new SolidColorBrush(Colors.Black) });
                    SetAddressList(message.Cc);
                }

                if (message.Attachments.Count() > 0)
                    AttachmentView.ItemsSource = message.Attachments;
                else
                    AttachmentView.Visibility = Visibility.Collapsed;

                // render the message body
                Render();
            }
        }

        void Render()
        {
            var visitor = new HtmlPreviewVisitor(webView);

            message.Accept(visitor);
        }

        // Hook navigation
        private async void webView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            Uri uri = args.Uri;
            if (uri != null && uri.AbsoluteUri.StartsWith("http"))
            {
                // Cancel navigation
                args.Cancel = true;
                // Launch browser
                bool success = await Launcher.LaunchUriAsync(uri);
            }
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
                        using (var _stream = await storageFile.OpenReadAsync())
                        {
                            Message = MimeMessage.Load(_stream.AsStreamForRead());
                        }
                    }
                }
            }
        }

        private void SetAddressList(InternetAddressList list)
        {
            foreach (InternetAddress item in list)
            {
                if (item is MailboxAddress)
                {
                    var _address = (item as MailboxAddress).Address;
                    var link = new Hyperlink {
                        NavigateUri = new Uri("ms-people:viewcontact?Email=" + _address),
                        UnderlineStyle = UnderlineStyle.None
                    };
                    if (string.IsNullOrEmpty(item.Name))
                        link.Inlines.Add(new Run {
                            Text = _address + "; ",
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 73, 73, 73))
                        });
                    else
                        link.Inlines.Add(new Run {
                            //Text = item.Name + "<" + _address + ">; ",
                            Text = item.Name + "; ",
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 73, 73, 73))
                        });
                    AddressBlock.Inlines.Add(link);
                }
                else
                {
                    AddressBlock.Inlines.Add(new Run {
                        Text = item.Name + "; ",
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 73, 73, 73))
                    });
                }
            }
        }

        private async void AddressTemplate_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);

            if ((sender as FrameworkElement).DataContext is GroupAddress) return;
            var _address = ((MailboxAddress) (sender as FrameworkElement).DataContext).Address;
            Uri uri = new Uri("ms-people:viewcontact?Email=" + _address);
            // Launch People app
            bool success = await Launcher.LaunchUriAsync(uri);

        }

        private void AttachmentView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OpenFile((MimePart)AttachmentView.SelectedItem);
        }

        private void AttachmentTemplate_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void OpenFlyoutItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OpenFile((MimePart)(sender as FrameworkElement).DataContext);
        }

        private async void OpenFile(MimePart item)
        {
            // https://social.msdn.microsoft.com/Forums/it-IT/2407812c-2b59-4a7c-972a-778c32b91220/sharing-an-inmemory-png-file
            var file = await StorageFile.CreateStreamedFileAsync(item.FileName, async (fileStream) =>
            {
                item.Content.DecodeTo(fileStream.AsStreamForWrite());
                await fileStream.FlushAsync();
                fileStream.Dispose();
            }, null);
            if (file != null)
            {
                // https://github.com/Microsoft/Windows-universal-samples/tree/master/Samples/AssociationLaunching
                //Can't launch files directly from install folder so copy it over 
                //to temporary folder first
                // ** Unnessesary for PC, but needed for Mobile
                file = await file.CopyAsync(ApplicationData.Current.TemporaryFolder, item.FileName, NameCollisionOption.ReplaceExisting);

                // Launch the retrieved file
                var success = await Launcher.LaunchFileAsync(file);
            }
        }

        private async void SaveFlyoutItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MimePart _item = (MimePart)(sender as FrameworkElement).DataContext;

            // https://msdn.microsoft.com/ja-jp/library/windows/apps/mt186455.aspx
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
            // Dropdown of file types the user can save the file as
            // http://stackoverflow.com/questions/17070908/how-can-i-accept-any-file-type-in-filesavepicker
            // The file is NOT saved in case of "All files"
            // savePicker.FileTypeChoices.Add("All files", new List<string>() { "." });
            string _extension = _item.FileName.Substring(_item.FileName.LastIndexOf("."));
            savePicker.FileTypeChoices.Add(_extension.ToUpper() + " File", new List<string>() { _extension });
            // Default file name if the user does not type one in or select a file to replace
            // The filename must NOT include extention
            savePicker.SuggestedFileName = _item.FileName.Remove(_item.FileName.LastIndexOf("."));

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until
                // we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                using (Stream _stream = await file.OpenStreamForWriteAsync())
                {
                    _item.Content.DecodeTo(_stream);
                }
                // Let Windows know that we're finished changing the file so
                // the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                Windows.Storage.Provider.FileUpdateStatus status =
                    await CachedFileManager.CompleteUpdatesAsync(file);
                if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                {
                    //this.textBlock.Text = "File " + file.Name + " was saved.";
                }
                else
                {
                    //this.textBlock.Text = "File " + file.Name + " couldn't be saved.";
                    var dlg = new MessageDialog("File " + file.Name + " couldn't be saved.");
                    await dlg.ShowAsync();
                }
            }
        }

        private async void FromView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //From.Text = (message.From.First() as MailboxAddress).Address;
            //FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);

            string _address = (message.From.First() as MailboxAddress).Address;
            Uri uri = new Uri("ms-people:viewcontact?Email=" + _address);
            // Launch People app
            bool success = await Launcher.LaunchUriAsync(uri);
        }

        private async void ReplyButton_Click(object sender, RoutedEventArgs e)
        {
            // https://ja.wikipedia.org/wiki/Mailto
            string _scheme = "mailto:";
            if (message.ReplyTo.Count() != 0)
                _scheme += (message.ReplyTo.First() as MailboxAddress).Address;
            else
                _scheme += (message.From.First() as MailboxAddress).Address;

            _scheme += "?Subject=" + WebUtility.UrlEncode("RE: " + message.Subject).Replace("+", "%20");

            Uri uri = new Uri(_scheme);
            // Launch browser
            bool success = await Launcher.LaunchUriAsync(uri);
        }

        private async void ReplyAllButton_Click(object sender, RoutedEventArgs e)
        {
            // https://ja.wikipedia.org/wiki/Mailto
            string _scheme = "mailto:";
            if (message.ReplyTo.Count() != 0)
                _scheme += (message.ReplyTo.First() as MailboxAddress).Address;
            else
                _scheme += (message.From.First() as MailboxAddress).Address;

            _scheme += "?subject=" + WebUtility.UrlEncode("RE: " + message.Subject).Replace("+", "%20");

            _scheme += "&cc=";
            // TO: in "mailto:" scheme can accept only one address...?
            if (message.To.Count() != 0)
            {
                foreach (InternetAddress item in message.To)
                {
                    if (item is MailboxAddress)
                        _scheme += "," + (item as MailboxAddress).Address + ",";
                }
            }
            if (message.Cc.Count() != 0)
            {
                
                foreach (InternetAddress item in message.Cc)
                {
                    if (item is MailboxAddress)
                        _scheme += (item as MailboxAddress).Address + ",";
                }
            }         

            Uri uri = new Uri(_scheme);
            //uri = new Uri("mailto:a@a.jp?subject=test&cc=c@c.jp,b@b.jp");

            // Launch browser
            bool success = await Launcher.LaunchUriAsync(uri);
        }

        private void HeaderExpandButton_Click(object sender, RoutedEventArgs e)
        {         
            if (HeaderExpandButtonText.Text.Equals("\uE011"))
            {
                // Expand
                HeaderExpandButtonText.Text = "\uE010"; // ScrollChevronUpLegacy
                // http://stackoverflow.com/questions/11385026/setting-height-of-textbox-to-auto-in-code-behind-for-a-dynamically-created-textb
                AddressBlock.Height = double.NaN;
            }
            else
            {
                // Shrink
                HeaderExpandButtonText.Text = "\uE011"; // ScrollChevronDownLegacy
                AddressBlock.Height = HeaderExpandButton.Height;
            }
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a new PrintHelper instance
            // "container" is a XAML panel that will be used to host printable control.
            // It needs to be in your visual tree but can be hidden with Opacity = 0
            //printHelper = new PrintHelper(Container);
            printHelper = new PrintHelper(PrintableContent);

            // Add controls that you want to print
            //printHelper.AddFrameworkElementToPrint(HeaderPanel);
            //printHelper.AddFrameworkElementToPrint(webView);
            //printHelper.AddFrameworkElementToPrint(PrintableContent);

            // Connect to relevant events
            printHelper.OnPrintFailed += PrintHelper_OnPrintFailed;
            printHelper.OnPrintSucceeded += PrintHelper_OnPrintSucceeded;
            printHelper.OnPrintCanceled += PrintHelper_OnPrintCanceled;

            // Start printing process
            await printHelper.ShowPrintUIAsync(file.DisplayName);
        }

        // Event handlers

        private async void PrintHelper_OnPrintSucceeded()
        {
            printHelper.Dispose();
            var dialog = new MessageDialog("Printing done.");
            await dialog.ShowAsync();
        }

        private async void PrintHelper_OnPrintFailed()
        {
            printHelper.Dispose();
            var dialog = new MessageDialog("Printing failed.");
            await dialog.ShowAsync();
        }

        private async void PrintHelper_OnPrintCanceled()
        {
            printHelper.Dispose();
            var dialog = new MessageDialog("Printing canceled.");
            await dialog.ShowAsync();
        }
    }
}

