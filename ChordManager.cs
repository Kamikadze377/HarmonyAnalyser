using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;

namespace HarmonyAnalyser
{
    public class ChordManager
    {
        public List<Subchord> Subchords = new List<Subchord>();
        public List<Chord> Chords = new List<Chord>();
        public List<Note> Notes = new List<Note>();

        private readonly ScorePartwise _score;

        public ChordManager(ScorePartwise score)
        {
            _score = score;
        }

        public class Subchord
        {
            public string Name { get; set; }
            public int Point { get; set; }
            public int MeasureNumber { get; set; }
            public List<Note> Notes { get; set; } = new List<Note>();
            public Chord Chord { get; set; }
            public string BassNote { get; set; }
            public string RootNote { get; set; }
            public MusicRenderer.SubchordElement Element { get; set; }
            public MusicRenderer.NoteElement NoteElement { get; set; }

            public bool IsRepeated { get; set; }
            public bool IsChecked { get; set; }
        }

        public class Chord
        {
            public string Name { get; set; }
            public int StartPoint { get; set;}
            public int EndPoint { get; set; }
            public int MeasureNumber { get; set; }
            public List<ChordStep> Steps { get; set; } = new List<ChordStep>();
            public List<ChordNote> Notes { get; set; } = new List<ChordNote>();
            public List<Subchord> Subchords { get; set; } = new List<Subchord>();
            public string BassNote { get; set; }
            public string RootNote { get; set; }
            public MusicRenderer.ChordElement Element { get; set; }
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

        public class ChordStep
        {
            public string Step { get; set; }
            public int Weight { get; set; }
        }

        public class ChordNote
        {
            public Note Note { get; set; }
            public int Weight { get; set; }
        }

