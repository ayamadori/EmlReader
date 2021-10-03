//using Microsoft.Toolkit.Uwp.Helpers;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
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
        //PrintHelper printHelper;
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
                    var dlg = new ContentDialog() { Title = "Unsupported file", Content = "The app can NOT open this file.", CloseButtonText = "OK"};
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
            string printheader = "<p class=\"printheader\" style=\"background: white; color: black;\">" +
                "FROM: " + HttpUtility.HtmlEncode(message.From.ToString()) + "</br>" +
                "DATE: " + HttpUtility.HtmlEncode(message.Date.ToString()) + "</br>" +
                "TO: " + HttpUtility.HtmlEncode(message.To.ToString()) + "</br>" +
                "CC: " + HttpUtility.HtmlEncode(message.Cc.ToString()) + "</br>" +
                "SUBJECT: " + HttpUtility.HtmlEncode(message.Subject.ToString()) +
                "</p>";

            var visitor = new HtmlPreviewVisitor(MailView, printheader);

            message.Accept(visitor);
        }

        // Hook navigation
        private async void MailView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
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
                    var link = new Hyperlink
                    {
                        UnderlineStyle = UnderlineStyle.None
                    };
                    // https://docs.microsoft.com/ja-jp/uwp/api/windows.ui.xaml.documents.hyperlink.click
                    link.Click += (sender, args) =>
                    {
                        OnUserClickShowContactCard(ToArea, item.Name, _address);
                    };

                    if (string.IsNullOrEmpty(item.Name))
                        link.Inlines.Add(new Run
                        {
                            Text = _address + "; ",
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 73, 73, 73))
                        });
                    else
                        link.Inlines.Add(new Run
                        {
                            //Text = item.Name + "<" + _address + ">; ",
                            Text = item.Name + "; ",
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 73, 73, 73))
                        });
                    AddressBlock.Inlines.Add(link);
                }
                else
                {
                    AddressBlock.Inlines.Add(new Run
                    {
                        Text = item.Name + "; ",
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 73, 73, 73))
                    });
                }
            }
        }

        private void FromView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MailboxAddress item = (message.From.First() as MailboxAddress);
            OnUserClickShowContactCard(FromView, item.Name, item.Address);
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
            else
            {
                // Launch People app
                bool success = await Launcher.LaunchUriAsync(new Uri("ms-people:viewcontact?Email=" + address));
            }
        }

        private void AttachmentView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MimePart item = AttachmentView.SelectedItem as MimePart;
            if (item != null) OpenFile(item);
        }

        private void AttachmentTemplate_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void OpenFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFile((sender as FrameworkElement).DataContext as MimePart);
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

            _scheme += "?subject=" + WebUtility.UrlEncode("RE: " + message.Subject).Replace("+", "%20").Replace("/", "%2F");

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
                foreach (MimePart _item in message.Attachments)
                {
                    file = await folder.CreateFileAsync(_item.FileName, CreationCollisionOption.GenerateUniqueName);

                    bool success = await MimePart2FileAsync(_item, file);
                    if (!success)
                    {
                        var dlg = new ContentDialog() { Title = "Aborted" , Content = "File " + file.Name + " couldn't be saved.", CloseButtonText = "OK"};
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

        private void DonationButton_Click(object sender, RoutedEventArgs e)
        {

        }

        //// https://docs.microsoft.com/en-us/windows/communitytoolkit/helpers/printhelper
        //private async void PrintButton_Click(object sender, RoutedEventArgs e)
        //{
        //    //// https://stackoverflow.com/questions/39033318/how-to-save-webviewbrush-as-image-uwp-universal
        //    var pages = await GetWebPages(MailView, new Windows.Foundation.Size(210, 297)); // A4 size

        //    // Create a new PrintHelper instance
        //    // "container" is a XAML panel that will be used to host printable control.
        //    // It needs to be in your visual tree but can be hidden with Opacity = 0
        //    printHelper = new PrintHelper(Container);

        //    // Add controls that you want to print
        //    foreach (FrameworkElement page in pages)
        //    {
        //        printHelper.AddFrameworkElementToPrint(page);
        //    }

        //    // Connect to relevant events
        //    printHelper.OnPrintFailed += PrintHelper_OnPrintFailed;
        //    printHelper.OnPrintSucceeded += PrintHelper_OnPrintSucceeded;
        //    printHelper.OnPrintCanceled += PrintHelper_OnPrintCanceled;

        //    // Start printing process
        //    await printHelper.ShowPrintUIAsync(file.DisplayName);
        //}

        //// Event handlers

        //private void PrintHelper_OnPrintSucceeded()
        //{
        //    printHelper.Dispose();
        //}

        //private void PrintHelper_OnPrintFailed()
        //{
        //    printHelper.Dispose();
        //}

        //private void PrintHelper_OnPrintCanceled()
        //{
        //    printHelper.Dispose();
        //}

        //public async Task<WebViewBrush> GetWebViewBrush(WebView MailView)
        //{
        //    // resize width to content
        //    double originalWidth = MailView.Width;
        //    var widthString = await MailView.InvokeScriptAsync("eval", new[] { "document.body.scrollWidth.toString()" });
        //    int contentWidth;

        //    if (!int.TryParse(widthString, out contentWidth))
        //    {
        //        throw new Exception(string.Format("failure/width:{0}", widthString));
        //    }

        //    MailView.Width = contentWidth;

        //    // resize height to content
        //    double originalHeight = MailView.Height;
        //    var heightString = await MailView.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
        //    int contentHeight;

        //    if (!int.TryParse(heightString, out contentHeight))
        //    {
        //        throw new Exception(string.Format("failure/height:{0}", heightString));
        //    }

        //    MailView.Height = contentHeight;

        //    // create brush
        //    var originalVisibilty = MailView.Visibility;
        //    MailView.Visibility = Windows.UI.Xaml.Visibility.Visible;

        //    WebViewBrush brush = new WebViewBrush
        //    {
        //        SourceName = MailView.Name,
        //        Stretch = Stretch.Uniform
        //    };

        //    brush.Redraw();

        //    // reset, return
        //    MailView.Width = originalWidth;
        //    MailView.Height = originalHeight;
        //    MailView.Visibility = originalVisibilty;

        //    return brush;
        //}

        //public async Task<IEnumerable<FrameworkElement>> GetWebPages(WebView MailView, Windows.Foundation.Size page)
        //{
        //    // ask the content its width
        //    var widthString = await MailView.InvokeScriptAsync("eval", new[] { "document.body.scrollWidth.toString()" });
        //    int contentWidth;

        //    if (!int.TryParse(widthString, out contentWidth))
        //    {
        //        throw new Exception(string.Format("failure/width:{0}", widthString));
        //    }

        //    MailView.Width = contentWidth;

        //    // ask the content its height
        //    var heightString = await MailView.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
        //    int contentHeight;

        //    if (!int.TryParse(heightString, out contentHeight))
        //    {
        //        throw new Exception(string.Format("failure/height:{0}", heightString));
        //    }

        //    MailView.Height = contentHeight;

        //    // how many pages will there be?
        //    double scale = page.Width / contentWidth;
        //    double scaledHeight = (contentHeight * scale);
        //    double pageCount = (double)scaledHeight / page.Height;
        //    pageCount = pageCount + ((pageCount > (int)pageCount) ? 1 : 0);

        //    // create the pages
        //    var pages = new List<Windows.UI.Xaml.Shapes.Rectangle>();

        //    for (int i = 0; i < (int)pageCount; i++)
        //    {
        //        var translateY = -page.Height * i;

        //        var rectanglePage = new Windows.UI.Xaml.Shapes.Rectangle
        //        {
        //            Height = page.Height,
        //            Width = page.Width,
        //            Margin = new Thickness(5),
        //            Tag = new TranslateTransform { Y = translateY },
        //        };

        //        rectanglePage.Loaded += (async (s, e) =>
        //        {
        //            var subRectangle = s as Windows.UI.Xaml.Shapes.Rectangle;
        //            var subBrush = await GetWebViewBrush(MailView);
        //            subBrush.Stretch = Stretch.UniformToFill;
        //            subBrush.AlignmentY = AlignmentY.Top;
        //            subBrush.Transform = subRectangle.Tag as TranslateTransform;
        //            subRectangle.Fill = subBrush;
        //        });

        //        pages.Add(rectanglePage);
        //    }

        //    return pages;
        //}
    }
}

