using System.Collections.Generic;
using System.Xml.Serialization;

namespace HarmonyAnalyser
{
    [XmlRoot("score-partwise")]
    public class ScorePartwise
    {
        [XmlElement("part-list")]
        public PartList PartList { get; set; }

        [XmlElement("part")]
        public List<Part> Parts { get; set; }
    }

    public class PartList
    {
        [XmlElement("score-part")]
        public ScorePart ScorePart { get; set; }
    }

    public class ScorePart
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("part-name")]
        public string PartName { get; set; }
    }

    public class Part
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("measure")]
        public List<Measure> Measures { get; set; }
    }

    public class Measure
    {
        [XmlAttribute("number")]
        public int Number { get; set; }

        [XmlElement("attributes")]
        public Attributes Attributes { get; set; }

        [XmlElement("note")]
        public List<Note> Notes { get; set; }

        [XmlElement("backup")]
        public List<Backup> Backups { get; set; }

        [XmlElement("forward")]
        public List<Forward> Forwards { get; set; }
    }

    public class Attributes
    {
        [XmlElement("divisions")]
        public int Divisions { get; set; }

        [XmlElement("key")]
        public Key Key { get; set; }

        [XmlElement("time")]
        public Time Time { get; set; }

        [XmlElement("staves")]
        public int Staves { get; set; }

        [XmlElement("clef")]
        public List<Clef> Clefs { get; set; }
    }

    public class Key
    {
        [XmlElement("fifths")]
        public int Fifths { get; set; }
    }

    public class Time
    {
        [XmlElement("beats")]
        public int Beats { get; set; }

        [XmlElement("beat-type")]
        public int BeatType { get; set; }
    }

    public class Clef
    {
        [XmlAttribute("number")]
        public int Number { get; set; }

        [XmlElement("sign")]
        public string Sign { get; set; }

        [XmlElement("line")]
        public int Line { get; set; }
    }

    public class Note
    {
        [XmlElement("rest")]
        public string Rest { get; set; }

        [XmlElement("chord")]
        public string Chord { get; set; }

        [XmlElement("pitch")]
        public Pitch Pitch { get; set; }

        [XmlElement("duration")]
        public int Duration { get; set; }

        [XmlElement("voice")]
        public int Voice { get; set; }

        [XmlElement("type")]
        public string Type { get; set; }

        [XmlElement("dot")]
        public string Dot { get; set; }

        [XmlElement("staff")]
        public int Staff { get; set; }
    }

    public class Pitch
    {
        [XmlElement("step")]
        public string Step { get; set; }

        [XmlElement("octave")]
        public int Octave { get; set; }
    }

    public class Backup
    {
        [XmlElement("duration")]
        public int Duration { get; set; }
    }

    public class Forward
    {
        [XmlElement("duration")]
        public int Duration { get; set; }

        [XmlElement("voice")]
        public int Voice { get; set; }

        [XmlElement("staff")]
        public int Staff { get; set; }
    }

    public class Rest
    {
        [XmlAttribute("measure")]
        public string Measure { get; set; }
    }
}