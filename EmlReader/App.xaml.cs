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
    /// ����� Application �N���X��⊮����A�v���P�[�V�����ŗL�̓����񋟂��܂��B
    /// </summary>
    sealed partial class Application : Windows.UI.Xaml.Application
    {
        /// <summary>
        /// �P��A�v���P�[�V���� �I�u�W�F�N�g�����������܂��B����́A���s�����쐬�����R�[�h��
        ///�ŏ��̍s�ł��邽�߁Amain() �܂��� WinMain() �Ƙ_���I�ɓ����ł��B
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
        /// �A�v���P�[�V�������G���h ���[�U�[�ɂ���Đ���ɋN�����ꂽ�Ƃ��ɌĂяo����܂��B���̃G���g�� �|�C���g�́A
        /// �A�v���P�[�V����������̃t�@�C�����J�����߂ɋN�����ꂽ�Ƃ��ȂǂɎg�p����܂��B
        /// </summary>
        /// <param name="e">�N���̗v���ƃv���Z�X�̏ڍׂ�\�����܂��B</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = false;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // �E�B���h�E�Ɋ��ɃR���e���c���\������Ă���ꍇ�́A�A�v���P�[�V�����̏��������J��Ԃ����ɁA
            // �E�B���h�E���A�N�e�B�u�ł��邱�Ƃ������m�F���Ă�������
            if (rootFrame == null)
            {
                // �i�r�Q�[�V���� �R���e�L�X�g�Ƃ��ē��삷��t���[�����쐬���A�ŏ��̃y�[�W�Ɉړ����܂�
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: �ȑO���f�����A�v���P�[�V���������Ԃ�ǂݍ��݂܂�
                }

                // �t���[�������݂̃E�B���h�E�ɔz�u���܂�
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // �i�r�Q�[�V���� �X�^�b�N����������Ȃ��ꍇ�́A�ŏ��̃y�[�W�Ɉړ����܂��B
                // ���̂Ƃ��A�K�v�ȏ����i�r�Q�[�V���� �p�����[�^�[�Ƃ��ēn���āA�V�����y�[�W��
                //�\�����܂�
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // ���݂̃E�B���h�E���A�N�e�B�u�ł��邱�Ƃ��m�F���܂�
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
        /// ����̃y�[�W�ւ̈ړ������s�����Ƃ��ɌĂяo����܂�
        /// </summary>
        /// <param name="sender">�ړ��Ɏ��s�����t���[��</param>
        /// <param name="e">�i�r�Q�[�V���� �G���[�̏ڍ�</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// �A�v���P�[�V�����̎��s�����f���ꂽ�Ƃ��ɌĂяo����܂��B
        /// �A�v���P�[�V�������I������邩�A�������̓��e�����̂܂܂ōĊJ����邩��
        /// ������炸�A�A�v���P�[�V�����̏�Ԃ��ۑ�����܂��B
        /// </summary>
        /// <param name="sender">���f�v���̑��M���B</param>
        /// <param name="e">���f�v���̏ڍׁB</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: �A�v���P�[�V�����̏�Ԃ�ۑ����ăo�b�N�O���E���h�̓��삪����Β�~���܂�
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

            // �E�B���h�E�Ɋ��ɃR���e���c���\������Ă���ꍇ�́A�A�v���P�[�V�����̏��������J��Ԃ����ɁA
            // �E�B���h�E���A�N�e�B�u�ł��邱�Ƃ������m�F���Ă�������
            if (rootFrame == null)
            {
                // �i�r�Q�[�V���� �R���e�L�X�g�Ƃ��ē��삷��t���[�����쐬���A�ŏ��̃y�[�W�Ɉړ����܂�
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // �t���[�������݂̃E�B���h�E�ɔz�u���܂�
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

            // ���݂̃E�B���h�E���A�N�e�B�u�ł��邱�Ƃ��m�F���܂�
            Window.Current.Activate();
        }

    }
}
