using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace HarmonyAnalyser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MusicRenderer musicRenderer;

        private WindowState _WindowState;
        private WindowStyle _WindowStyle;

        public MainWindow()
        {
            InitializeComponent();

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
                    ChordManager chordManager = new(score);
                    musicRenderer = new(ScoreCanvas, score, chordManager, PianoKeyboard);
                    chordManager.ExtractChords();
                    musicRenderer.DrawScore();
                    PianoKeyboard.ResetHighlight();
                }
            }
        }

        static ScorePartwise LoadMusicXml(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ScorePartwise));

            using (StreamReader reader = new StreamReader(filePath))
            {
                return (ScorePartwise)serializer.Deserialize(reader);
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
            /* if (musicRenderer.ChordSelection != null || musicRenderer.SelectedNote != null)
                musicRenderer.UncheckChord(); */
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
    }
}
