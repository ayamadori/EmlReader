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
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
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

        public MailPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Load the message
            StorageFile file = (StorageFile)e.Parameter;
            using (var _stream = await file.OpenReadAsync())
            {
                Message = MimeMessage.Load(_stream.AsStreamForRead());
            }
        }

        public MimeMessage Message
        {
            get { return message; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                message = value;

                var from = message.From.First() as MailboxAddress;
                if (string.IsNullOrEmpty(from.Name)) from.Name = from.Address;
                fromTextBlock.Text = from.Name;

                dateTextBlock.Text = message.Date.ToString(CultureInfo.CurrentCulture);

                subjectTextBlock.Text = message.Subject;

                InternetAddressList addressList = new InternetAddressList();

                if (message.To.Count > 0)
                    ToView.ItemsSource = FormatList(message.To);

                if (message.Cc.Count > 0)
                    CcView.ItemsSource = FormatList(message.Cc);

                if (message.Attachments.Count() > 0)
                    attachmentView.ItemsSource = message.Attachments;
                else
                    attachmentView.Visibility = Visibility.Collapsed;

                // render the message body
                Render(message.Body);
            }
        }

        void RenderMultipartRelated(MultipartRelated related)
        {
            var root = related.Root;

            if (root == null)
                return;

            Render(root);
        }

        void RenderText(TextPart text)
        {
            string html;

            if (text.IsHtml)
            {
                // the text content is already in HTML format
                html = text.Text;
            }
            else if (text.IsFlowed)
            {
                var converter = new FlowedToHtml();
                string delsp;

                // the delsp parameter specifies whether or not to delete spaces at the end of flowed lines
                if (!text.ContentType.Parameters.TryGetValue("delsp", out delsp))
                    delsp = "no";

                if (string.Compare(delsp, "yes", StringComparison.OrdinalIgnoreCase) == 0)
                    converter.DeleteSpace = true;

                html = converter.Convert(text.Text);
            }
            else {
                html = new TextToHtml().Convert(text.Text);
            }

            webView.NavigateToString(html);
        }

        void Render(MimeEntity entity)
        {
            var related = entity as MultipartRelated;

            if (related != null)
            {
                RenderMultipartRelated(related);
                return;
            }

            var multipart = entity as Multipart;
            var text = entity as TextPart;

            // check if the entity is a multipart
            if (multipart != null)
            {
                if (multipart.ContentType.IsMimeType("multipart", "alternative"))
                {
                    // A multipart/alternative is just a collection of alternate views.
                    // The last part is the format that most closely matches what the
                    // user saw in his or her email client's WYSIWYG editor.
                    TextPart preferred = null;

                    for (int i = multipart.Count; i > 0; i--)
                    {
                        related = multipart[i - 1] as MultipartRelated;

                        if (related != null)
                        {
                            var root = related.Root;

                            if (root != null && root.ContentType.IsMimeType("text", "html"))
                            {
                                RenderMultipartRelated(related);
                                return;
                            }

                            continue;
                        }

                        text = multipart[i - 1] as TextPart;

                        if (text == null)
                            continue;

                        if (text.IsHtml)
                        {
                            // we prefer html over plain text
                            preferred = text;
                            break;
                        }

                        if (preferred == null)
                        {
                            // we'll take what we can get
                            preferred = text;
                        }
                    }

                    if (preferred != null)
                        RenderText(preferred);
                }
                else if (multipart.Count > 0)
                {
                    // At this point we know we're not dealing with a multipart/related or a
                    // multipart/alternative, so we can safely treat this as a multipart/mixed
                    // even if it's not.

                    // The main message body is usually the first part of a multipart/mixed. I
                    // suppose that it might be better to render the first text/* part instead
                    // (in case it's not the first part), but that's rare and probably also
                    // indicates that the text is meant to be displayed between the other parts
                    // (probably images or video?) in some sort of pseudo-multimedia "document"
                    // layout. Modern clients don't do this, they use HTML or RTF instead.
                    Render(multipart[0]);
                }
            }
            else if (text != null)
            {
                // render the text part
                RenderText(text);
            }
            else {
                // message/rfc822 part
            }
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
                    if (filetype.Equals(".msg") || filetype.Equals(".eml"))
                    {
                        using (var _stream = await storageFile.OpenReadAsync())
                        {
                            Message = MimeMessage.Load(_stream.AsStreamForRead());
                        }
                    }
                }
            }
        }

        private async void attachmentView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MimePart _item = (MimePart)attachmentView.SelectedItem;
            // https://social.msdn.microsoft.com/Forums/it-IT/2407812c-2b59-4a7c-972a-778c32b91220/sharing-an-inmemory-png-file
            var file = await StorageFile.CreateStreamedFileAsync(_item.FileName, async (fileStream) =>
            {
                _item.ContentObject.DecodeTo(fileStream.AsStreamForWrite());
                await fileStream.FlushAsync();
                fileStream.Dispose();
            }, null);
            if (file != null)
            {
                // Launch the retrieved file
                var success = await Windows.System.Launcher.LaunchFileAsync(file);
            }
        }

        private InternetAddressList FormatList(InternetAddressList list)
        {
            foreach(InternetAddress item in list)
            {
                if (string.IsNullOrEmpty(item.Name))
                    item.Name = (item as MailboxAddress).Address;
                //if (item is GroupAddress)
                //    list.Remove(item);
            }
            return list;
        }

        private void AddressTemplate_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private async void OpenFlyoutItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MimePart _item = (MimePart)(sender as FrameworkElement).DataContext;
            // https://social.msdn.microsoft.com/Forums/it-IT/2407812c-2b59-4a7c-972a-778c32b91220/sharing-an-inmemory-png-file
            var file = await StorageFile.CreateStreamedFileAsync(_item.FileName, async (fileStream) =>
            {
                _item.ContentObject.DecodeTo(fileStream.AsStreamForWrite());
                await fileStream.FlushAsync();
                fileStream.Dispose();
            }, null);
            if (file != null)
            {
                // Launch the retrieved file
                var success = await Windows.System.Launcher.LaunchFileAsync(file);
            }
        }

        private async void SaveFlyoutItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MimePart _item = (MimePart)(sender as FrameworkElement).DataContext;

            // https://msdn.microsoft.com/ja-jp/library/windows/apps/mt186455.aspx
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            // Dropdown of file types the user can save the file as
            // http://stackoverflow.com/questions/17070908/how-can-i-accept-any-file-type-in-filesavepicker
            savePicker.FileTypeChoices.Add("All files", new List<string>() { "." });
            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = _item.FileName;

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until
                // we finish making changes and call CompleteUpdatesAsync.
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                // write to file
                using (Stream _stream = await file.OpenStreamForWriteAsync())
                {
                    _item.ContentObject.DecodeTo(_stream);
                }
                // Let Windows know that we're finished changing the file so
                // the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                Windows.Storage.Provider.FileUpdateStatus status =
                    await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
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

        private void AttachmentTemplate_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void FromView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            From.Text = (message.From.First() as MailboxAddress).Address;
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private async void Flyout_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var _address = (sender as MenuFlyoutItem).Text;
            Uri uri = new Uri("mailto:" + _address);
            // Launch browser
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


        private async void webView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            // Autosize WebView to its Content
            // http://www.jasonpoon.ca/2015/01/08/resizing-webview-to-its-content/

            //ResizeToContent(webView);

            // Disable scrolling in WebView
            // http://blogs.msdn.com/b/ashish/archive/2014/09/17/universal-windows-app-how-to-disable-scroll-in-webview.aspx

            string SetBodyOverFlowHiddenString = 
                @"function SetBodyOverFlowHidden()
                  {
                     document.body.style.overflow = 'hidden';
                     return 'Set Style to hidden';
                  }
                  SetBodyOverFlowHidden();";

            string returnStr = await webView.InvokeScriptAsync("eval", new string[] { SetBodyOverFlowHiddenString });
        }

        // http://www.jasonpoon.ca/2015/01/08/resizing-webview-to-its-content/
        private async void ResizeToContent(WebView webView)
        {
            string heightString = await webView.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
            int height;
            if (int.TryParse(heightString, out height))
            {
                webView.Height = height;
            }

            string widthString = await webView.InvokeScriptAsync("eval", new[] { "document.body.scrollWidth.toString()" });
            int width;
            if (int.TryParse(widthString, out width))
            {
                webView.Width = width;
            }
        }
		
		private void MailPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            webView.Width = Window.Current.Bounds.Width;
            webView.Height = Window.Current.Bounds.Height;
        }

        private void HeaderExpandButton_Click(object sender, RoutedEventArgs e)
        {         
            if (HeaderExpandButtonText.Text.Equals("\uE011"))
            {
                // Expand
                HeaderExpandButtonText.Text = "\uE010"; // ScrollChevronUpLegacy
                ToView.ItemsPanel = (ItemsPanelTemplate)Resources["WrapTemplate"];
                if(message.Cc.Count() != 0) CcArea.Visibility = Visibility.Visible;
            }
            else
            {
                // Shrink
                HeaderExpandButtonText.Text = "\uE011"; // ScrollChevronDownLegacy
                ToView.ItemsPanel = (ItemsPanelTemplate)Resources["StackTemplate"];
                CcArea.Visibility = Visibility.Collapsed;
            }
        }
    }
}