        public void ExtractSubchords()
        {
            // Wyodrębnienie podakordów

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
                            Subchords.Clear();
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
                            Subchords.Clear();
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

                                        Subchord subchord = new Subchord
                                        {
                                            Notes = new List<Note>(chordNotes),
                                            MeasureNumber = barNumber,
                                            Point = treblePoint,
                                            BassNote = chordNotes[0].Step
                                        };

                                        subchord.Name = IdentifySubchord(subchord);
                                        subchord.RootNote = GetSubchordRootNote(subchord);

                                        Subchords.Add(subchord);  // Dodanie akordu
                                        if (Subchords.Count > 1 && subchord.Name == Subchords[Subchords.Count - 2].Name && subchord.MeasureNumber == Subchords[Subchords.Count - 2].MeasureNumber)
                                            subchord.IsRepeated = true;
                                        else
                                            subchord.IsRepeated = false;

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
                            Subchords.Clear();
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

                                            Subchord subchord = new Subchord
                                            {
                                                Notes = new List<Note>(chordNotes),
                                                MeasureNumber = barNumber,
                                                Point = treblePoint,
                                                BassNote = chordNotes[0].Step
                                            };

                                            subchord.Name = IdentifySubchord(subchord);
                                            subchord.RootNote = GetSubchordRootNote(subchord);
                                            Subchords.Add(subchord);  // Dodanie akordu
                                            if (Subchords.Count > 1 && subchord.Name == Subchords[Subchords.Count - 2].Name && subchord.MeasureNumber == Subchords[Subchords.Count - 2].MeasureNumber)
                                                subchord.IsRepeated = true;
                                            else
                                                subchord.IsRepeated = false;

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

                                            Subchord subchord = new Subchord
                                            {
                                                Notes = new List<Note>(chordNotes),
                                                MeasureNumber = barNumber,
                                                Point = treblePoint,
                                                BassNote = chordNotes[0].Step
                                            };

                                            subchord.Name = IdentifySubchord(subchord);
                                            subchord.RootNote = GetSubchordRootNote(subchord);
                                            Subchords.Add(subchord);  // Dodanie akordu
                                            if (Subchords.Count > 1 && subchord.Name == Subchords[Subchords.Count - 2].Name && subchord.MeasureNumber == Subchords[Subchords.Count - 2].MeasureNumber)
                                                subchord.IsRepeated = true;
                                            else
                                                subchord.IsRepeated = false;

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

                                            Subchord subchord = new Subchord
                                            {
                                                Notes = new List<Note>(chordNotes),
                                                MeasureNumber = barNumber,
                                                Point = bassPoint,
                                                BassNote = chordNotes[0].Step
                                            };

                                            subchord.Name = IdentifySubchord(subchord);
                                            subchord.RootNote = GetSubchordRootNote(subchord);
                                            Subchords.Add(subchord);  // Dodanie akordu
                                            if (Subchords.Count > 1 && subchord.Name == Subchords[Subchords.Count - 2].Name && subchord.MeasureNumber == Subchords[Subchords.Count - 2].MeasureNumber)
                                                subchord.IsRepeated = true;
                                            else
                                                subchord.IsRepeated = false;

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

        public void ExtractChords()
        {
            foreach (var part in _score.Parts)
            {
                for (int i = 1; i <= part.Measures.Count; i++)
                {
                    List<Subchord> measureSubchords = new List<Subchord>();
                    List<ChordStep> chordSteps = new List<ChordStep>();
                    List<ChordNote> chordNotes = new List<ChordNote>();

                    // Pobranie wszystkich podakordów z taktu
                    foreach (var subchord in Subchords)
                    {
                        if (subchord.MeasureNumber == i)
                            measureSubchords.Add(subchord);
                        else if (subchord.MeasureNumber > i)
                            break;
                    }

                    var chords = IdentifyChordsBySubchords(measureSubchords, true);   // Klasyfikacja nr 1 — według podakordów
                    // Klasyfikacja nr 2 — według składników akordów
                    // Klasyfikacja nr 3 - według basu

                    if (chords != null)
                        Chords.AddRange(chords);
                    else
                    {
                        Chord chord = new Chord
                        {
                            Name = "?",
                            StartPoint = measureSubchords[0].Point,
                            EndPoint = measureSubchords[measureSubchords.Count - 1].Point,
                            MeasureNumber = i
                        };

                        foreach (var subchord in measureSubchords)
                        {
                            subchord.Chord = chord;
                            chord.Subchords.Add(subchord);
                        }

                        Chords.Add(chord);
                    }
                }
            }
        }

        public List<Chord> IdentifyChordsBySubchords(List<Subchord> subchords, bool returnNotNull = false)
        {
            List<Chord> chords = new List<Chord>();
            List<string> steps = new List<string>();
            int chordIndex = 0;
            int startPointIndex = 0;
            int endPointIndex = -1;
            bool incompleteChords = false;

            goto analysis;

            analysis:

            for (int i = endPointIndex + 1; i < subchords.Count; i++)
            {
                if (chords.Count == chordIndex && subchords[i].Name.ToArray()[0] != '(')
                {
                    Chord chord = new Chord
                    {
                        Name = subchords[i].Name,
                        StartPoint = subchords[i].Point,
                        MeasureNumber = subchords[i].MeasureNumber,
                        RootNote = subchords[i].RootNote
                    };

                    chords.Add(chord);
                    steps = GetSubchordSteps(subchords[i]);
                    steps = SortChordSteps(steps);
                        
                    if (startPointIndex != endPointIndex + 1)
                        startPointIndex = i;

                    if (i == subchords.Count - 1)
                    {
                        chords[chordIndex].EndPoint = subchords[i].Point;
                        endPointIndex = i;
                    }

                    continue;
                }
                else if (chords.Count > chordIndex && subchords[i].Name.ToArray()[0] != '(' && !subchords[i].IsRepeated)
                {
                    if (!(i > endPointIndex + 1 && CheckSubchordAffilliation(subchords[i], subchords[i - 1])))  // Pierwsze trzy składniki nie są identyczne w porównaniu do poprzedniego podakordu.
                    {
                        chords[chordIndex].EndPoint = subchords[i - 1].Point;
                        endPointIndex = i - 1;
                        break;
                    }
                }
                else if (subchords[i].Name.ToArray()[0] == '(')
                {
                    incompleteChords = true;
                    if (i == endPointIndex - 1)
                        startPointIndex = i;
                }
                
                if (i == subchords.Count - 1 && chords.Count > chordIndex)
                {
                    chords[chordIndex].EndPoint = subchords[i].Point;
                    endPointIndex = i;
                }
            }

            if (!incompleteChords && chords.Count > chordIndex && endPointIndex == subchords.Count - 1)  // Brak niepełnych podakordów, identyczne pełne podakordy.
            {
                for (int i = startPointIndex; i < subchords.Count; i++)
                {
                    chords[chordIndex].Subchords.Add(subchords[i]);
                    subchords[i].Chord = chords[chordIndex];
                }

                return IncludeBass(chords);
            }
            else if (incompleteChords && chords.Count > chordIndex && endPointIndex == subchords.Count - 1)  // Niepełne podakordy, identyczne pełne podakordy.
            {
                bool belongsToChord;
                string subchordStep;

                for (int i = startPointIndex; i < subchords.Count; i++)
                {
                    belongsToChord = false;

                    if (subchords[i].Name.ToArray()[0] == '(')
                    {
                        for (int j = 0; j < GetSubchordSteps(subchords[i]).Count; j++)  // Sprawdzenie, czy dźwięki niepełnego podakordu należą do obranego akordu.
                        {
                            subchordStep = SortChordSteps(GetSubchordSteps(subchords[i]))[j];
                            belongsToChord = false;

                            for (int k = 0; k < steps.Count; k++)
                            {
                                if (subchordStep == steps[k])
                                {
                                    belongsToChord = true;
                                    break;
                                }
                            }

                            if (belongsToChord)
                                continue;
                            else
                            {
                                List<string> modifiedSteps = steps;     // Sprawdzenie, czy dźwięk niepełnego podakordu stanowi septymę akordu (w przyszłości: "septymę, nonę, undecymę lub tercdecymę akordu").
                                modifiedSteps.Add(subchordStep);
                                string newChordName = GetChordName(modifiedSteps, subchords[i].BassNote);
                                if (newChordName.ToArray()[0] != '(' && newChordName.ToArray()[0] != '?')
                                {
                                    chords[chordIndex].Name = newChordName;
                                    steps.Add(subchordStep);
                                    belongsToChord = true;
                                }
                                else
                                {
                                    belongsToChord = false;
                                }

                                if (belongsToChord)
                                    continue;
                                else
                                    break;
                            }
                        }

                        if (belongsToChord)
                        {
                            if (i == startPointIndex)
                                chords[chordIndex].StartPoint = subchords[i].Point;

                            chords[chordIndex].Subchords.Add(subchords[i]);
                            subchords[i].Chord = chords[chordIndex];
                        }
                        else   // Podakord nie pasuje do akordu.
                        {
                            if (!returnNotNull)
                                return null;    
                            else
                            {
                                chords[chordIndex].EndPoint = subchords[i - 1].Point;

                                Chord chord = new Chord
                                {
                                    Name = "?",
                                    StartPoint = subchords[i].Point,
                                    EndPoint = subchords[subchords.Count - 1].Point,
                                    MeasureNumber = subchords[i].MeasureNumber,
                                };

                                for (int j = i; j < subchords.Count; j++)
                                {
                                    chord.Subchords.Add(subchords[j]);
                                    subchords[j].Chord = chord;
                                }

                                chords.Add(chord);

                                return IncludeBass(chords);
                            }
                        }
                    }
                    else
                    {
                        chords[chordIndex].Subchords.Add(subchords[i]);
                        subchords[i].Chord = chords[chordIndex];
                    }
                }

                chords[chordIndex].EndPoint = subchords[subchords.Count - 1].Point;
            }
            else if (incompleteChords && chords.Count == 0 && endPointIndex == subchords.Count - 1)     // Niepełne podakordy, brak pełnych akordów
            {
                if (!returnNotNull)
                    return null;
                else
                {
                    Chord chord = new Chord
                    {
                        Name = "?",
                        StartPoint = subchords[chordIndex].Point,
                        EndPoint = subchords[subchords.Count - 1].Point,
                        MeasureNumber = subchords[chordIndex].MeasureNumber,
                    };

                    chord.Subchords.AddRange(subchords);

                    foreach (var subchord in subchords)
                        subchord.Chord = chord;

                    chords.Add(chord);

                    return IncludeBass(chords);
                }
            }
            else if (!incompleteChords && chords.Count > 0 && endPointIndex < subchords.Count - 1)      // Różne pełne podakordy
            {
                for (int i = startPointIndex; i < endPointIndex + 1; i++)
                {
                    chords[chordIndex].Subchords.Add(subchords[i]);
                    subchords[i].Chord = chords[chordIndex];
                }

                chordIndex++;

                if (chordIndex < subchords.Count)
                    goto analysis;
            }
            else
            {
                if (!returnNotNull)
                    return null;
                else
                {
                    chords.Clear();

                    Chord chord = new Chord
                    {
                        Name = "?",
                        StartPoint = subchords[0].Point,
                        EndPoint = subchords[subchords.Count - 1].Point,
                        MeasureNumber = subchords[0].MeasureNumber,
                    };

                    chord.Subchords.AddRange(subchords);

                    foreach (var subchord in subchords)
                        subchord.Chord = chord;

                    chords.Add(chord);

                    return chords;
                }
            }

            return IncludeBass(chords);
        }

        public List<Chord> IncludeBass(List<Chord> chords)
        {
            bool isBassDifferent;
            bool foreignNote;

            for (int i = 0; i < chords.Count; i++)
            {
                isBassDifferent = false;
                foreignNote = false;

                var subchords = chords[i].Subchords;

                if (subchords.Count == 1)   // Jeżeli akord składa się z jednego podakordu, przejmuje jego nutę basową.
                    chords[i].BassNote = subchords[0].BassNote;
                else
                {
                    for (int j = 0; j < subchords.Count; j++)
                    {
                        if (j > 0)
                            if (subchords[j].BassNote != subchords[j - 1].BassNote)         // Sprawdzenie, czy dźwięk w basie jest stały.
                                isBassDifferent = true;

                        var stepsExceptBass = EraseBassFromSubchordSteps(SortChordSteps(GetSubchordSteps(subchords[j])), subchords[j].BassNote);     // Sprawdzenie, czy w basie znajdują się dźwięki obce.
                        if (GetChordName(stepsExceptBass, subchords[j].BassNote).ToArray()[0] != '(' && GetChordName(stepsExceptBass, subchords[j].BassNote) != "?")
                            foreignNote = true;
                    }

                    if (!isBassDifferent)
                        chords[i].BassNote = subchords[0].BassNote;
                    else if (isBassDifferent && foreignNote)
                    {
                        var newChords = new List<Chord>();

                        foreach (var subchord in subchords)
                        {
                            subchord.IsRepeated = false;

                            Chord chord = new Chord
                            {
                                Name = subchord.Name,
                                BassNote = subchord.BassNote,
                                RootNote = subchord.RootNote,
                                StartPoint = subchord.Point,
                                EndPoint = subchord.Point,
                                MeasureNumber = subchord.MeasureNumber,
                            };

                            chord.Subchords.Add(subchord);
                            subchord.Chord = chord;
                            newChords.Add(chord);
                        }

                        chords.Remove(chords[i]);
                        newChords.AddRange(chords);
                        chords = newChords;
                    }
                    else
                        chords[i].BassNote = null;
                }
            }

            return chords;
        }

        public string IdentifySubchord(Subchord subchord)
        {
            List <string> chordSteps = GetSubchordSteps(subchord);

            chordSteps = SortChordSteps(chordSteps);

            return GetChordName(chordSteps, subchord.BassNote);
        }

        public List<string> GetSubchordSteps(Subchord subchord)
        {
            List<string> chordSteps = new List<string>();
            string[] notes = { "C", "D", "E", "F", "G", "A", "B" };

            for (int i = 0; i < notes.Length; i++)
            {
                for (int j = 0; j < subchord.Notes.Count; j++)
                {
                    if (subchord.Notes[j].Step == notes[i])
                    {
                        chordSteps.Add(notes[i]);
                        break;
                    }
                }
            }

            return chordSteps;
        }

        public List<int> GetSubchordIntervals(Subchord subchord)
        {
            return GetStepsIntervals(GetSubchordSteps(subchord));
        }

        public List<int> GetSubchordSemitones(Subchord subchord)
        {
            return GetStepsSemitones(GetSubchordSteps(subchord));
        }

        public List<string> SortChordSteps(List<string> chordSteps)
        {
            string chordStep;
            List<int> chordIntervals = GetStepsIntervals(chordSteps);
            List<int> initialChordIntervals = chordIntervals;
            bool noThirdOrFifth = false;

            do
            {
                for (int i = 0; i < chordIntervals.Count; i++)
                {
                    if (chordIntervals[i] % 2 != 1)
                    {
                        noThirdOrFifth = true;

                        if (chordIntervals.SequenceEqual(initialChordIntervals) || GetStepNumber(chordSteps[chordSteps.Count - 1]) < GetStepNumber(chordSteps[i]))
                        {
                            chordStep = chordSteps[i];
                            chordSteps.Remove(chordSteps[i]);
                            chordSteps.Add(chordStep);
                            chordIntervals = GetStepsIntervals(chordSteps);
                        }
                        else
                        {
                            chordStep = chordSteps[i];
                            chordSteps.Remove(chordStep);
                            chordSteps.Add(chordStep);
                            chordStep = chordSteps[chordSteps.Count - 2];
                            chordSteps.Remove(chordStep);
                            chordSteps.Add(chordStep);
                            chordIntervals = GetStepsIntervals(chordSteps);
                        }

                        break;
                    }

                    noThirdOrFifth = false;
                }
            }
            while (noThirdOrFifth && !(chordIntervals.SequenceEqual(initialChordIntervals)));

            return chordSteps;
        }

        public string GetChordName(List<string> chordSteps, string bassNote, bool getRootNote = false)
        {
            string chordName;
            var chordIntervals = GetStepsIntervals(chordSteps);
            var chordSemitones = GetStepsSemitones(chordSteps);

            switch (chordSemitones.Count)
            {
                case 1:     // Dwudźwięki

                    chordName = $"({chordIntervals[0]})";
                    break;

                case 2:     // Trójdźwięki

                    if (chordSemitones[0] == 4 && chordSemitones[1] == 3)           // Akord durowy
                        chordName = chordSteps[0];
                    else if (chordSemitones[0] == 3 && chordSemitones[1] == 4)      // Akord molowy
                        chordName = $"{chordSteps[0]}m";
                    else if (chordSemitones[0] == 4 && chordSemitones[1] == 4)      // Akord zwiększony
                        chordName = $"{chordSteps[0]}aug";
                    else if (chordSemitones[0] == 3 && chordSemitones[1] == 3)      // Akord zmniejszony
                        chordName = $"{chordSteps[0]}dim";
                    else if ((chordSemitones[0] == 4 && chordSemitones[1] == 6) ||  // Niepełny akord septymowy
                        (chordSemitones[0] == 7 && chordSemitones[1] == 3))
                        chordName = $"({chordIntervals[0]},{chordIntervals[1]})";
                    else
                        goto default;

                    break;

                case 3:     // Czterodźwięki

                    if (chordSemitones[0] == 4 && chordSemitones[1] == 3 && chordSemitones[2] == 3)         // Akord durowy z septymą małą
                    {
                        if (chordSteps[3] == bassNote)
                            chordName = $"{chordSteps[0]}";
                        else
                            chordName = $"{chordSteps[0]}7";
                    }
                    else if (chordSemitones[0] == 4 && chordSemitones[1] == 3 && chordSemitones[2] == 4)    // Akord durowy z septymą wielką
                    {
                        if (chordSteps[3] == bassNote)
                            chordName = $"{chordSteps[0]}";
                        else
                            chordName = $"{chordSteps[0]}maj7";
                    }
                    else if (chordSemitones[0] == 3 && chordSemitones[1] == 4 && chordSemitones[2] == 3)    // Akord molowy z septymą małą
                    {
                        if (chordSteps[3] == bassNote)
                            chordName = $"{chordSteps[0]}m";
                        else
                            chordName = $"{chordSteps[0]}m7";
                    }
                    else if (chordSemitones[0] == 3 && chordSemitones[1] == 4 && chordSemitones[2] == 4)
                    {
                        if (chordSteps[3] == bassNote)
                            chordName = $"{chordSteps[0]}m";
                        else
                            chordName = $"{chordSteps[0]}m(maj7)";
                    }
                    else if (chordSemitones[0] == 3 && chordSemitones[1] == 3 && chordSemitones[2] == 3)    // Akord zmniejszony
                        chordName = $"{chordSteps[0]}dim";
                    else if (chordSemitones[0] == 3 && chordSemitones[1] == 3 && chordSemitones[2] == 4)    // Akord półzmniejszony
                    {
                        if (chordSteps[3] == bassNote)
                            chordName = $"{chordSteps[0]}m";
                        else
                            chordName = $"{chordSteps[0]}m(♭5)";
                    }
                    else
                    {
                        var newChordName = GetChordName(SortChordSteps(EraseBassFromSubchordSteps(chordSteps, bassNote)), bassNote);

                        if (newChordName != "?" && newChordName.ToArray()[0] != '(')
                            chordName = newChordName;
                        else
                            goto default;
                    }

                    break;

                default:

                    chordName = "?";
                    break;
            }

            if (getRootNote)
                return chordSteps[0];

            return chordName;
        }

        public List<string> EraseBassFromSubchordSteps(List<string> steps, string bassNote)
        {
            var reducedSteps = steps;
            steps.Remove(bassNote);
            return reducedSteps;
        }

        public List<int> GetStepsIntervals(List<string> steps)
        {
            List<int> chordIntervals = new List<int>();
            string[] notes = { "C", "D", "E", "F", "G", "A", "B" };

            if (steps.Count == 1)
            {
                chordIntervals.Add(1);
            }
            else
            {
                for (int i = 1; i < steps.Count; i++)
                {
                    int index = 0;
                    int interval = 1;

                    for (int j = 0; j < 7; j++)
                    {
                        if (steps[i - 1] == notes[j])
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

                        if (steps[i] == notes[index])
                        {
                            chordIntervals.Add(interval);
                            break;
                        }
                    }
                }
            }

            return chordIntervals;
        }

        public List<int> GetStepsSemitones(List<string> steps)
        {
            List<int> chordSemitones = new List<int>();
            string[] notes = { "C", "D", "E", "F", "G", "A", "B" };

            if (steps.Count == 1)
            {
                chordSemitones.Add(0);
            }
            else
            {
                for (int i = 1; i < steps.Count; i++)
                {
                    int index = 0;
                    int semitones = 0;

                    for (int j = 0; j < 7; j++)
                    {
                        if (steps[i - 1] == notes[j])
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

                        if (steps[i] == notes[index])
                        {
                            chordSemitones.Add(semitones);
                            break;
                        }
                    }
                }
            }

            return chordSemitones;
        }

        public int GetStepNumber(string step)
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

        public string GetSubchordRootNote(Subchord subchord)
        {
            List<string> chordSteps = GetSubchordSteps(subchord);
            chordSteps = SortChordSteps(chordSteps);

            return GetChordName(chordSteps, subchord.BassNote, true);
        }

        public bool CheckSubchordAffilliation(Subchord checkedSubchord, Subchord templateSubchord)
        {
            bool belongsToTemplate = false;

            var checkedSteps = SortChordSteps(GetSubchordSteps(checkedSubchord));
            var templateSteps = SortChordSteps(GetSubchordSteps(templateSubchord));

            if (templateSteps.Count > checkedSteps.Count && checkedSubchord.RootNote == templateSubchord.RootNote)
                for (int i = 0; i < checkedSteps.Count; i++)
                {
                    var checkedStep = checkedSteps[i];
                    belongsToTemplate = false;
                    foreach (var templateStep in templateSteps)
                    {
                        if (checkedStep == templateStep)
                        {
                            belongsToTemplate = true;
                            break;
                        }
                    }

                    if (belongsToTemplate)
                        continue;
                    else
                        break;
                }

            return belongsToTemplate;
        }
    }
}
