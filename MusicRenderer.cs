using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace HarmonyAnalyser
{
    public class MusicRenderer
    {
        private readonly Canvas _canvas;
        private readonly ScorePartwise _score;
        private readonly ChordManager _chordManager;
        private readonly PianoKeyboard _pianoKeyboard;

        private readonly double _xOffset = 102;
        private readonly double _yOffset = 186;
        private readonly double _xLimit = 1336;
        private readonly double _systemSpacing = 227;

        private NoteElement _selectedNote;
        public NoteElement SelectedNote => _selectedNote;

        private Border _chordSelection;
        public Border ChordSelection => _chordSelection;

        public MusicRenderer(Canvas canvas)
        {
            _canvas = canvas;
        }

        public MusicRenderer(Canvas canvas, ScorePartwise score, ChordManager chordManager, PianoKeyboard pianoKeyboard)
        {
            _canvas = canvas;
            _score = score;
            _chordManager = chordManager;
            _pianoKeyboard = pianoKeyboard;
        }

        public class NoteElement
        {
            public List<TextBlock> Components { get; set; } = new();
            public ChordManager.Note Note { get; set; }
            public ChordManager.Chord Chord { get; set; }
            public TextBlock LedgerLines { get; set; }
            public List<NoteElement> HiddenNotes { get; set; } = new();

            public bool IsChecked { get; set; }
            public bool IsAnimated { get; set; }
            public bool IsExpanded { get; set; }
        }

        public List<NoteElement> NoteElements = new List<NoteElement>();

        private void UpdateCanvasSize() // Dynamiczny rozmiar obszaru nutowego
        {
            double maxX = 0, maxY = 0;

            foreach (var child in _canvas.Children.OfType<FrameworkElement>())
            {
                double x = Canvas.GetLeft(child);
                double y = Canvas.GetTop(child);

                if (double.IsNaN(x)) x = 0;
                if (double.IsNaN(y)) y = 0;

                double right = x + child.RenderSize.Width;
                double bottom = y + child.RenderSize.Height;

                if (right > maxX) maxX = right;
                if (bottom > maxY) maxY = bottom;
            }

            _canvas.Width = maxX;
            _canvas.Height = maxY + 250;
        }

        public void DrawScore()
        {
            _canvas.Children.Clear();   // Wyczyszczenie arkusza

            double xOffset = _xOffset;
            double yOffset = _yOffset;
            double xSpaceOffset = xOffset + 60;
            double xShifted = xSpaceOffset;
            double xLimit = _xLimit;
            double systemSpacing = _systemSpacing;
            double measureSpacing;
            double noteSpacing = 36;
            double chordSpacing = 46;
            double beatDuration = 1;
            double chordShifted;

            int noteIndex;
            int notePoint;
            int barIndex;
            int timeIndex = 0;
            int chordIndex = 0;

            foreach (var part in _score.Parts)
            {
                barIndex = 0;

                foreach (var measure in part.Measures)
                {
                    // Przeliczenie długości taktu

                    int measureDuration = 0;

                    for (int i = 0; i < measure.Notes.Count; i++)
                    {
                        if (measure.Notes[i].Staff == 1)
                        {
                            if (measure.Notes[i].Chord == null)
                            {
                                measureDuration += measure.Notes[i].Duration;
                            }
                        }
                    }

                    if (measure.Attributes != null)
                    {
                        // Dodanie klucza wiolinowego

                        if (measure.Attributes.Clefs.Count > 0)
                        {
                            if (measure.Attributes.Clefs[0].Sign == "G" && measure.Attributes.Clefs[0].Line == 2)
                            {
                                xShifted += 5;
                                AddTrebleClef(xShifted, yOffset);
                                DrawStaff(xShifted - 5, xShifted, yOffset);
                            }
                        }

                        // Dodanie metrum

                        if (measure.Attributes.Time != null)
                        {
                            timeIndex = barIndex;

                            xShifted += 36;
                            AddTimeSignature(xShifted, yOffset, measure.Attributes.Time.Beats, measure.Attributes.Time.BeatType);
                            xShifted += 27;
                            DrawStaff(xShifted - 63, xShifted, yOffset);
                        }
                    }

                    if (barIndex % 4 == 0)
                    {
                        if (barIndex != 0)
                        {
                            AddBarNumber(xShifted, yOffset, barIndex + 1);
                            xShifted += 5;
                            AddTrebleClef(xShifted, yOffset);
                            xShifted += 36;

                            DrawStaff(xShifted - 41, xShifted, yOffset);
                        }

                        noteSpacing = (xLimit - xShifted) / (part.Measures[timeIndex].Attributes.Time.Beats * 4);
                        measureSpacing = noteSpacing * part.Measures[timeIndex].Attributes.Time.Beats;
                        chordSpacing = measureSpacing / measureDuration;
                        beatDuration = measureDuration / part.Measures[timeIndex].Attributes.Time.Beats;
                    }

                    chordShifted = xShifted;

                    // Dodanie nut 

                    noteIndex = 0;
                    notePoint = 0;

                    foreach (var note in measure.Notes)
                    {
                        if (note.Staff == 1)
                        {
                            if (note.Chord == null)
                            {
                                if (noteIndex + 1 < measure.Notes.Count)
                                {
                                    if (measure.Notes[noteIndex + 1].Chord == null)
                                    {
                                        if (note.Dot != null)
                                        {
                                            ChordManager.Note mainVoiceNote = new ChordManager.Note { Step = note.Pitch.Step, Octave = note.Pitch.Octave, Point = notePoint, MeasureNumber = measure.Number };
                                            NoteElement noteElement = new NoteElement { Note = mainVoiceNote };
                                            NoteElements.Add(noteElement);
                                            AddNote(xShifted, yOffset, note.Pitch.Step, note.Pitch.Octave, note.Type, noteElement, true);
                                        }
                                        else
                                        {
                                            ChordManager.Note mainVoiceNote = new ChordManager.Note { Step = note.Pitch.Step, Octave = note.Pitch.Octave, Point = notePoint, MeasureNumber = measure.Number };
                                            NoteElement noteElement = new NoteElement { Note = mainVoiceNote };
                                            NoteElements.Add(noteElement);
                                            AddNote(xShifted, yOffset, note.Pitch.Step, note.Pitch.Octave, note.Type, noteElement);
                                        }

                                        notePoint += note.Duration;
                                        xShifted += noteSpacing * (note.Duration / beatDuration);

                                        DrawStaff(xShifted - noteSpacing * (note.Duration / beatDuration), xShifted, yOffset);
                                    }
                                }
                            }
                            else
                            {
                                if (noteIndex + 1 < measure.Notes.Count)
                                {
                                    if (measure.Notes[noteIndex + 1].Chord == null)
                                    {
                                        if (note.Dot != null)
                                        {
                                            ChordManager.Note mainVoiceNote = new ChordManager.Note { Step = note.Pitch.Step, Octave = note.Pitch.Octave, Point = notePoint, MeasureNumber = measure.Number };
                                            NoteElement noteElement = new NoteElement { Note = mainVoiceNote };
                                            NoteElements.Add(noteElement);
                                            AddNote(xShifted, yOffset, note.Pitch.Step, note.Pitch.Octave, note.Type, noteElement, true);
                                        }
                                        else
                                        {
                                            ChordManager.Note mainVoiceNote = new ChordManager.Note { Step = note.Pitch.Step, Octave = note.Pitch.Octave, Point = notePoint, MeasureNumber = measure.Number };
                                            NoteElement noteElement = new NoteElement { Note = mainVoiceNote };
                                            NoteElements.Add(noteElement);
                                            AddNote(xShifted, yOffset, note.Pitch.Step, note.Pitch.Octave, note.Type, noteElement);
                                        }

                                        notePoint += note.Duration;
                                        xShifted += noteSpacing * (note.Duration / beatDuration);

                                        DrawStaff(xShifted - noteSpacing * (note.Duration / beatDuration), xShifted, yOffset);
                                    }
                                }
                            }
                            noteIndex++;
                        }
                    }

                    // Dodanie akordów

                    for (int i = 0; i < measureDuration; i++)
                    {
                        for (int j = chordIndex; j < _chordManager.Chords.Count; j++)
                        {
                            if (_chordManager.Chords[j].Point == i && _chordManager.Chords[j].MeasureNumber == barIndex + 1)
                            {
                                AddChord(chordShifted, yOffset, _chordManager.Chords[j]);

                                // Powiązanie akordów z wyświetlanymi nutami

                                for (int k = 0; k < NoteElements.Count; k++)
                                {
                                    if (NoteElements[k].Note.Point == i && NoteElements[k].Note.MeasureNumber == barIndex + 1)
                                    {
                                        NoteElements[k].Chord = _chordManager.Chords[j];
                                        _chordManager.Chords[j].NoteElement = NoteElements[k];
                                        break;
                                    }
                                    else if (NoteElements[k].Note.Point > i && NoteElements[k].Note.MeasureNumber == barIndex + 1)
                                    {
                                        ChordManager.Note note = new ChordManager.Note
                                        {
                                            Step = NoteElements[k - 1].Note.Step,
                                            Octave = NoteElements[k - 1].Note.Octave,
                                            Alter = NoteElements[k - 1].Note.Alter,
                                            MeasureNumber = NoteElements[k - 1].Note.MeasureNumber,
                                            Point = i,
                                        };

                                        NoteElement noteElement = new NoteElement
                                        {
                                            Note = note,
                                            Chord = _chordManager.Chords[j]
                                        };

                                        _chordManager.Chords[j].NoteElement = noteElement;
                                        NoteElements[k - 1].HiddenNotes.Add(noteElement);
                                        AddNote(chordShifted, yOffset, noteElement.Note.Step, noteElement.Note.Octave, null, noteElement, false, true);
                                        
                                        foreach (var component in noteElement.Components)
                                            component.Visibility = Visibility.Hidden;

                                        if (noteElement.LedgerLines != null)
                                            noteElement.LedgerLines.Visibility = Visibility.Hidden;

                                        break;
                                    }
                                    else if (k == NoteElements.Count - 1)
                                    {
                                        ChordManager.Note note = new ChordManager.Note
                                        {
                                            Step = NoteElements[k].Note.Step,
                                            Octave = NoteElements[k].Note.Octave,
                                            Alter = NoteElements[k].Note.Alter,
                                            MeasureNumber = NoteElements[k].Note.MeasureNumber,
                                            Point = i,
                                        };

                                        NoteElement noteElement = new NoteElement
                                        {
                                            Note = note,
                                            Chord = _chordManager.Chords[j]
                                        };

                                        _chordManager.Chords[j].NoteElement = noteElement;
                                        NoteElements[k].HiddenNotes.Add(noteElement);
                                        AddNote(chordShifted, yOffset, noteElement.Note.Step, noteElement.Note.Octave, null, noteElement, false, true);

                                        foreach (var component in noteElement.Components)
                                            component.Visibility = Visibility.Hidden;

                                        if (noteElement.LedgerLines != null)
                                            noteElement.LedgerLines.Visibility = Visibility.Hidden;
                                    }
                                }

                                chordIndex++;
                                break;
                            }
                        }

                        chordShifted += chordSpacing;
                    }

                    // Dodanie kreski taktowej

                    if (barIndex < part.Measures.Count - 1)
                    {
                        AddBarline(xShifted, yOffset);

                        if (Math.Round(xShifted) >= xLimit)
                        {
                            yOffset += systemSpacing;
                            xShifted = xOffset;
                        }

                        barIndex++;
                    }
                    else
                    {
                        AddFinalBarline(xShifted, yOffset);
                    }
                }
            }

            UpdateCanvasSize();
        }

        public void DrawStartupScore()
        {
            _canvas.Children.Clear();   // Wyczyszczenie arkusza

            double xSpaceOffset = 162;
            double xShifted = xSpaceOffset;
            double xOffset = 102;
            double yOffset = 186;
            double xLimit = 1336;
            double systemSpacing = 227;

            // System 1

            for (int i = 0; i < 5; i++)
            {
                Line line = new Line
                {
                    X1 = xSpaceOffset,
                    Y1 = yOffset + (i * 10),
                    X2 = xLimit,
                    Y2 = yOffset + (i * 10),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                _canvas.Children.Add(line);
            }

            xShifted += 5;

            AddTrebleClef(xShifted, yOffset);

            xShifted += 36;

            AddTimeSignature(xShifted, yOffset, 4, 4);

            xShifted += 28;

            AddChord(xShifted, yOffset);

            // 4 takty na system

            for (int i = 0; i < 4; i++)
            {
                xShifted += 133;
                AddRest(xShifted, yOffset, 1);
                xShifted += 143;
                AddBarline(xShifted, yOffset);
            }

            yOffset += systemSpacing;

            // Systemy 2-8

            for (int i = 1; i < 8; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Line line = new Line
                    {
                        X1 = xOffset,
                        Y1 = yOffset + (j * 10),
                        X2 = xLimit,
                        Y2 = yOffset + (j * 10),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    _canvas.Children.Add(line);
                }

                AddBarNumber(xOffset, yOffset, i * 4 + 1);

                xShifted = xOffset + 5;

                AddTrebleClef(xShifted, yOffset);

                xShifted += 40;

                // 4 takty na linię

                for (int j = 0; j < 4; j++)
                {
                    if (i < 7)
                    {
                        xShifted += 145;
                        AddRest(xShifted, yOffset, 1);
                        xShifted += 152;
                        AddBarline(xShifted, yOffset);
                    }
                    else if (i == 7 && j < 3)
                    {
                        xShifted += 145;
                        AddRest(xShifted - 13, yOffset, 1);
                        xShifted += 152;
                        AddBarline(xShifted - 13, yOffset);
                    }
                    else
                    {
                        xShifted += 145;
                        AddRest(xShifted - 13, yOffset, 1);
                        xShifted += 152;
                        AddFinalBarline(xLimit, yOffset);
                    }
                }

                yOffset += systemSpacing;
            }

            UpdateCanvasSize();
        }

        private TextBlock AddSymbol(string text, double left, double top, double fontSize, bool transparent = false)
        {
            TextBlock symbol = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Bravura"),
                FontSize = fontSize,
                Foreground = new SolidColorBrush(Colors.Black),
                Opacity = transparent ? 0.25 : 1.0
            };

            Canvas.SetLeft(symbol, left);
            Canvas.SetTop(symbol, top);
            _canvas.Children.Add(symbol);

            return symbol;
        }

        public void AddTrebleClef(double x, double y)
        {
            TextBlock clef = new TextBlock
            {
                Text = "\uE050",
                FontFamily = new FontFamily("Bravura"),
                FontSize = 40,
                Foreground = Brushes.Black
            };

            Canvas.SetLeft(clef, x);
            Canvas.SetTop(clef, y - 50);

            _canvas.Children.Add(clef);
        }

        public void AddTimeSignature(double x, double y, int number, int type)
        {
            // Liczba

            if (number == 4)
            {
                TextBlock numerator = new TextBlock
                {
                    Text = "\uE084",
                    FontFamily = new FontFamily("Bravura"),
                    FontSize = 40,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(numerator, x);
                Canvas.SetTop(numerator, y - 70);

                _canvas.Children.Add(numerator);
            }
            else if (number == 3)
            {
                TextBlock numerator = new TextBlock
                {
                    Text = "\uE083",
                    FontFamily = new FontFamily("Bravura"),
                    FontSize = 40,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(numerator, x);
                Canvas.SetTop(numerator, y - 70);

                _canvas.Children.Add(numerator);
            }
            else
            {
                TextBlock numerator = new TextBlock
                {
                    Text = "\uE080",
                    FontFamily = new FontFamily("Bravura"),
                    FontSize = 40,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(numerator, x);
                Canvas.SetTop(numerator, y - 70);

                _canvas.Children.Add(numerator);
            }

            // Wartość rytmiczna

            if (type == 4)
            {
                TextBlock denominator = new TextBlock
                {
                    Text = "\uE084",
                    FontFamily = new FontFamily("Bravura"),
                    FontSize = 40,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(denominator, x);
                Canvas.SetTop(denominator, y - 50);

                _canvas.Children.Add(denominator);
            }
            else
            {
                TextBlock denominator = new TextBlock
                {
                    Text = "\uE080",
                    FontFamily = new FontFamily("Bravura"),
                    FontSize = 40,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(denominator, x);
                Canvas.SetTop(denominator, y - 50);

                _canvas.Children.Add(denominator);
            }
        }

        public void AddNote(double x, double y, string pitch, int octave, string type, NoteElement note, bool hasDot = false, bool transparent = false)
        {
            string[] notes = { "C", "D", "E", "F", "G", "A", "B" };

            // Dla klucza wiolinowego

            switch(octave)
            {
                case 3:
                    for (int i = 0; i < notes.Length; i++)
                    {
                        if (pitch == notes[i])
                        {
                            // Linie dodane

                            TextBlock ledgerLines;

                            if (i == 6)
                                ledgerLines = AddSymbol("\uE010", x + 11, y - 10, 40, transparent);
                            else if (i == 4 || i == 5)
                                ledgerLines = AddSymbol("\uE011", x + 11, y - 5, 40, transparent);
                            else if (i == 2 || i == 3)
                                ledgerLines = AddSymbol("\uE012", x + 11, y, 40, transparent);
                            else
                                ledgerLines = AddSymbol("\uE013", x + 11, y + 5, 40, transparent);

                            note.LedgerLines = ledgerLines;

                            // Główka

                            TextBlock noteHead;

                            switch (type)
                            {
                                case "quarter":
                                    noteHead = AddSymbol("\uE0A4", x + 15, y + 5 - i * 5, 40, transparent);
                                    note.Components.Add(noteHead);
                                    break;
                                case "half":
                                    noteHead = AddSymbol("\uE0A3", x + 15, y + 5 - i * 5, 40, transparent);
                                    note.Components.Add(noteHead);
                                    break;
                                case "whole":
                                    noteHead = AddSymbol("\uE0A2", x + 12.5, y + 5 - i * 5, 40, transparent);
                                    note.Components.Add(noteHead);
                                    break;
                                default:
                                    goto case "quarter";
                            }

                            // Laseczka

                            if (type != "whole" && type != null)
                            {
                                TextBlock stem = AddSymbol("\uE210", x + 26, y + 5 - i * 5, 40, transparent);
                                note.Components.Add(stem);
                            }

                            // Flaga — do uzupełnienia o drobniejsze wartości (szesnastki, trzydziestodwójki itd)

                            if (type == "eighth")
                            {
                                TextBlock flag = AddSymbol("\uE240", x + 26, y - 30 - i * 5, 40, transparent);
                                note.Components.Add(flag);
                            }

                            // Kropka — do uzupełnienia o podwójne kropki

                            if (hasDot)
                            {
                                TextBlock dot = AddSymbol("\uE1E7", x + 32, y + 4 - i * 5, 40, transparent);
                                note.Components.Add(dot);
                            }
                        }
                    }
                    break;

                case 4:
                    for (int i = 0; i < notes.Length; i++)
                    {
                        if (pitch == notes[i])
                        {
                            // Linie dodane

                            TextBlock ledgerLines;

                            if (i == 0)
                            {
                                ledgerLines = AddSymbol("\uE010", x + 11, y - 10, 40, transparent);
                                note.LedgerLines = ledgerLines;
                            }

                            // Główka

                            TextBlock noteHead;

                            switch (type)
                            {
                                case "quarter":
                                    noteHead = AddSymbol("\uE0A4", x + 15, y - 30 - i * 5, 40, transparent);
                                    note.Components.Add(noteHead);
                                    break;
                                case "half":
                                    noteHead = AddSymbol("\uE0A3", x + 15, y - 30 - i * 5, 40, transparent);
                                    note.Components.Add(noteHead);
                                    break;
                                case "whole":
                                    noteHead = AddSymbol("\uE0A2", x + 12.5, y - 30 - i * 5, 40, transparent);
                                    note.Components.Add(noteHead);
                                    break;
                                default:
                                    goto case "quarter";
                            }

                            if (pitch == "B")
                            {
                                // Laseczka

                                if (type != "whole")
                                {
                                    TextBlock stem = AddSymbol("\uE210", x + 16, y + 5 - i * 5, 40, transparent);
                                    note.Components.Add(stem);
                                }

                                // Flaga – do uzupełnienia o drobniejsze wartości (szesnastki, trzydziestodwójki itd)

                                if (type == "eighth")
                                {
                                    TextBlock flag = AddSymbol("\uE241", x + 15, y + 6 - i * 5, 40, transparent);
                                    note.Components.Add(flag);
                                }
                            }
                            else
                            {
                                // Laseczka

                                if (type != "whole" && type != null)
                                {
                                    TextBlock stem = AddSymbol("\uE210", x + 26, y - 30 - i * 5, 40, transparent);
                                    note.Components.Add(stem);
                                }

                                // Flaga – do uzupełnienia o drobniejsze wartości (szesnastki, trzydziestodwójki itd)

                                if (type == "eighth")
                                {
                                    TextBlock flag = AddSymbol("\uE240", x + 26, y - 65 - i * 5, 40, transparent);
                                    note.Components.Add(flag);
                                }
                            }

                            // Kropka

                            if (hasDot)
                            {
                                TextBlock dot = AddSymbol("\uE1E7", x + 32, y - 31 - i * 5, 40, transparent);
                                note.Components.Add(dot);
                            }
                        }
                    }
                    break;

                case 5:
                    for (int i = 0; i < notes.Length; i++)
                    {
                        if (pitch == notes[i])
                        {
                            // Linie dodane

                            TextBlock ledgerLines;

                            if (i == 5 || i == 6)
                            {
                                ledgerLines = AddSymbol("\uE010", x + 11, y - 70, 40, transparent);
                                note.LedgerLines = ledgerLines;
                            }

                            // Główka

                            TextBlock noteHead;

                            switch (type)
                            {
                                case "quarter":
                                    noteHead = AddSymbol("\uE0A4", x + 15, y - 65 - i * 5, 40, transparent);
                                    note.Components.Add(noteHead);
                                    break;
                                case "half":
                                    noteHead = AddSymbol("\uE0A3", x + 15, y - 65 - i * 5, 40, transparent);
                                    note.Components.Add(noteHead);
                                    break;
                                case "whole":
                                    noteHead = AddSymbol("\uE0A2", x + 12.5, y - 65 - i * 5, 40, transparent);
                                    note.Components.Add(noteHead);
                                    break;
                                default:
                                    goto case "quarter";
                            }

                            // Laseczka

                            if (type != "whole" && type != null)
                            {
                                TextBlock stem = AddSymbol("\uE210", x + 16, y - 30 - i * 5, 40, transparent);
                                note.Components.Add(stem);
                            }

                            // Flaga – do uzupełnienia o drobniejsze wartości (szesnastki, trzydziestodwójki itd)

                            if (type == "eighth")
                            {
                                TextBlock flag = AddSymbol("\uE241", x + 15, y - 29 - i * 5, 40, transparent);
                                note.Components.Add(flag);
                            }

                            // Kropka

                            if (hasDot)
                            {
                                TextBlock dot = AddSymbol("\uE1E7", x + 32, y - 66 - i * 5, 40, transparent);
                                note.Components.Add(dot);
                            }
                        }
                    }
                    break;

                case 6:
                    for (int i = 0; i < notes.Length; i++)
                    {
                        if (pitch == notes[i])
                        {
                            // Linie dodane

                            TextBlock ledgerLines;

                            if (i == 0 || i == 1)
                                ledgerLines = AddSymbol("\uE011", x + 11, y - 75, 40, transparent);
                            else if (i == 2 || i == 3)
                                ledgerLines = AddSymbol("\uE012", x + 11, y - 80, 40, transparent);
                            else if (i == 4 || i == 5)
                                ledgerLines = AddSymbol("\uE013", x + 11, y - 85, 40, transparent);
                            else
                                ledgerLines = AddSymbol("\uE014", x + 11, y - 90, 40, transparent);

                            note.LedgerLines = ledgerLines;

                            // Główka

                            TextBlock noteHead;

                            switch (type)
                            {
                                case "quarter":
                                    noteHead = AddSymbol("\uE0A4", x + 15, y - 100 - i * 5, 40, transparent);
                                    note.Components.Add(noteHead);
                                    break;
                                case "half":
                                    noteHead = AddSymbol("\uE0A3", x + 15, y - 100 - i * 5, 40, transparent);
                                    note.Components.Add(noteHead);
                                    break;
                                case "whole":
                                    noteHead = AddSymbol("\uE0A2", x + 12.5, y - 100 - i * 5, 40, transparent);
                                    note.Components.Add(noteHead);
                                    break;
                                default:
                                    goto case "quarter";
                            }

                            // Laseczka

                            if (type != "whole" && type != null)
                            {
                                TextBlock stem = AddSymbol("\uE210", x + 16, y - 65 - i * 5, 40, transparent);
                                note.Components.Add(stem);
                            }

                            // Flaga – do uzupełnienia o drobniejsze wartości (szesnastki, trzydziestodwójki itd)

                            if (type == "eighth")
                            {
                                TextBlock flag = AddSymbol("\uE241", x + 15, y - 64 - i * 5, 40, transparent);
                                note.Components.Add(flag);
                            }

                            // Kropka

                            if (hasDot)
                            {
                                TextBlock dot = AddSymbol("\uE1E7", x + 32, y - 101 - i * 5, 40, transparent);
                                note.Components.Add(dot);
                            }
                        }
                    }
                    break;

                default:
                    break;
            }

            foreach (var component in note.Components)
            {
                Panel.SetZIndex(component, 1);

                component.MouseLeftButtonDown += (s, e) => 
                {
                    if (_chordSelection != null && _chordSelection != note.Chord.Selection && _selectedNote != null && _selectedNote != note)
                        UncheckChord();

                    if (e.ClickCount == 1)
                        CheckChord(note.Chord);
                    else if (e.ClickCount == 2)
                        CheckChord(note.Chord, true);
                };
            }
        }

        public void AddRest(double x, double y, int value)
        {
            if (value == 1)
            {
                TextBlock rest = new TextBlock
                {
                    Text = "\uE4E3",
                    FontFamily = new FontFamily("Bravura"),
                    FontSize = 40,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(rest, x);
                Canvas.SetTop(rest, y - 70);

                _canvas.Children.Add(rest);
            }
        }

        public void AddBarline(double x, double y)
        {
            Line barLine = new Line
            {
                X1 = x,
                Y1 = y,
                X2 = x,
                Y2 = y + 40,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            _canvas.Children.Add(barLine);
        }

        public void AddFinalBarline(double x, double y)
        {
            Line boldLine = new Line
            {
                X1 = x - 3,
                Y1 = y,
                X2 = x - 3,
                Y2 = y + 40,
                Stroke = Brushes.Black,
                StrokeThickness = 6
            };
            _canvas.Children.Add(boldLine);

            Line thinLine = new Line
            {
                X1 = x - 11,
                Y1 = y,
                X2 = x - 11,
                Y2 = y + 40,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            _canvas.Children.Add(thinLine);
        }

        public void AddBarNumber(double x, double y, int barNumber)
        {
            int number = barNumber;
            int numberOfDigits = 0;

            while (number > 0)
            {
                numberOfDigits++;
                number = (number - number % 10) / 10;
            }

            while (barNumber > 0)
            {
                TextBlock digit = new TextBlock
                {
                    Text = (barNumber % 10).ToString(),
                    FontFamily = new FontFamily("Century"),
                    FontSize = 16,
                    FontStyle = FontStyles.Italic,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(digit, x + (numberOfDigits - 1) * 8);
                Canvas.SetTop(digit, y - 38);

                _canvas.Children.Add(digit);

                numberOfDigits--;
                barNumber = (barNumber - barNumber % 10) / 10;
            }
        }

        public void AddChord(double x, double y)
        {
            TextBlock chord = new TextBlock
            {
                Text = "N.C.",
                FontFamily = new FontFamily("Century"),
                FontSize = 25,
                Foreground = Brushes.Black
            };

            Canvas.SetLeft(chord, x + 12);
            Canvas.SetTop(chord, y - 66);

            _canvas.Children.Add(chord);
        }

        public void AddChord(double x, double y, ChordManager.Chord chord)
        {
            TextBlock chordName = new TextBlock
            {
                Text = chord.Name,
                FontFamily = new FontFamily("Century"),
                FontSize = 25,
                Foreground = Brushes.Black,
                Margin = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsHitTestVisible = false
            };

            Border border = new Border
            {
                Child = chordName,
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(1.25),
                Background = Brushes.Transparent
            };

            chord.Selection = border;

            Panel.SetZIndex(chord.Selection, 2);

            chord.Selection.MouseEnter += (s, e) =>
            {
                if (_chordSelection != border)
                    AnimateBrush((SolidColorBrush)border.BorderBrush, Colors.Transparent, Colors.Black);
            };

            chord.Selection.MouseLeave += (s, e) =>
            {
                if (_chordSelection != border)
                    AnimateBrush((SolidColorBrush)border.BorderBrush, Colors.Black, Colors.Transparent);
            };

            chord.Selection.MouseLeftButtonDown += (s, e) =>
            {
                if (_chordSelection != null && _chordSelection != chord.Selection && _selectedNote != null && _selectedNote != chord.NoteElement)
                    UncheckChord();

                if (e.ClickCount == 1)
                    CheckChord(chord);
                else if (e.ClickCount == 2)
                    CheckChord(chord, true);
            };

            Canvas.SetLeft(chord.Selection, x + 2);
            Canvas.SetTop(chord.Selection, y - 66);

            _canvas.Children.Add(chord.Selection);
        }

        public void CheckChord(ChordManager.Chord chord, bool doubleClick = false)
        {
            _chordSelection = chord.Selection;
            if (!chord.IsChecked)
            {
                AnimateBrush((SolidColorBrush)chord.Selection.BorderBrush, ((SolidColorBrush)chord.Selection.BorderBrush).Color, Colors.LimeGreen);
                _pianoKeyboard.HighlightKeys(chord);
            }

            _selectedNote = chord.NoteElement;

            if (!_selectedNote.IsChecked)
                foreach (var component in _selectedNote.Components)
                    AnimateBrush((SolidColorBrush)component.Foreground, ((SolidColorBrush)component.Foreground).Color, Colors.LimeGreen);

            _selectedNote.IsChecked = true;
            _selectedNote.Chord.IsChecked = true;

            if (doubleClick)
            {
                if (_selectedNote.HiddenNotes != null && _selectedNote.HiddenNotes.Count > 0 && !_selectedNote.IsExpanded)
                    AnimateHiddenNotes(_selectedNote, true);
                else if (_selectedNote.HiddenNotes != null && _selectedNote.HiddenNotes.Count > 0 && _selectedNote.IsExpanded)
                    AnimateHiddenNotes(_selectedNote, false);
            }
        }

        public void UncheckChord()
        {
            if (_chordSelection != null)
            {
                AnimateBrush((SolidColorBrush)_chordSelection.BorderBrush, ((SolidColorBrush)_chordSelection.BorderBrush).Color, Colors.Transparent);
                _chordSelection = null;
            }
            _pianoKeyboard.ResetHighlight();

            if (_selectedNote != null)
            {
                _selectedNote.IsChecked = false;
                _selectedNote.Chord.IsChecked = false;  

                foreach (var component in _selectedNote.Components)
                AnimateBrush((SolidColorBrush)component.Foreground, ((SolidColorBrush)component.Foreground).Color, Colors.Black);

                if (_selectedNote.HiddenNotes != null && _selectedNote.HiddenNotes.Count > 0)
                    AnimateHiddenNotes(_selectedNote, false);

                _selectedNote = null;
            }
        }

        private void AnimateBrush(SolidColorBrush brush, Color from, Color to, int ms = 150)
        {
            ColorAnimation animation = new ColorAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(ms),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        private void AnimateHiddenNotes(NoteElement mainNote, bool show, int ms = 400)
        {
            if (mainNote.IsAnimated)
                return;

            mainNote.IsAnimated = true;

            var mainNoteHead = mainNote.Components[0];

            foreach (var hiddenNote in mainNote.HiddenNotes)
            {
                var hiddenNoteHead = hiddenNote.Components[0];

                double mainX = Canvas.GetLeft(mainNoteHead);
                double mainY = Canvas.GetTop(mainNoteHead);

                double targetX = Canvas.GetLeft(hiddenNoteHead);
                double targetY = Canvas.GetTop(hiddenNoteHead);

                if (show)
                {
                    // Pozycja startowa

                    Canvas.SetLeft(hiddenNoteHead, mainX);
                    Canvas.SetTop(hiddenNoteHead, mainY);
                    hiddenNoteHead.Visibility = Visibility.Visible;

                    // Przesunięcie

                    DoubleAnimation moveX = new DoubleAnimation(mainX, targetX, TimeSpan.FromMilliseconds(ms))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    DoubleAnimation moveY = new DoubleAnimation(mainY, targetY, TimeSpan.FromMilliseconds(ms))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };

                    moveY.Completed += (s, e) =>
                    {
                        mainNote.IsAnimated = false;
                        mainNote.IsExpanded = true;

                        if (!mainNote.IsChecked && !mainNote.IsExpanded)
                            AnimateHiddenNotes(mainNote, false);
                    };

                    hiddenNoteHead.BeginAnimation(Canvas.LeftProperty, moveX);
                    hiddenNoteHead.BeginAnimation(Canvas.TopProperty, moveY);
                }
                else
                {
                    // Powrót do pozycji startowej

                    DoubleAnimation moveX = new DoubleAnimation(targetX, mainX, TimeSpan.FromMilliseconds(ms))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    DoubleAnimation moveY = new DoubleAnimation(targetY, mainY, TimeSpan.FromMilliseconds(ms))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };

                    moveY.Completed += (s, e) =>
                    {
                        hiddenNoteHead.Visibility = Visibility.Hidden;

                        Canvas.SetLeft(hiddenNoteHead, targetX);
                        Canvas.SetTop(hiddenNoteHead, targetY);

                        hiddenNoteHead.BeginAnimation(Canvas.LeftProperty, null);
                        hiddenNoteHead.BeginAnimation(Canvas.TopProperty, null);

                        mainNote.IsAnimated = false;
                        mainNote.IsExpanded = false;

                        if (mainNote.IsChecked && mainNote.IsExpanded)
                            AnimateHiddenNotes(mainNote, true);
                    };

                    hiddenNoteHead.BeginAnimation(Canvas.LeftProperty, moveX);
                    hiddenNoteHead.BeginAnimation(Canvas.TopProperty, moveY);
                }
            }
        }

        public void DrawStaff(double x1, double x2, double y)
        {
            for (int i = 0; i < 5; i++)
            {
                Line line = new Line
                {
                    X1 = x1,
                    Y1 = y + (i * 10),
                    X2 = x2,
                    Y2 = y + (i * 10),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                _canvas.Children.Add(line);
            }
        }
    }
}