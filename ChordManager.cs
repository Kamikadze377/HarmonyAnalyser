using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HarmonyAnalyser
{
    public class ChordManager
    {
        public List<Chord> Chords = new List<Chord>();

        public List<Note> Notes = new List<Note>();

        private readonly ScorePartwise _score;

        public ChordManager(ScorePartwise score)
        {
            _score = score;
        }

        public class Chord
        {
            public List<Note> Notes { get; set; } = new List<Note>();
            public MusicRenderer.NoteElement NoteElement { get; set; }
            public int MeasureNumber { get; set; }
            public int Point { get; set; }
            public string Name { get; set; }
            public Border Selection { get; set; }

            public bool IsChecked { get; set; }
        }

        public class Note
        {
            public string Step { get; set; }
            public int Octave { get; set; }
            public int Alter { get; set; }
            public int MeasureNumber { get; set; }
            public int Point { get; set; }

            public override string ToString()
            {
                string alterSymbol = Alter switch
                {
                    -1 => "♭",
                    1 => "♯",
                    _ => ""
                };

                return $"{Step}{alterSymbol}{Octave}";
            }
        }

        public void ExtractChords()
        {
            foreach (var part in _score.Parts)
            {
                int barNumber = 0;
                
                foreach (var measure in part.Measures)
                {
                    barNumber++;

                    // Przeliczenie długości taktu

                    int measureDuration = 0;

                    for (int k = 0; k < measure.Notes.Count; k++)
                    {
                        if (measure.Notes[k].Staff == 1)
                        {
                            if (measure.Notes[k].Chord == null)
                            {
                                measureDuration += measure.Notes[k].Duration;
                            }
                        }
                    }

                    // Dla dwóch pięciolinii w systemie, dla maksymalnie jednego głosu w pięciolinii, brak obsługi pauz

                    if (measure.Attributes != null)
                    {
                        if ((measure.Attributes.Staves != 2 && measure.Number == 1) || (measure.Attributes.Staves != 0 && measure.Attributes.Staves != 2 && measure.Number > 1))
                        {
                            MessageBox.Show("Wymagany wyciąg fortepianowy (klucz wiolinowy i basowy).", "Analiza nie powiodła się", MessageBoxButton.OK, MessageBoxImage.Error);
                            Chords.Clear();
                            break;
                        }
                    }

                    int bassNotesIndex = 0;

                    foreach (var note in measure.Notes)
                    {
                        if (note.Staff != 2)
                        {
                            bassNotesIndex++;
                        }
                    }

                    int treblePoint = 0;
                    int bassPoint = 0;
                    int i = 0; int j = bassNotesIndex;
                    List<int> voices = new List<int>();
                    bool ifChord, firstChordNote, existingVoice;

                    List<Note> chordNotes = new List<Note>();

                    // Pierwszy akord taktu

                    if (treblePoint == 0 && bassPoint == 0)
                    {
                        // Pięciolinia 2 (klucz basowy)

                        // Liczenie głosów

                        voices.Clear();
                        voices.Add(measure.Notes[bassNotesIndex].Voice);
                        existingVoice = false;

                        for (int k = bassNotesIndex; k < measure.Notes.Count; k++)
                        {
                            for (int l = 0; l < voices.Count; l++)
                            {
                                if (voices[l] == measure.Notes[k].Voice)
                                    existingVoice = true;
                            }

                            if (!existingVoice)
                                voices.Add(measure.Notes[k].Voice);

                            existingVoice = false;
                        }

                        // Dodanie składników akordu

                        if (voices.Count == 1)
                        {
                            ifChord = true;

                            do
                            {
                                Note chordNote = new Note
                                {
                                    Step = measure.Notes[j].Pitch.Step,
                                    Octave = measure.Notes[j].Pitch.Octave,
                                    Alter = 0
                                };

                                if (measure.Notes[j].Chord != null)
                                {
                                    chordNotes.Add(chordNote);
                                    j++;
                                }
                                else
                                {
                                    if (chordNotes.Count > 0)
                                    {
                                        ifChord = false;
                                        bassPoint += measure.Notes[bassNotesIndex].Duration;
                                    }
                                    else
                                    {
                                        chordNotes.Add(chordNote);
                                        j++;

                                        if (j == measure.Notes.Count)
                                        {
                                            bassPoint += measure.Notes[j - 1].Duration;
                                        }
                                    }
                                }
                            } while (ifChord && j < measure.Notes.Count);
                        }
                        else
                        {
                            MessageBox.Show("Wymagany maksymalnie jeden głos na pięciolinię", "Analiza nie powiodła się", MessageBoxButton.OK, MessageBoxImage.Error);
                            Chords.Clear();
                            break;
                        }

                        // Pięciolinia 1 (klucz wiolinowy)

                        // Liczenie głosów

                        voices.Clear();
                        voices.Add(measure.Notes[0].Voice);
                        existingVoice = false;

                        for (int k = 0; k < bassNotesIndex; k++)
                        {
                            for (int l = 0; l < voices.Count; l++)
                            {
                                if (voices[l] == measure.Notes[k].Voice)
                                    existingVoice = true;
                            }

                            if (!existingVoice)
                                voices.Add(measure.Notes[k].Voice);

                            existingVoice = false;
                        }

                        // Dodanie składników akordu

                        if (voices.Count == 1)
                        {
                            ifChord = true;

                            do
                            {
                                Note chordNote = new Note
                                {
                                    Step = measure.Notes[i].Pitch.Step,
                                    Octave = measure.Notes[i].Pitch.Octave,
                                    Alter = 0
                                };

                                if (measure.Notes[i].Chord != null)
                                {
                                    chordNotes.Add(chordNote);
                                    i++;
                                }
                                else
                                {
                                    if (i > 0)
                                    {
                                        ifChord = false;

                                        Chord chord = new Chord
                                        {
                                            Notes = new List<Note>(chordNotes),
                                            MeasureNumber = barNumber,
                                            Point = treblePoint
                                        };

                                        chord.Name = IdentifyChord(chord);
                                        Chords.Add(chord);  // Dodanie akordu

                                        treblePoint += measure.Notes[0].Duration;
                                        chordNotes.Clear();

                                        // MessageBox.Show("treblePoint: " + treblePoint + ", bassPoint: " + bassPoint + "\ni = " + i + ", j = " + j, "treblePoint == 0 && bassPoint == 0");
                                    }
                                    else
                                    {
                                        chordNotes.Add(chordNote);
                                        i++;
                                    }
                                }
                            } while (ifChord && i < measure.Notes.Count);
                        }
                        else
                        {
                            MessageBox.Show("Wymagany maksymalnie jeden głos na pięciolinię", "Analiza nie powiodła się", MessageBoxButton.OK, MessageBoxImage.Error);
                            Chords.Clear();
                            break;
                        }
                    }

                    // Kolejne akordy taktu

                    while (treblePoint != measureDuration || bassPoint != measureDuration)
                    {
                        if (voices.Count == 1)
                        {
                            if (treblePoint == bassPoint && j < measure.Notes.Count)
                            {
                                // Pięciolinia 2 (klucz basowy)

                                ifChord = true;

                                do
                                {
                                    Note chordNote = new Note
                                    {
                                        Step = measure.Notes[j].Pitch.Step,
                                        Octave = measure.Notes[j].Pitch.Octave,
                                        Alter = 0
                                    };

                                    if (measure.Notes[j].Chord != null)
                                    {
                                        chordNotes.Add(chordNote);
                                        j++;

                                        if (j == measure.Notes.Count)
                                        {
                                            bassPoint += measure.Notes[j - 1].Duration;
                                        }
                                    }
                                    else
                                    {
                                        if (chordNotes.Count > 0)
                                        {
                                            ifChord = false;
                                            bassPoint += measure.Notes[j - 1].Duration;
                                        }
                                        else
                                        {
                                            chordNotes.Add(chordNote);
                                            j++;

                                            if (j == measure.Notes.Count)
                                            {
                                                bassPoint += measure.Notes[j - 1].Duration;
                                            }
                                        }
                                    }
                                } while (ifChord && j < measure.Notes.Count);

                                // Pięciolinia 1 (klucz wiolinowy)

                                ifChord = true;
                                firstChordNote = true;

                                do
                                {
                                    Note chordNote = new Note
                                    {
                                        Step = measure.Notes[i].Pitch.Step,
                                        Octave = measure.Notes[i].Pitch.Octave,
                                        Alter = 0
                                    };

                                    if (measure.Notes[i].Chord != null)
                                    {
                                        chordNotes.Add(chordNote);
                                        i++;
                                    }
                                    else
                                    {
                                        if (!firstChordNote)
                                        {
                                            ifChord = false;

                                            Chord chord = new Chord
                                            {
                                                Notes = new List<Note>(chordNotes),
                                                MeasureNumber = barNumber,
                                                Point = treblePoint
                                            };

                                            chord.Name = IdentifyChord(chord);
                                            Chords.Add(chord);  // Dodanie akordu

                                            treblePoint += measure.Notes[i - 1].Duration;
                                            chordNotes.Clear();

                                            // MessageBox.Show("treblePoint: " + treblePoint + ", bassPoint: " + bassPoint + "\ni = " + i + ", j = " + j, "treblePoint == bassPoint");
                                        }
                                        else
                                        {
                                            chordNotes.Add(chordNote);
                                            firstChordNote = false;
                                            i++;
                                        }
                                    }
                                } while (ifChord && i < measure.Notes.Count);
                            }

                            else if (treblePoint < bassPoint)
                            {
                                // Pięciolinia 2 (klucz basowy)

                                int jBack = j - 1;

                                while (measure.Notes[jBack].Chord != null)
                                {
                                    jBack--;
                                }

                                ifChord = true;

                                do
                                {
                                    Note chordNote = new Note
                                    {
                                        Step = measure.Notes[jBack].Pitch.Step,
                                        Octave = measure.Notes[jBack].Pitch.Octave,
                                        Alter = 0
                                    };

                                    if (measure.Notes[jBack].Chord != null)
                                    {
                                        chordNotes.Add(chordNote);
                                        jBack++;
                                    }
                                    else
                                    {
                                        if (chordNotes.Count > 0)
                                        {
                                            ifChord = false;
                                        }
                                        else
                                        {
                                            chordNotes.Add(chordNote);
                                            jBack++;
                                        }
                                    }
                                } while (ifChord && jBack < measure.Notes.Count);

                                // Pięciolinia 1 (klucz wiolinowy)

                                ifChord = true;
                                firstChordNote = true;

                                do
                                {
                                    Note chordNote = new Note
                                    {
                                        Step = measure.Notes[i].Pitch.Step,
                                        Octave = measure.Notes[i].Pitch.Octave,
                                        Alter = 0
                                    };

                                    if (measure.Notes[i].Chord != null)
                                    {
                                        chordNotes.Add(chordNote);
                                        i++;
                                    }
                                    else
                                    {
                                        if (!firstChordNote)
                                        {
                                            ifChord = false;

                                            Chord chord = new Chord
                                            {
                                                Notes = new List<Note>(chordNotes),
                                                MeasureNumber = barNumber,
                                                Point = treblePoint
                                            };

                                            chord.Name = IdentifyChord(chord);
                                            Chords.Add(chord);  // Dodanie akordu

                                            treblePoint += measure.Notes[i - 1].Duration;
                                            chordNotes.Clear();

                                            // MessageBox.Show("treblePoint: " + treblePoint + ", bassPoint: " + bassPoint + "\ni = " + i + ", j = " + j, "treblePoint < bassPoint");
                                        }
                                        else
                                        {
                                            chordNotes.Add(chordNote);
                                            firstChordNote = false;
                                            i++;
                                        }
                                    }
                                } while (ifChord && i < measure.Notes.Count);
                            }

                            else if (treblePoint > bassPoint)
                            {
                                // Pięciolinia 2 (klucz basowy)

                                ifChord = true;

                                do
                                {
                                    Note chordNote = new Note
                                    {
                                        Step = measure.Notes[j].Pitch.Step,
                                        Octave = measure.Notes[j].Pitch.Octave,
                                        Alter = 0
                                    };

                                    if (measure.Notes[j].Chord != null)
                                    {
                                        chordNotes.Add(chordNote);
                                        j++;
                                    }
                                    else
                                    {
                                        if (chordNotes.Count > 0)
                                        {
                                            ifChord = false;
                                        }
                                        else
                                        {
                                            chordNotes.Add(chordNote);
                                            j++;
                                        }
                                    }
                                } while (ifChord && j < measure.Notes.Count);

                                // Pięciolinia 1 (klucz wiolinowy)

                                int iBack = i - 1;

                                while (measure.Notes[iBack].Chord != null)
                                {
                                    iBack--;
                                }

                                ifChord = true;
                                firstChordNote = true;

                                do
                                {
                                    Note chordNote = new Note
                                    {
                                        Step = measure.Notes[iBack].Pitch.Step,
                                        Octave = measure.Notes[iBack].Pitch.Octave,
                                        Alter = 0
                                    };

                                    if (measure.Notes[iBack].Chord != null)
                                    {
                                        chordNotes.Add(chordNote);
                                        iBack++;
                                    }
                                    else
                                    {
                                        if (!firstChordNote)
                                        {
                                            ifChord = false;

                                            Chord chord = new Chord
                                            {
                                                Notes = new List<Note>(chordNotes),
                                                MeasureNumber = barNumber,
                                                Point = bassPoint
                                            };

                                            chord.Name = IdentifyChord(chord);
                                            Chords.Add(chord);  // Dodanie akordu

                                            chordNotes.Clear();

                                            bassPoint += measure.Notes[j - 1].Duration;

                                            // MessageBox.Show("treblePoint: " + treblePoint + ", bassPoint: " + bassPoint + "\ni = " + i + ", j = " + j, "treblePoint > bassPoint");
                                        }
                                        else
                                        {
                                            chordNotes.Add(chordNote);
                                            firstChordNote = false;
                                            iBack++;
                                        }
                                    }
                                } while (ifChord && iBack < measure.Notes.Count);
                            }
                        }
                    }
                }
            }
        }

        public string IdentifyChord(Chord chord)
        {
            string chordName;
            string chordStep;
            List <string> chordSteps;
            List <int> chordIntervals;
            List<int> initialChordIntervals;
            List <int> chordSemitones;
            bool noThirdOrFifth = false;

            chordSteps = ExtractChordSteps(chord);
            chordIntervals = CheckChordIntervals(chordSteps);
            initialChordIntervals = chordIntervals;

            do
            {
                for (int i = 0; i < chordIntervals.Count; i++)
                {
                    if (chordIntervals[i] % 2 != 1)
                    {
                        noThirdOrFifth = true;

                        if (chordIntervals.SequenceEqual(initialChordIntervals) || CheckStepNumber(chordSteps[chordSteps.Count - 1]) < CheckStepNumber(chordSteps[i]))
                        {
                            chordStep = chordSteps[i];
                            chordSteps.Remove(chordSteps[i]);
                            chordSteps.Add(chordStep);
                            chordIntervals = CheckChordIntervals(chordSteps);
                        }
                        else
                        {
                            chordStep = chordSteps[i];
                            chordSteps.Remove(chordStep);
                            chordSteps.Add(chordStep);
                            chordStep = chordSteps[chordSteps.Count - 2];
                            chordSteps.Remove(chordStep);
                            chordSteps.Add(chordStep);
                            chordIntervals = CheckChordIntervals(chordSteps);
                        }

                        break;
                    }

                    noThirdOrFifth = false;
                }
            }
            while (noThirdOrFifth && !(chordIntervals.SequenceEqual(initialChordIntervals)));

            chordSemitones = CheckChordSemitones(chordSteps);

            switch (chordSemitones.Count)
            {
                case 1:     // Dwudźwięki

                    chordName = $"({chordIntervals[0]})";
                    break;

                case 2:     // Trójdźwięki

                    if (chordSemitones[0] == 4 && chordSemitones[1] == 3)       // Akord durowy
                        chordName = chordSteps[0];
                    else if (chordSemitones[0] == 3 && chordSemitones[1] == 4)  // Akord molowy
                        chordName = $"{chordSteps[0]}m";
                    else if (chordSemitones[0] == 4 && chordSemitones[1] == 4)  // Akord zwiększony
                        chordName = $"{chordSteps[0]}aug";
                    else if (chordSemitones[0] == 3 && chordSemitones[1] == 3)  // Akord zmniejszony
                        chordName = $"{chordSteps[0]}dim";
                    else
                        goto default;

                    break;

                case 3:     // Czterodźwięki

                    if (chordSemitones[0] == 4 && chordSemitones[1] == 3 && chordSemitones[2] == 3)         // Akord durowy z septymą małą
                        chordName = $"{chordSteps[0]}7";
                    else if (chordSemitones[0] == 4 && chordSemitones[1] == 3 && chordSemitones[2] == 4)    // Akord durowy z septymą wielką
                        chordName = $"{chordSteps[0]}maj7";
                    else if (chordSemitones[0] == 3 && chordSemitones[1] == 4 && chordSemitones[2] == 3)    // Akord molowy z septymą małą
                        chordName = $"{chordSteps[0]}m7";
                    else if (chordSemitones[0] == 3 && chordSemitones[1] == 4 && chordSemitones[2] == 4)    // Akord molowy z septymą wielką
                        chordName = $"{chordSteps[0]}m(maj7)";
                    else if (chordSemitones[0] == 3 && chordSemitones[1] == 3 && chordSemitones[2] == 3)    // Akord zmniejszony
                        chordName = $"{chordSteps[0]}dim";
                    else if (chordSemitones[0] == 3 && chordSemitones[1] == 3 && chordSemitones[2] == 3)    // Akord półzmniejszony
                        chordName = $"{chordSteps[0]}m(♭5)";
                    else
                        goto default;

                    break;

                default:

                    chordName = "   ";
                    break;
            }

            // MessageBox.Show(string.Join(", ", chordIntervals), $"{chordName} ({string.Join("-", chordSteps)})");
            // MessageBox.Show(string.Join(", ", chordSemitones), $"{chordName} ({string.Join("-", chordSteps)})"); 

            return chordName;
        }

        public List<string> ExtractChordSteps(Chord chord)
        {
            List<string> chordSteps = new List<string>();
            string[] notes = { "C", "D", "E", "F", "G", "A", "B" };

            for (int i = 0; i < notes.Length; i++)
            {
                for (int j = 0; j < chord.Notes.Count; j++)
                {
                    if (chord.Notes[j].Step == notes[i])
                    {
                        chordSteps.Add(notes[i]);
                        break;
                    }
                }
            }

            return chordSteps;
        }

        public List<int> CheckChordIntervals(List<string> chordSteps)
        {
            List<int> chordIntervals = new List<int>();
            string[] notes = { "C", "D", "E", "F", "G", "A", "B" };

            if (chordSteps.Count == 1)
            {
                chordIntervals.Add(1);
            }
            else
            {
                for (int i = 1; i < chordSteps.Count; i++)
                {
                    int index = 0;
                    int interval = 1;

                    for (int j = 0; j < 7; j++)
                    {
                        if (chordSteps[i - 1] == notes[j])
                        {
                            index = j;
                            break;
                        }
                    }

                    while (true)
                    {
                        index++;
                        interval++;

                        if (index > 6)
                        {
                            index = 0;
                        }

                        if (chordSteps[i] == notes[index])
                        {
                            chordIntervals.Add(interval);
                            break;
                        }
                    }
                }
            }

            return chordIntervals;
        }

        public List<int> CheckChordSemitones(List<string> chordSteps)
        {
            List<int> chordSemitones = new List<int>();
            string[] notes = { "C", "D", "E", "F", "G", "A", "B" };

            if (chordSteps.Count == 1)
            {
                chordSemitones.Add(0);
            }
            else
            {
                for (int i = 1; i < chordSteps.Count; i++)
                {
                    int index = 0;
                    int semitones = 0;

                    for (int j = 0; j < 7; j++)
                    {
                        if (chordSteps[i - 1] == notes[j])
                        {
                            index = j;
                            break;
                        }
                    }

                    while (true)
                    {
                        index++;

                        if (index > 6)
                        {
                            index = 0;
                        }

                        if (notes[index] == "C" || notes[index] == "F")
                        {
                            semitones++;
                        }
                        else
                        {
                            semitones += 2;
                        }

                        if (chordSteps[i] == notes[index])
                        {
                            chordSemitones.Add(semitones);
                            break;
                        }
                    }
                }
            }

            return chordSemitones;
        }

        public int CheckStepNumber(string step)
        {
            int stepNumber = 0;
            string[] notes = { "C", "D", "E", "F", "G", "A", "B" };

            for (int i = 0; i < notes.Length; i++)
            {
                if (notes[i] == step)
                {
                    stepNumber = i + 1;
                    break;
                }
            }

            return stepNumber;
        }
    }
}
