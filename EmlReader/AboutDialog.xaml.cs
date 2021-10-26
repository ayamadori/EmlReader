using System;
using System.Diagnostics;
using Windows.Services.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace EmlReader
{
    public sealed partial class AboutDialog : ContentDialog
    {
        public AboutDialog()
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
            // https://docs.microsoft.com/en-us/windows/uwp/monetize/request-ratings-and-reviews
            var success = await StoreContext.GetDefault().RequestRateAndReviewAppAsync();
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

        private async void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            //Uri uriFeedback = new Uri($"feedback-hub://?tabid=2&appid={Package.Current.Id.FamilyName}!App");
            //bool success = await Launcher.LaunchUriAsync(uriFeedback);

            //// https://docs.microsoft.com/en-us/windows/uwp/monetize/launch-feedback-hub-from-your-app
            var launcher = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.GetDefault();
            await launcher.LaunchAsync();
        }
    }
}
