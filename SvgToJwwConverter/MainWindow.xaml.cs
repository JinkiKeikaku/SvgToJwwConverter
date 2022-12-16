using Microsoft.Win32;
using SvgHelper;
using SvgToJwwConverter.SvgToJww;
using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Path = System.IO.Path;


namespace SvgToJwwConverter {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow()
        {
            if (Properties.Settings.Default.IsUpgrade == false)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.IsUpgrade = true;
                Debug.WriteLine(this, "Upgraded");
            }
            InitializeComponent();
            DataContext = this;
            Utility.LoadJwwDll();
        }

        public JwwPaper Paper {
            get => JwwPaper.GetJwwPaperSize(Properties.Settings.Default.JwwPaperCode);
            set => Properties.Settings.Default.JwwPaperCode = value.Code;
        }

        public JwwPaper[] PaperList {
            get => JwwPaper.JwwPaperSizeArray;
        }


        public JwwScale Scale {
            get => new JwwScale(Properties.Settings.Default.JwwScaleValue);
            set => Properties.Settings.Default.JwwScaleValue = value.ScaleNumber;
        }

        public JwwScale[] ScaleList { get; } = new JwwScale[]
        {
            new (0.2),
            new (0.5),
            new (1),
            new (2),
            new (5),
            new (10),
            new (50),
            new (100),
            new (200),
            new (500),
            new (1000),
        };


        public int CurveDiv {
            get => Properties.Settings.Default.CurveDiv;
            set => Properties.Settings.Default.CurveDiv = value;
        }

        public int[] CurveDivList { get; } = new int[]
        {
            6, 8, 10,20,30,40,50
        };

        public int PenNumber {
            get => Properties.Settings.Default.PenNumber;
            set => Properties.Settings.Default.PenNumber = value;
        }
        public int[] PenNumberList { get; } = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        public bool EnableOverwrite {
            get => Properties.Settings.Default.EnableOverwrite;
            set => Properties.Settings.Default.EnableOverwrite = value;
        }

        public bool OnlyLine {
            get => Properties.Settings.Default.OnlyLine;
            set => Properties.Settings.Default.OnlyLine = value;
        }

        public bool OpenJwwAfterConversion {
            get => Properties.Settings.Default.OpenJwwAfterConversion;
            set => Properties.Settings.Default.OpenJwwAfterConversion = value;
        }

        private string mSvgPath = "";
        private string mJwwPath = "";
        private CancellationTokenSource mCancellationTokenSource = null!;

        private void Part_Open_Click(object sender, RoutedEventArgs e)
        {
            var f = new OpenFileDialog
            {
                FileName = "",
                FilterIndex = 1,
                Filter = "SVG file(.svg)|*.svg|All files (*.*)|*.*",
            };
            if (f.ShowDialog(Application.Current.MainWindow) == true) SetSvgPath(f.FileName);
        }

        private void Part_Convert_Click(object sender, RoutedEventArgs e)
        {
            Conveert();
        }

        private async void Conveert()
        {
            try
            {
                if (!File.Exists(mSvgPath)) return;
                var f = new SaveFileDialog
                {
                    FileName = Path.ChangeExtension(mSvgPath, "jww"),
                    FilterIndex = 1,
                    Filter = "Jww file(.jww)|*.jww|All files (*.*)|*.*",
                };
                if (f.ShowDialog(Application.Current.MainWindow) != true) return;
                mJwwPath = f.FileName;
                using var r = File.OpenRead(mSvgPath);
                var reader = new SvgHelper.SvgReader();
                Part_WaitingOverlay.Visibility = Visibility.Visible;
                Part_Cancel.Visibility = Visibility.Visible;
                Part_Progress.Visibility = Visibility.Visible;
                Part_Progress.IsIndeterminate = true;
                mCancellationTokenSource = new CancellationTokenSource();
                await Task.Run(() => {
                    //ファイル読み込み。実行完了後にCompleted()が呼ばれる。
                    reader.Read(r, Completed, mCancellationTokenSource.Token);
                });
                r.Close();
                SetMessage(Properties.Resources.Completed, 3000);
            } catch (OperationCanceledException ex)
            {
                SystemSounds.Beep.Play();
                SetMessage("Canceled", 3000);
            } catch (Exception ex)
            {
                SystemSounds.Beep.Play();
                SetMessage("Error!", 3000);
                MessageBox.Show(ex.Message, "Error");
            } finally
            {
                Part_WaitingOverlay.Visibility = Visibility.Collapsed;
                Part_Cancel.Visibility = Visibility.Hidden;
                Part_Progress.Visibility = Visibility.Collapsed;
                Part_Progress.IsIndeterminate = false;
            }
        }

        /// <summary>
        /// SVGファイル読み込み完了処理。
        /// </summary>
        private void Completed(SvgShapeContainer container)
        {
            var cancelToken = mCancellationTokenSource.Token;
            Converter.ConvertToJww(mJwwPath, container, cancelToken, (s) => {
                this.Dispatcher.Invoke(() => {
                    SetMessage(s, 0);
                });
            });
            if (OpenJwwAfterConversion)
            {
                Process.Start(
                    new ProcessStartInfo("cmd", $"/c start {mJwwPath}")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    });
            }
        }

        private DispatcherTimer mMessageTimer = new();
        //ステータスバーメッセージ表示
        private void SetMessage(string message, int periodMS)
        {
            mMessageTimer.Stop();
            if (periodMS > 0)
            {
                mMessageTimer.Tick += (s, args) => {
                    Part_Message.Text = "";
                };
                mMessageTimer.Interval = TimeSpan.FromMilliseconds(periodMS);
                mMessageTimer.Start();
            }
            Part_Message.Text = message;
        }

        /// <summary>
        /// ファイルパスをテキストボックスと変数に入れる。
        /// </summary>
        private void SetSvgPath(string svgPath)
        {
            mSvgPath = svgPath;
            Part_File.Text = svgPath;
        }

        private bool IsSvgFile(string name)
        {
            return Path.GetExtension(name).ToLower() == ".svg";
        }

        private void Part_Cancel_Click(object sender, RoutedEventArgs e)
        {
            mCancellationTokenSource.Cancel();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RecoverWindowBounds();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowBounds();
            Properties.Settings.Default.Save();
        }

        private void SaveWindowBounds()
        {
            var settings = Properties.Settings.Default;
            WindowState = WindowState.Normal; // 最大化解除
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
        }
        private void RecoverWindowBounds()
        {
            var settings = Properties.Settings.Default;
            // 左
            if (settings.WindowLeft >= 0 &&
                (settings.WindowLeft + ActualWidth) < SystemParameters.VirtualScreenWidth) 
            { 
                Left = settings.WindowLeft;
            }
            // 上
            if (settings.WindowTop >= 0 &&
                (settings.WindowTop + ActualHeight) < SystemParameters.VirtualScreenHeight)
            { 
                Top = settings.WindowTop; 
            }
        }
        private void Window_Drop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("Window_Drop");
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
                {
                    if (IsSvgFile(files[0])) SetSvgPath(files[0]);
                }
            }
        }
        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            Debug.WriteLine("Window_PreviewDragOver");
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                e.Effects = System.Windows.DragDropEffects.None;
                if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
                {
                    var ext = Path.GetExtension(files[0]).ToLower();
                    e.Effects = IsSvgFile(files[0]) ?
                        System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
                    e.Handled = true;
                    return;
                }
            }
        }
        /// <summary>
        /// ハイパーリンクからHPを開く。（MainWindow.xaml内で使用）
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(
                new ProcessStartInfo("cmd", $"/c start {e.Uri.ToString()}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                });
        }
    }
}
