using Xunit;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace HarmonyAnalyser.Tests
{
    public class ChordManagerTests
    {
        private readonly ITestOutputHelper _output;

        public ChordManagerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(ChordTestData.Chords1), MemberType = typeof(ChordTestData))]
        [MemberData(nameof(ChordTestData.Chords2), MemberType = typeof(ChordTestData))]
        public void SortChordSteps_LessThanThree_ReturnsUnchanged(List<string> input)
        {
            var chordManager = new ChordManager();

            var output = chordManager.SortChordSteps(input);

            Assert.Equal(input, output);
        }

        [Theory]
        [MemberData(nameof(ChordTestData.Chords3), MemberType = typeof(ChordTestData))]
        [MemberData(nameof(ChordTestData.Chords4), MemberType = typeof(ChordTestData))]
        [MemberData(nameof(ChordTestData.Chords5), MemberType = typeof(ChordTestData))]
        public void SortChordSteps_Three_Four_Five_OddIntervals(List<string> input)
        {
            var chordManager = new ChordManager();

            var output = chordManager.SortChordSteps(input);
            var intervals = chordManager.GetStepsIntervals(output);
            bool hasEvenInterval = intervals.Any(i => i % 2 == 0);

            _output.WriteLine($"INPUT:     {string.Join(", ", input)}");
            _output.WriteLine($"OUTPUT:    {string.Join(", ", output)}");
            _output.WriteLine($"INTERVALS: {string.Join(", ", intervals)}");

            Assert.False(hasEvenInterval);
        }

        [Fact]
        public void IdentifyChordsBySubchords_SingleFullSubchord_ReturnsSingleChord()
        {
            var manager = new ChordManager();

            var subchord = new ChordManager.Subchord
            {
                Name = "C",
                Point = 0,
                MeasureNumber = 1,
                Notes = new List<ChordManager.Note>
                {
                    ChordTestData.N("C"),
                    ChordTestData.N("E"), 
                    ChordTestData.N("G")
                },
                BassNote = "C",
                RootNote = "C"
            };

            var input = new List<ChordManager.Subchord> { subchord };

            var result = manager.IdentifyChordsBySubchords(input);

            Assert.NotNull(result);
            Assert.Single(result);

            var chord = result[0];
            Assert.Equal("C", chord.Name);
            Assert.Equal(0, chord.StartPoint);
            Assert.Equal(0, chord.EndPoint);
            Assert.Single(chord.Subchords);
            Assert.Same(subchord, chord.Subchords[0]);
            Assert.Same(chord, subchord.Chord);
        }

        [Fact]
        public void IdentifyChordsBySubchords_FullSubchordsAndOneIncompleteSubchord_BelongsToChord()
        {
            var manager = new ChordManager();

            var subchords = new List<ChordManager.Subchord>
            {
                new()
                {
                    Name = "C",
                    Point = 0,
                    MeasureNumber = 1,
                    Notes = new()
                    {
                        ChordTestData.N("C"), 
                        ChordTestData.N("E"), 
                        ChordTestData.N("G")
                    },
                    RootNote = "C",
                    BassNote = "C"
                },
                new()
                {
                    Name = "(3>)",
                    Point = 2,
                    MeasureNumber = 1,
                    Notes = new()
                    {
                        ChordTestData.N("E"), 
                        ChordTestData.N("G")
                    },
                    RootNote = "E",
                    BassNote = "E"
                },
                new()
                {
                    Name = "C",
                    Point = 4,
                    MeasureNumber = 1,
                    Notes = new()
                    {
                        ChordTestData.N("C"),
                        ChordTestData.N("E"),
                        ChordTestData.N("G")
                    },
                    RootNote = "C",
                    BassNote = "C"
                }
            };

            var result = manager.IdentifyChordsBySubchords(subchords);

            Assert.Single(result);
            Assert.Equal(3, result[0].Subchords.Count);
        }
    }
}