using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HarmonyAnalyser
{
    /// <summary>
    /// Logika interakcji dla klasy PianoKeyboard.xaml
    /// </summary>
    public partial class PianoKeyboard : UserControl
    {
        private const string whiteKeyPathData = "M 0 0 H 270 V 1787 a 23 23 0 0 1 -23 23 H 23 a 23 23 0 0 1 -23 -23 Z";
        private const string blackKeyPathData = "M 0 0 H 135 V 1072 a 23 23 0 0 1 -23 23 H 23 a 23 23 0 0 1 -23 -23 Z";

        private const double whiteKeyWidth = 27;
        private const double whiteKeyHeight = 181;
        private const double blackKeyWidth = 13.5;
        private const double blackKeyHeight = 109.5;

        public PianoKeyboard()
        {
            InitializeComponent();
            DrawKeyboard();
        }

        public class Key
        {
            public ChordManager.Note Note { get; set; }
            public Path Path { get; set; }
            public bool IsBlack { get; set; }
        }

        public List<Key> Keys { get; } = new();

        private void DrawKeyboard()
        {
            string[] notes = { "C", "D", "E", "F", "G", "A", "B" };
            int index = 5;
            int octave = 0;
            bool blackKey = false;

            double x = 0;

            for (int i = 0; i < 88; i++)
            {
                if (!blackKey)
                {
                    Key key = new Key
                    {
                        Note = new ChordManager.Note
                        {
                            Step = notes[index],
                            Octave = octave,
                            Alter = 0
                        },
                        Path = CreateKey(whiteKeyPathData, Colors.White, whiteKeyWidth, whiteKeyHeight),
                        IsBlack = blackKey
                    };

                    Keys.Add(key);

                    Canvas.SetLeft(key.Path, x);
                    PianoCanvas.Children.Add(key.Path);
                }
                else
                {
                    Key key = new Key
                    {
                        Note = new ChordManager.Note
                        {
                            Step = notes[index],
                            Octave = octave,
                            Alter = 1
                        },
                        Path = CreateKey(blackKeyPathData, Colors.Black, blackKeyWidth, blackKeyHeight),
                        IsBlack = blackKey
                    };

                    Keys.Add(key);

                    Canvas.SetLeft(key.Path, x + (whiteKeyWidth - 1) - blackKeyWidth / 2);
                    Panel.SetZIndex(key.Path, 1);
                    PianoCanvas.Children.Add(key.Path);
                }

                if (index != 2 && index != 6 && blackKey == false)
                {
                    blackKey = true;
                }
                else
                {
                    blackKey = false;
                    x += whiteKeyWidth - 1;

                    if (index == notes.Length - 1)
                    {
                        index = 0;
                        octave++;
                    }
                    else
                    {
                        index++;
                    }
                }
            }    
        }

        private Path CreateKey(string pathData, Color color, double width, double height)
        {
            var path = new Path
            {
                Data = Geometry.Parse(pathData),
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Width = width,
                Height = height,
                Stretch = Stretch.Fill
            };

            return path;
        }

        public void HighlightKeys(ChordManager.Chord chord)
        {
            ResetHighlight();

            foreach (var note in chord.Notes)
            {
                var match = Keys.FirstOrDefault(key =>
                key.Note.Step == note.Step &&
                key.Note.Octave == note.Octave &&
                key.Note.Alter == note.Alter);

                if (match != null)
                {
                    var brush = (SolidColorBrush)match.Path.Fill;
                    AnimateKeyColor(match.Path, brush.Color, Colors.LimeGreen);
                }
            }
        }

        public void ResetHighlight()
        {
            foreach (var key in Keys)
            {
                var brush = (SolidColorBrush)key.Path.Fill;
                var targetColor = key.IsBlack ? Colors.Black : Colors.White;
                AnimateKeyColor(key.Path, brush.Color, targetColor);
            }
        }

        private void AnimateKeyColor(Path keyPath, Color from, Color to, int ms = 150)
        {
            var brush = keyPath.Fill as SolidColorBrush;
            if (brush == null) return;

            ColorAnimation animation = new ColorAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(ms),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut}
            };

            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
    }
}