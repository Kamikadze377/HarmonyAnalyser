using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonyAnalyser.Tests
{
    public static class ChordTestData
    {
        private static readonly List<string> Notes = new() { "C", "D", "E", "F", "G", "A", "B" };

        public static ChordManager.Note N(
            string step,
            int octave = 4,
            int alter = 0,
            int measure = 1,
            int point = 0)
        {
            return new ChordManager.Note
            {
                Step = step,
                Octave = octave,
                Alter = alter,
                MeasureNumber = measure,
                Point = point
            };
        }

        public static IEnumerable<object[]> Chords1() => Chords(1);
        public static IEnumerable<object[]> Chords2() => Chords(2);
        public static IEnumerable<object[]> Chords3() => Chords(3);
        public static IEnumerable<object[]> Chords4() => Chords(4);
        public static IEnumerable<object[]> Chords5() => Chords(5);

        private static IEnumerable<object[]> Chords(int numberOfNotes)
        {
            foreach (var chord in PermutationHelper.GetPermutations(Notes, numberOfNotes))
                yield return new object[] { chord };
        }
    }
}
