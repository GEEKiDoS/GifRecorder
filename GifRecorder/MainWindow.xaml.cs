using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace GifRecorder
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StateChanged += (_s, _e) =>
            {
                if(WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                }
            };
        }

        private void btnStart_MouseEnter(object sender, MouseEventArgs e)
        {
            btnStart.BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xb0, 0xff));
        }

        private void btnStart_MouseLeave(object sender, MouseEventArgs e)
        {
            btnStart.BorderBrush = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0));
        }

        Process ffmpegProcess = null;
        string ffmpegLog = null;

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            var fps = txtFps.Text;

            if (ffmpegProcess != null)
            {
                disableMove = false;
                btnStart.IsEnabled = false;
                txtTitle.Text = "GIF录制者 - 结束录制中...";

                ffmpegProcess.Kill();

                ffmpegProcess.Dispose();
                ffmpegProcess = null;

                txtTitle.Text = "GIF录制者 - 生成调色板中...";
                var process = RunFFMpegWithArgs($"-i temp\\out\\%03d.jpg -vf \"fps={fps},scale=320:-1:flags=lanczos,palettegen\" -y temp\\palette.png");
                await TaskEx.Run(() => process.WaitForExit());

                txtTitle.Text = "GIF录制者 - 生成GIF中...";
                var dialog = new SaveFileDialog
                {
                    CheckFileExists = false,
                    AddExtension = true,
                    ValidateNames = true,
                    Title = "选择保存GIF的位置",
                    OverwritePrompt = true,
                    Filter = "GIF动画|.gif",
                    DefaultExt = ".gif",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                };


            tag1:
                if (dialog.ShowDialog() != true)
                {
                    goto tag1;
                }

                process = RunFFMpegWithArgs($"-r {fps} -i temp\\out\\%03d.jpg -i temp\\palette.png -lavfi \"paletteuse\" -r {fps} -y {dialog.FileName}");
                await TaskEx.Run(() => process.WaitForExit());

                Directory.Delete("temp", true);

                btnStart.IsEnabled = true;
                btnStartTxt.Text = "录制";
                txtTitle.Text = "GIF录制者";

                MessageBox.Show($"GIF生成完毕!\r\n\r\n位置:{dialog.FileName}","提示",MessageBoxButton.OK);
            }
            else
            {
                var resStr = $"{txtOutputX.Text}x{txtOutputY.Text}";

                Directory.CreateDirectory("temp\\out");

                var offsety = Top + 76;
                var offsetx = Left + 1;
                var arg = $"-f gdigrab -video_size {txtRecordSize.Text} -offset_x {offsetx} -offset_y {offsety} -draw_mouse {(cbDrawMouse.IsChecked == true ? 1 : 0)} -framerate {fps} -i desktop -s {resStr} -q 0 temp\\out\\%03d.jpg";
                if (cbFullscreen.IsChecked == true)
                {
                    arg = $"-f gdigrab -draw_mouse {(cbDrawMouse.IsChecked == true ? 1 : 0)} -r {fps} -i desktop -s {resStr} -r {fps} -q 0 temp\\out\\%03d.jpg";
                }
                ffmpegProcess = RunFFMpegWithArgs(arg);

                btnStartTxt.Text = "停止录制";
                txtTitle.Text = "GIF录制者 - 录制中...";
                disableMove = true;
            }
        }

        private Process RunFFMpegWithArgs(string args)
        {

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = args,
                    FileName = "bin\\ffmpeg.exe",
                    CreateNoWindow = true,
                    UseShellExecute = false,
//RedirectStandardOutput = true,
                   // RedirectStandardError = true
                },
            };
            /*
            process.OutputDataReceived += (sender, e) =>
            {
                ffmpegLog += e.Data;
                ffmpegLog += "\n";
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                ffmpegLog += e.Data;
                ffmpegLog += "\n";
            };

            process.Exited += (sender, e) =>
            {
                //MessageBox.Show(ffmpegLog);
                ffmpegLog = "";
            };
            */

            process.Start();

            return process;
        }

        bool disableScale = false;
        bool disableMove = false;

        #region 窗口缩放
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwndSource = PresentationSource.FromVisual(RecordZone) as HwndSource;

            if (hwndSource != null)
            {
                hwndSource.AddHook(new HwndSourceHook(WndProc));
            }
        }

        private const int WM_NCHITTEST = 0x0084;
        private readonly int agWidth = 12;
        private readonly int bThickness = 2;
        private Point mousePoint = new Point();

        public enum HitTest : int
        {
            HTERROR = -2,
            HTTRANSPARENT = -1,
            HTNOWHERE = 0,
            HTCLIENT = 1,
            HTCAPTION = 2,
            HTSYSMENU = 3,
            HTGROWBOX = 4,
            HTSIZE = HTGROWBOX,
            HTMENU = 5,
            HTHSCROLL = 6,
            HTVSCROLL = 7,
            HTMINBUTTON = 8,
            HTMAXBUTTON = 9,
            HTLEFT = 10,
            HTRIGHT = 11,
            HTTOP = 12,
            HTTOPLEFT = 13,
            HTTOPRIGHT = 14,
            HTBOTTOM = 15,
            HTBOTTOMLEFT = 16,
            HTBOTTOMRIGHT = 17,
            HTBORDER = 18,
            HTREDUCE = HTMINBUTTON,
            HTZOOM = HTMAXBUTTON,
            HTSIZEFIRST = HTLEFT,
            HTSIZELAST = HTBOTTOMRIGHT,
            HTOBJECT = 19,
            HTCLOSE = 20,
            HTHELP = 21,
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (disableScale)
                return IntPtr.Zero;

            switch (msg)
            {
                case WM_NCHITTEST:
                    mousePoint.X = (lParam.ToInt32() & 0xFFFF);
                    mousePoint.Y = (lParam.ToInt32() >> 16);

                    // 窗口左上角  
                    if (mousePoint.Y - Top <= agWidth
                       && mousePoint.X - Left <= agWidth)
                    {
                        handled = true;
                        return new IntPtr((int)HitTest.HTTOPLEFT);
                    }
                    // 窗口左下角      
                    else if (ActualHeight + Top - mousePoint.Y <= agWidth
                       && mousePoint.X - Left <= agWidth)
                    {
                        handled = true;
                        return new IntPtr((int)HitTest.HTBOTTOMLEFT);
                    }
                    // 窗口右上角  
                    else if (mousePoint.Y - Top <= agWidth
                       && ActualWidth + Left - mousePoint.X <= agWidth)
                    {
                        handled = true;
                        return new IntPtr((int)HitTest.HTTOPRIGHT);
                    }
                    // 窗口右下角  
                    else if (ActualWidth + Left - mousePoint.X <= agWidth
                       && ActualHeight + Top - mousePoint.Y <= agWidth)
                    {
                        handled = true;
                        return new IntPtr((int)HitTest.HTBOTTOMRIGHT);
                    }
                    // 窗口左侧  
                    else if (mousePoint.X - Left <= bThickness)
                    {
                        handled = true;
                        return new IntPtr((int)HitTest.HTLEFT);
                    }
                    // 窗口右侧  
                    else if (ActualWidth + Left - mousePoint.X <= bThickness)
                    {
                        handled = true;
                        return new IntPtr((int)HitTest.HTRIGHT);
                    }
                    // 窗口上方  
                    else if (mousePoint.Y - Top <= bThickness)
                    {
                        handled = true;
                        return new IntPtr((int)HitTest.HTTOP);
                    }
                    // 窗口下方  
                    else if (ActualHeight + Top - mousePoint.Y <= bThickness)
                    {
                        handled = true;
                        return new IntPtr((int)HitTest.HTBOTTOM);
                    }
                    break;
            }

            Console.Write($"{handled}");

            Console.Write("{0}", handled);

            return IntPtr.Zero;
        }
        #endregion

        private void TitleBar_Drag(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed && !disableMove)
            {
                DragMove();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (cbFullscreen.IsChecked == true)
            {
                txtRecordSize.Text = $"{SystemParameters.PrimaryScreenWidth}x{SystemParameters.PrimaryScreenHeight}";
                txtOutputX.Text = SystemParameters.PrimaryScreenWidth.ToString();
                txtOutputY.Text = SystemParameters.PrimaryScreenHeight.ToString();
            }
            else
            {
                txtRecordSize.Text = $"{Width - 2}x{Height - 81}";
                txtOutputX.Text = (Convert.ToInt32(txtOutputX.Text) + (Math.Round(Width - 2) - Convert.ToInt32(txtOutputX.Text))).ToString();
                txtOutputY.Text = (Convert.ToInt32(txtOutputY.Text) + (Math.Round(Height - 81) - Convert.ToInt32(txtOutputY.Text))).ToString();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ffmpegProcess != null)
            {
                MessageBox.Show("录制还未完成，请先结束录制再关闭!", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                e.Cancel = true;
            }
        }

        private void cbFullscreen_click(object sender, RoutedEventArgs e)
        {
            if(cbFullscreen.IsChecked == true)
            {
                Height = MinHeight;
                disableScale = true;
            }
            else
            {
                Height = 561;
                disableScale = false;
            }
        }
    }
}


//录制三连
//$"-i temp\\out\\\%03d.jpg -i temp\\palette.png -lavfi /"fps={fps} [x]; [x][1:v] paletteuse/" -y {outFileName}"
//$"-i temp\\out\\%03d.jpg -vf /"fps={fps},scale=320:-1:flags=lanczos,palettegen/" -y temp\\palette.png"
//$"-f gdigrab -framerate {fps} -i desktop -s {resStr} -q 0 temp\\out\\%03d.jpg"