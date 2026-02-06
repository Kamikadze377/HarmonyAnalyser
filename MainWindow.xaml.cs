using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Xml.Serialization;
using System.Windows.Media;
using System.IO.Compression;
using System.Linq;

namespace HarmonyAnalyser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MusicRenderer musicRenderer;
        private ChordManager chordManager;

        private WindowState _WindowState;
        private WindowStyle _WindowStyle;

        public MainWindow()
        {
            InitializeComponent();

            SourceInitialized += OnSourceInitialized;
            musicRenderer = new(ScoreCanvas);
            musicRenderer.DrawStartupScore();
        }

        private void Otworz_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".xml";
            dialog.Filter = "Pliki Music XML (*.mxl *.musicxml *.xml)|*.mxl;*.musicxml;*.xml";

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string filePath = dialog.FileName;
                ScorePartwise score = LoadMusicXml(filePath);

                if (score != null)
                {
                    chordManager = new(score);
                    musicRenderer = new(ScoreCanvas, score, chordManager, PianoKeyboard);
                    chordManager.ExtractSubchords();
                    chordManager.ExtractChords();
                    musicRenderer.DrawScore();
                    PianoKeyboard.ResetHighlight();
                }
            }
        }

        private static ScorePartwise LoadMusicXml(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (ext == ".xml" || ext == ".musicxml")
            {
                try
                {
                    XmlSerializer serializer = new(typeof(ScorePartwise));
                    using StreamReader reader = new(filePath);
                    return (ScorePartwise?)serializer.Deserialize(reader);
                }
                catch
                {
                    return null;
                }
            }

            if (ext == ".mxl")
            {
                try
                {
                    using ZipArchive archive = ZipFile.OpenRead(filePath);

                    var scoreEntry = archive.Entries
                        .FirstOrDefault(e =>
                            string.Equals(Path.GetFileName(e.FullName),
                                          "score.xml",
                                          StringComparison.OrdinalIgnoreCase));

                    if (scoreEntry == null)
                        return null;

                    using var stream = scoreEntry.Open();
                    XmlSerializer serializer = new(typeof(ScorePartwise));
                    using StreamReader reader = new(stream);
                    return (ScorePartwise?)serializer.Deserialize(reader);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private void PianoKeyboardView_Checked(object sender, RoutedEventArgs e)
        {
            if (PianoKeyboard_Viewbox != null)
                PianoKeyboard_Viewbox.Visibility = Visibility.Visible;
        }

        private void PianoKeyboardView_Unchecked(object sender, RoutedEventArgs e)
        {
            PianoKeyboard_Viewbox.Visibility = Visibility.Collapsed;
        }

        private void Zamknij_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Drukuj_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                ScoreCanvas.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                ScoreCanvas.Arrange(new Rect(ScoreCanvas.DesiredSize));

                printDialog.PrintVisual(ScoreCanvas, "Harmony Analyser – partytura");
            }
        }

        private void FullScreen_Checked(object sender, RoutedEventArgs e)
        {
            _WindowState = this.WindowState;
            _WindowStyle = this.WindowStyle;

            this.WindowState = WindowState.Normal;
            this.WindowStyle = WindowStyle.None;
            this.WindowState = WindowState.Maximized;
        }

        private void FullScreen_Unchecked(object sender, RoutedEventArgs e)
        {
            this.WindowStyle = _WindowStyle;
            this.WindowState = _WindowState;
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.WindowStyle = _WindowStyle;
                this.WindowState = _WindowState;

                if (FullScreen.IsChecked)
                {
                    FullScreen.IsChecked = false;
                }
            }

            if (e.Key == System.Windows.Input.Key.F11)
            {
                if (!FullScreen.IsChecked)
                {
                    _WindowState = this.WindowState;
                    _WindowStyle = this.WindowStyle;

                    FullScreen.IsChecked = true;

                    this.WindowState = WindowState.Normal;
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;
                }
                else
                {
                    this.WindowStyle = _WindowStyle;
                    this.WindowState = _WindowState;

                    if (FullScreen.IsChecked)
                    {
                        FullScreen.IsChecked = false;
                    }
                }
            }

            base.OnKeyDown(e);
        }

        private void MainArea_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            /* if (musicRenderer.SubchordSelection != null || musicRenderer.SelectedNote != null)
                musicRenderer.UncheckSubchord(); */
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            var source = (HwndSource)PresentationSource.FromVisual(this);
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case NativeHelpers.WM_NCHITTEST:
                    if (NativeHelpers.IsSnapLayoutEnabled())
                    {
                        var point = PointFromScreen(new Point(lParam.ToInt32() & 0xFFFF, lParam.ToInt32() >> 16));
                        if (WpfHelpers.GetElementBoundsRelativeToWindow(maximizeRestoreButton, this).Contains(point))
                        {
                            handled = true;
                            maximizeRestoreButton.Background = (Brush)App.Current.Resources["TitleBarButtonHoverBackground"];
                            maximizeRestoreButton.Foreground = (Brush)App.Current.Resources["TitleBarButtonHoverForeground"];
                            return new IntPtr(NativeHelpers.HTMAXBUTTON);
                        }
                        else
                        {
                            maximizeRestoreButton.Background = (Brush)App.Current.Resources["TitleBarButtonBackground"];
                            maximizeRestoreButton.Foreground = (Brush)App.Current.Resources["TitleBarButtonForeground"];
                        }
                    }
                    break;
                case NativeHelpers.WM_NCLBUTTONDOWN:
                    if (NativeHelpers.IsSnapLayoutEnabled())
                    {
                        if (wParam.ToInt32() == NativeHelpers.HTMAXBUTTON)
                        {
                            handled = true;
                            maximizeRestoreButton.Background = (Brush)App.Current.Resources["TitleBarButtonPressedBackground"];
                            maximizeRestoreButton.Foreground = (Brush)App.Current.Resources["TitleBarButtonPressedForeground"];
                        }
                    }
                    break;
                case NativeHelpers.WM_NCLBUTTONUP:
                    if (NativeHelpers.IsSnapLayoutEnabled())
                    {
                        if (wParam.ToInt32() == NativeHelpers.HTMAXBUTTON)
                        {
                            maximizeRestoreButton.Background = (Brush)App.Current.Resources["TitleBarButtonBackground"];
                            maximizeRestoreButton.Foreground = (Brush)App.Current.Resources["TitleBarButtonForeground"];
                            ToggleWindowState();
                        }
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        public void ToggleWindowState()
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }

        private void maximizeRestoreButton_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            maximizeRestoreButton.ToolTip = WindowState == WindowState.Normal ? "Maximize" : "Restore";
        }
    }
}
