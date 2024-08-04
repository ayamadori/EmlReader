using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace EmlReader
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class Application : Windows.UI.Xaml.Application
    {
        /// <summary>
        /// 単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
        ///最初の行であるため、main() または WinMain() と論理的に等価です。
        /// </summary>
        public Application()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            // https://github.com/jstedfast/MimeKit/issues/1047
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        // https://github.com/microsoft/AppModelSamples/blob/master/Samples/BananaEdit/BananaEdit/App.xaml.cs
        private static Guid id = Guid.NewGuid();
        public static Guid Id { get { return id; } }

        /// <summary>
        /// アプリケーションがエンド ユーザーによって正常に起動されたときに呼び出されます。他のエントリ ポイントは、
        /// アプリケーションが特定のファイルを開くために起動されたときなどに使用されます。
        /// </summary>
        /// <param name="e">起動の要求とプロセスの詳細を表示します。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = false;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // ウィンドウに既にコンテンツが表示されている場合は、アプリケーションの初期化を繰り返さずに、
            // ウィンドウがアクティブであることだけを確認してください
            if (rootFrame == null)
            {
                // ナビゲーション コンテキストとして動作するフレームを作成し、最初のページに移動します
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 以前中断したアプリケーションから状態を読み込みます
                }

                // フレームを現在のウィンドウに配置します
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // ナビゲーション スタックが復元されない場合は、最初のページに移動します。
                // このとき、必要な情報をナビゲーション パラメーターとして渡して、新しいページを
                //構成します
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // 現在のウィンドウがアクティブであることを確認します
            Window.Current.Activate();

            // Back Button
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            rootFrame.Navigated += RootFrame_Navigated;
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = rootFrame.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            // Check that no one has already handled this
            if (!e.Handled)
            {
                // Default is to navigate back within the Frame
                Frame frame = Window.Current.Content as Frame;
                if (frame.CanGoBack)
                {
                    frame.GoBack();
                    // Signal handled so the system doesn't navigate back through the app stack
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 特定のページへの移動が失敗したときに呼び出されます
        /// </summary>
        /// <param name="sender">移動に失敗したフレーム</param>
        /// <param name="e">ナビゲーション エラーの詳細</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// アプリケーションの実行が中断されたときに呼び出されます。
        /// アプリケーションが終了されるか、メモリの内容がそのままで再開されるかに
        /// かかわらず、アプリケーションの状態が保存されます。
        /// </summary>
        /// <param name="sender">中断要求の送信元。</param>
        /// <param name="e">中断要求の詳細。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: アプリケーションの状態を保存してバックグラウンドの動作があれば停止します
            deferral.Complete();
        }

        // refer to https://msdn.microsoft.com/en-us/library/windows/apps/mt243292.aspx
        protected override async void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            // Code to handle activation goes here.	

            // https://msdn.microsoft.com/en-us/library/windows/apps/windows.applicationmodel.datatransfer.datapackageview.getstorageitemsasync.aspx?cs-save-lang=1&cs-lang=csharp#code-snippet-1
            var shareOperation = args.ShareOperation;
            if (shareOperation.Data.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await shareOperation.Data.GetStorageItemsAsync();
                LaunchFiles(items);
            }
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            LaunchFiles(args.Files);
        }

        private async void LaunchFiles(IReadOnlyList<IStorageItem> files)
        {
            // The number of files received is args.Files.Size
            // The name of the first file is args.Files[0].Name

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = false;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // ウィンドウに既にコンテンツが表示されている場合は、アプリケーションの初期化を繰り返さずに、
            // ウィンドウがアクティブであることだけを確認してください
            if (rootFrame == null)
            {
                // ナビゲーション コンテキストとして動作するフレームを作成し、最初のページに移動します
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // フレームを現在のウィンドウに配置します
                Window.Current.Content = rootFrame;
            }

            StorageFile file;

            // Launch primary file
            if (rootFrame.Content == null)
            {
                // We always want to navigate to the MainPage, regardless
                // of whether or not this is a redirection.
                file = files[0] as StorageFile;                
                rootFrame.Navigate(typeof(MailPage), file);

                // Add to MRU
                // https://docs.microsoft.com/en-us/windows/uwp/files/how-to-track-recently-used-files-and-folders
                var mru = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;
                string mruToken = mru.Add(file, file.Name);
            }

            // Launch secondary file
            for (int i = 1; i < files.Count; i++)
            {
                // Open file on this app
                var options = new Windows.System.LauncherOptions();
                options.TargetApplicationPackageFamilyName = Package.Current.Id.FamilyName;
                file = files[i] as StorageFile;
                await Windows.System.Launcher.LaunchFileAsync(file, options);
            }

            // 現在のウィンドウがアクティブであることを確認します
            Window.Current.Activate();
        }

    }
}
