using CadMath2D;
using JwwHelper;
using Microsoft.Win32;
using SvgHelper;
using SvgToJwwConverter.SvgToJww;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Formats.Asn1.AsnWriter;
using Path = System.IO.Path;


namespace SvgToJwwConverter
{
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


        public int CurveDiv {
            get => Properties.Settings.Default.CurveDiv;
            set => Properties.Settings.Default.CurveDiv = value;
        }

        public int[] CurveDivList { get; } = new int[]
        {
            10,20,30,40,50,60,70,80
        };

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
        private bool mConvertCancel = false;

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

        private async void Part_Convert_Click(object sender, RoutedEventArgs e)
        {
            using var r = File.OpenRead(mSvgPath);
            var reader = new SvgHelper.SvgReader();
            try
            {
                Part_WaitingOverlay.Visibility = Visibility.Visible;
                Part_Cancel.Visibility = Visibility.Visible;
                Part_Progress.Visibility = Visibility.Visible;
                Part_Progress.IsIndeterminate = true;
                await Task.Run(() => {
                    reader.Read(r, Completed);
                });
                r.Close();
                if (mConvertCancel)
                {
                    SetMessage(Properties.Resources.Canceled, 3000);
                } else
                {
                    SetMessage(Properties.Resources.Completed, 3000);
                }
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
                mConvertCancel = false;
            }
        }

        private void Part_Cancel_Click(object sender, RoutedEventArgs e)
        {
            mConvertCancel = true;
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

        /// <summary>
        /// SVGファイル読み込み完了処理。
        /// </summary>
        private void Completed(SvgShapeContainer container)
        {
            var fileName = @"d:\out.jww";
            Converter.ConvertToJww(fileName, container);
            if (OpenJwwAfterConversion)
            {
                Process.Start(
                    new ProcessStartInfo("cmd", $"/c start {fileName}")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    });
            }
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
                (settings.WindowLeft + ActualWidth) < SystemParameters.VirtualScreenWidth) { Left = settings.WindowLeft; }
            // 上
            if (settings.WindowTop >= 0 &&
                (settings.WindowTop + ActualHeight) < SystemParameters.VirtualScreenHeight) { Top = settings.WindowTop; }
        }

        DispatcherTimer mMessageTimer = new();
        void SetMessage(string message, int periodMS)
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
