﻿using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Services.Store;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
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
        string filename;

        public MailPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Load the message
            StorageFile file = e.Parameter as StorageFile;
            filename = file.Name.Remove(file.Name.LastIndexOf("."));
            using (var _stream = await file.OpenReadAsync())
            {
                try
                {
                    Message = MimeMessage.Load(_stream.AsStreamForRead());
                    ApplicationView.GetForCurrentView().Title = Message.Subject;
                }
                catch (Exception ex)
                {
                    var dlg = new ContentDialog() { Title = "Unsupported file", Content = "The app can NOT open this file.", CloseButtonText = "OK" };
                    await dlg.ShowAsync();

                    Debug.WriteLine(ex.ToString());

                    // Exit app
                    Windows.UI.Xaml.Application.Current.Exit();
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Reset title
            ApplicationView.GetForCurrentView().Title = "";
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
                    FromPicture.DisplayName = from.Address;
                    FromTextBlock.Text = from.Address;
                }
                else
                {
                    FromPicture.DisplayName = from.Name;
                    FromTextBlock.Text = from.Name + " <" + from.Address + ">";
                }

                // https://stackoverflow.com/questions/479300/detect-os-language-from-c-sharp
                DateTextBlock.Text = message.Date.LocalDateTime.ToString("g");

                SubjectTextBlock.Text = message.Subject;

                AddressBlock.Text = "";

                // Set Address
                // http://blog.okazuki.jp/entry/2016/02/25/232252
                if (message.To.Count > 0)
                {
                    AddressBlock.Inlines.Add(new Run { Text = "To: " });
                    SetAddressList(message.To);
                }
                if (message.Cc.Count > 0)
                {
                    AddressBlock.Inlines.Add(new Run { Text = "\nCc: " });
                    SetAddressList(message.Cc);
                }

                int count = message.Attachments.Count();
                if (count > 0)
                {
                    AttachmentView.ItemsSource = message.Attachments;
                    SaveAllButton.Visibility = (count > 1) ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                    AttachmentArea.Visibility = Visibility.Collapsed;

                // render the message body
                Render();
            }
        }

        void Render()
        {
            var visitor = new HtmlPreviewVisitor(MailView);
            message.Accept(visitor);
        }

        // Hook navigation
        private async void MailView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            string uri = args.Uri;
            if (uri != null && uri.StartsWith("http"))
            {
                // Cancel navigation
                args.Cancel = true;
                // Launch browser
                bool success = await Launcher.LaunchUriAsync(new Uri(uri));
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

        private void SetAddressList(InternetAddressList list)
        {
            foreach (InternetAddress item in list)
            {
                if (item is MailboxAddress)
                {
                    var _address = (item as MailboxAddress).Address;
                    var link = new Hyperlink
                    {
                        UnderlineStyle = UnderlineStyle.None,
                        Foreground = new SolidColorBrush(Colors.Black)
                    };
                    // https://docs.microsoft.com/ja-jp/uwp/api/windows.ui.xaml.documents.hyperlink.click
                    link.Click += (sender, args) =>
                    {
                        OnUserClickShowContactCard(ToArea, item.Name, _address);
                    };

                    if (string.IsNullOrEmpty(item.Name))
                        link.Inlines.Add(new Run { Text = _address + "; " });
                    else
                        link.Inlines.Add(new Run { Text = item.Name + "; " });
                    AddressBlock.Inlines.Add(link);
                }
                else
                {
                    AddressBlock.Inlines.Add(new Run { Text = item.Name + "; " });
                }
            }
        }

        private void FromView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MailboxAddress item = (message.From.First() as MailboxAddress);
            OnUserClickShowContactCard(FromPicture, item.Name, item.Address);
        }

        // https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/contact-card
        // Gets the rectangle of the element 
        public static Rect GetElementRect(FrameworkElement element)
        {
            // Passing "null" means set to root element. 
            GeneralTransform elementTransform = element.TransformToVisual(null);
            Rect rect = elementTransform.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            return rect;
        }

        // https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/contact-card
        // Display a contact in response to an event
        private async void OnUserClickShowContactCard(FrameworkElement element, string name, string address)
        {
            if (ContactManager.IsShowContactCardSupported())
            {
                Rect selectionRect = GetElementRect(element);
                try
                {
                    // https://docs.microsoft.com/en-us/windows/uwp/contacts-and-calendar/integrating-with-contacts
                    ContactStore contactStore = await ContactManager.RequestStoreAsync();
                    IReadOnlyList<Contact> contacts = await contactStore.FindContactsAsync(address);

                    // Retrieve the contact to display
                    Contact contact;
                    if (contacts.Count > 0)
                    {
                        contact = contacts[0];
                    }
                    else
                    {
                        // Create new contact
                        contact = new Contact();
                        var email = new ContactEmail();
                        email.Address = address;
                        contact.Emails.Add(email);
                        if (!string.IsNullOrEmpty(name)) contact.Name = name;
                    }

                    ContactManager.ShowContactCard(contact, selectionRect, Placement.Default);
                }
                catch(Exception e)
                {
                    // Launch People app
                    bool success = await Launcher.LaunchUriAsync(new Uri("ms-people:viewcontact?Email=" + address + "&ContactName=" + name));
                }
            }
            else
            {
                // Launch People app
                bool success = await Launcher.LaunchUriAsync(new Uri("ms-people:viewcontact?Email=" + address + "&ContactName=" + name));
            }
        }

        private void AttachmentTemplate_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MimePart item = (sender as FrameworkElement).DataContext as MimePart;
            if (item != null) OpenFile(item);
        }

        private void AttachmentTemplate_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void OpenFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MimePart item = (sender as FrameworkElement).DataContext as MimePart;
            if (item != null) OpenFile(item);
        }

        private async void OpenFile(MimePart item)
        {
            // https://github.com/Microsoft/Windows-universal-samples/tree/master/Samples/AssociationLaunching
            //Can't launch files directly from install folder so copy it over 
            //to temporary folder first
            // ** Unnessesary for PC, but needed for Mobile
            StorageFolder tempfolder = ApplicationData.Current.TemporaryFolder;
            StorageFile file = await tempfolder.CreateFileAsync(item.FileName, CreationCollisionOption.ReplaceExisting);
            bool success = await MimePart2FileAsync(item, file);
            if (!success)
            {
                var dlg = new ContentDialog() { Content = "File " + file.Name + " couldn't be opened.", CloseButtonText = "OK" };
                await dlg.ShowAsync();
                return;
            }

            // Launch the retrieved file
            success = await Launcher.LaunchFileAsync(file);
        }

        private async void SaveFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MimePart _item = (MimePart)(sender as FrameworkElement).DataContext;

            // https://docs.microsoft.com/en-us/windows/uwp/files/quickstart-save-a-file-with-a-picker
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
                bool success = await MimePart2FileAsync(_item, file);
                if (!success)
                {
                    var dlg = new ContentDialog() { Content = "File " + file.Name + " couldn't be saved.", CloseButtonText = "OK" };
                    await dlg.ShowAsync();
                }
            }
        }

        private async void ReplyButton_Click(object sender, RoutedEventArgs e)
        {
            // https://ja.wikipedia.org/wiki/Mailto
            string _scheme = "mailto:";
            if (message.ReplyTo.Count() != 0)
                _scheme += (message.ReplyTo.First() as MailboxAddress).Address;
            else
                _scheme += (message.From.First() as MailboxAddress).Address;

            _scheme += "?Subject=" + WebUtility.UrlEncode("Re: " + message.Subject).Replace("+", "%20");

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

            _scheme += "?subject=" + WebUtility.UrlEncode("Re: " + message.Subject).Replace("+", "%20").Replace("/", "%2F");

            _scheme += "&cc=";
            // TO: in "mailto:" scheme can accept only one address...?
            if (message.To.Count() != 0)
            {
                foreach (InternetAddress item in message.To)
                {
                    if (item is MailboxAddress)
                        _scheme += (item as MailboxAddress).Address + ",";
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

            // Launch browser
            bool success = await Launcher.LaunchUriAsync(uri);
        }

        private void HeaderExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (HeaderExpandButtonText.Text.Equals("\uE70D")) // ChevronDown
            {
                // Expand
                HeaderExpandButtonText.Text = "\uE70E"; // ChevronUp
                // http://stackoverflow.com/questions/11385026/setting-height-of-textbox-to-auto-in-code-behind-for-a-dynamically-created-textb
                AddressBlock.Height = double.NaN;
            }
            else
            {
                // Shrink
                HeaderExpandButtonText.Text = "\uE70D"; // ChevronDown
                AddressBlock.Height = HeaderExpandButton.Height;
            }
        }

        private async void SaveAllButton_Click(object sender, RoutedEventArgs e)
        {
            // https://docs.microsoft.com/en-us/windows/uwp/files/quickstart-using-file-and-folder-pickers
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                StorageFile file;
                foreach (MimePart _item in message.Attachments)
                {
                    file = await folder.CreateFileAsync(_item.FileName, CreationCollisionOption.GenerateUniqueName);

                    bool success = await MimePart2FileAsync(_item, file);
                    if (!success)
                    {
                        var dlg = new ContentDialog() { Title = "Aborted", Content = "File " + file.Name + " couldn't be saved.", CloseButtonText = "OK" };
                        await dlg.ShowAsync();
                        return;
                    }
                }
            }
        }

        private async Task<bool> MimePart2FileAsync(MimePart item, StorageFile file)
        {
            // Prevent updates to the remote version of the file until
            // we finish making changes and call CompleteUpdatesAsync.
            CachedFileManager.DeferUpdates(file);

            // write to file
            using (Stream _stream = await file.OpenStreamForWriteAsync())
            {
                await item.Content.DecodeToAsync(_stream);
            }

            // Let Windows know that we're finished changing the file so
            // the other app can update the remote version of the file.
            // Completing updates may require Windows to ask for user input.
            Windows.Storage.Provider.FileUpdateStatus status =
                await CachedFileManager.CompleteUpdatesAsync(file);
            if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
            {
                //this.textBlock.Text = "File " + file.Name + " was saved.";
                return true;
            }
            else
            {
                //this.textBlock.Text = "File " + file.Name + " couldn't be saved.";
                return false;
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

        private async void OpenAsPdfButton_Click(object sender, RoutedEventArgs e)
        {
            // https://itc.tokyo/html/html-font-size/
            StringBuilder printheader = new StringBuilder();
            printheader
                //.Append("<style>.bigtext{font-size:18px;}.smalltext{font-size:12px;}</style>")
                .Append("<big><b>").Append(SubjectTextBlock.Text).Append("</b></big></br></br>")
                .Append("<big>").Append(HttpUtility.HtmlEncode(message.From.ToString())).Append("</big></br>")
                .Append("<small>").Append(DateTextBlock.Text).Append("</br>")
                .Append("To: ").Append(HttpUtility.HtmlEncode(message.To.ToString())).Append("</br>");
            if (message.Cc.Count() > 0)
                printheader.Append("Cc: ").Append(HttpUtility.HtmlEncode(message.Cc.ToString())).Append("</br>");
            printheader.Append("</small></br>");

            int count = message.Attachments.Count();
            if (count > 0)
            {
                printheader.Append("<small>").Append(count).Append(" attachments</br>");
                foreach (MimePart _item in message.Attachments)
                {
                    printheader.Append(_item.FileName).Append("; ");
                }
                printheader.Append("</small></br></br>");
            }

            Progress.IsActive = true;
            OpenAsPdfButton.IsEnabled = false;

            await MailView.EnsureCoreWebView2Async();

            try
            {
                // Insert header
                await MailView.ExecuteScriptAsync($"document.getElementById(\"emlReaderPrintHeader\").innerHTML = \"{printheader.ToString()}\";");
                // Get header height
                var _height = await MailView.ExecuteScriptAsync("(function(){var h = document.getElementById(\"emlReaderPrintHeader\").clientHeight; return h;})()");
                double height;
                _ = double.TryParse(_height, out height);
                // Set margin of webview
                MailView.Margin = new Thickness(4, 0 - height, 0, 0);

                // Print PDF document to temp folder
                string temppath = $"{ApplicationData.Current.TemporaryFolder.Path}\\{this.filename}.pdf";
                bool success = await MailView.CoreWebView2.PrintToPdfAsync(temppath, default);

                // Load temp file as StorageFile
                StorageFile tempfile = await StorageFile.GetFileFromPathAsync(temppath);
                if (success && tempfile != null)
                {
                    // Launch the retrieved file
                    _ = await Launcher.LaunchFileAsync(tempfile);
                }
                else
                {
                    NotificationBar.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                NotificationBar.IsOpen = true;
            }
            finally
            {
                // Remove header
                await MailView.ExecuteScriptAsync($"document.getElementById(\"emlReaderPrintHeader\").innerHTML = \"\";");
                // Reset margin of webview
                MailView.Margin = new Thickness(4, 12, 0, 0);

                Progress.IsActive = false;
                OpenAsPdfButton.IsEnabled = true;
            }
        }

        private async void AttachmentTemplate_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.AllowedOperations = DataPackageOperation.Copy;
            MimePart item = (sender as FrameworkElement).DataContext as MimePart;

            // https://github.com/Microsoft/Windows-universal-samples/tree/master/Samples/AssociationLaunching
            //Can't launch files directly from install folder so copy it over 
            //to temporary folder first
            // ** Unnessesary for PC, but needed for Mobile
            StorageFolder tempfolder = ApplicationData.Current.TemporaryFolder;
            StorageFile file = await tempfolder.CreateFileAsync(item.FileName, CreationCollisionOption.ReplaceExisting);
            bool success = await MimePart2FileAsync(item, file);
            if (!success)
            {
                var dlg = new ContentDialog() { Content = "File " + file.Name + " couldn't be copied.", CloseButtonText = "OK" };
                await dlg.ShowAsync();
                return;
            }

            // Set file to DataPackage
            args.Data.SetStorageItems(new StorageFile[] { file });
        }

    }
}

