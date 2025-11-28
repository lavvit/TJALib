using AstrumLoom;

using SongGenre = TJALib.TJA.Genre;

namespace TJALib.TJA;

public class Header
{
    public string Path { get; set; } = "";
    public string Title { get; set; } = "";
    public string SubTitle { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Designer { get; set; } = "";
    public string Genre { get; set; } = "";
    public string Wave { get; set; } = "";
    public string Preview { get; set; } = "";
    public string Movie { get; set; } = "";
    public string Image { get; set; } = "";
    public double Offset { get; set; } = 0;
    public double MovieOffset { get; set; } = 0;
    public double Demo { get; set; } = 0;
    public double BPM { get; set; } = 0;
    public double Length { get; private set; } = 0;

    public List<(string file, double offset)> Wavelist { get; set; } = [];
    public List<string> Genres { get; set; } = [];

    public ECourse Difficulty = ECourse.Oni;
    public double Level = 0;
    public double ScoreInit = -1;
    public double ScoreDiff = -1;
    public double Total = 0;
    public int HBS = 0;
    public List<int> Balloon = [];

    public DateTime CreateTime = DateTime.MinValue;

    public DanHeader? Dan = null;

    public Header() { }
    public Header(Header header)
    {
        Title = header.Title;
        SubTitle = header.SubTitle;
        Artist = header.Artist;
        Designer = header.Designer;
        Genre = header.Genre;
        Wave = header.Wave;
        Preview = header.Preview;
        Movie = header.Movie;
        Image = header.Image;
        Offset = header.Offset;
        Demo = header.Demo;
        BPM = header.BPM;
        Length = header.Length;
        Genres.AddRange(header.Genres);

        Difficulty = header.Difficulty;
        Level = header.Level;
        ScoreInit = header.ScoreInit;
        ScoreDiff = header.ScoreDiff;
        Total = header.Total;
        HBS = header.HBS;
        Balloon.AddRange(header.Balloon);

        Dan = header.Dan;
    }
    public Header(Header header, Header course)
        : this(header)
    {
        if (!string.IsNullOrEmpty(course.Designer))
        {
            Designer = course.Designer;
        }
        Level = course.Level;
        ScoreInit = course.ScoreInit;
        ScoreDiff = course.ScoreDiff;
        Total = course.Total;
        HBS = course.HBS;
        Balloon = [.. course.Balloon];
    }

    public int course = 3; // Default to Oni course
    public int dangauge = 0;
    public void Read(string line, Header[] courses)
    {
        string split = line.Split(':')[0].Trim().ToLower();
        string value = line[(split.Length + 1)..].Trim();
        switch (split)
        {
            case "title":
                Title = value;
                break;
            case "subtitle":
                SubTitle = value.StartsWith("++") || value.StartsWith("--") ? value[2..] : value;
                break;
            case "artist":
                Artist = value;
                break;
            case "designer":
            case "notesdesigner":
            case "notesdesigner0":
            case "notesdesigner1":
            case "notesdesigner2":
            case "notesdesigner3":
            case "notesdesigner4":
            case "maker":
            case "author":
                if (int.TryParse(split.Last().ToString(), out int c))
                {
                    if (c == course)
                    {
                        courses[c].Designer = value;
                    }
                    if (!Designer.Contains(value))
                    {
                        if (string.IsNullOrEmpty(Designer)) Designer = value;
                        else Designer += " / " + value;
                    }
                }
                else
                {
                    Designer = value;
                }
                break;
            case "genre":
                Genre = value;
                Genres.Add(value);
                break;
            case "subgenre":
                Genres.Add(value);
                break;
            case "wave":
                SetWave(value);
                break;
            case "cwave":
                {
                    string raw = value.Trim();
                    string filename;
                    double offsetVal = Offset;

                    int lastComma = raw.LastIndexOf(',');
                    if (lastComma >= 0)
                    {
                        filename = raw[..lastComma].Trim();
                        string ofsStr = raw[(lastComma + 1)..].Trim();
                        if (!double.TryParse(ofsStr, out offsetVal))
                        {
                            offsetVal = Offset;
                        }
                    }
                    else
                    {
                        filename = raw;
                    }

                    if (string.IsNullOrEmpty(Wave))
                    {
                        SetWave(filename);
                    }
                    Wavelist.Add((filename, offsetVal));
                }
                break;
            case "offset":
                if (double.TryParse(value, out double offset))
                {
                    Offset = Math.Round(offset, 3, MidpointRounding.AwayFromZero);
                }
                break;
            case "movieoffset":
                if (double.TryParse(value, out double movieOffset))
                {
                    MovieOffset = Math.Round(movieOffset, 3, MidpointRounding.AwayFromZero);
                }
                break;
            case "preview":
                Preview = value;
                break;
            case "bga":
            case "movie":
            case "bgmovie":
                Movie = value;
                break;
            case "image":
            case "bgimage":
                Image = value;
                break;
            case "bpm":
                if (double.TryParse(value, out double bpm))
                {
                    BPM = bpm;
                }
                break;
            case "demostart":
                if (double.TryParse(value, out double demo))
                {
                    Demo = Math.Round(demo * 1000.0, 1);
                }
                break;
            case "course":
                course = Course.GetCourse(value);
                Difficulty = (ECourse)course;
                break;
            case "dangaugered":
            case "dangaugegold":
                {
                    Dan ??= new DanHeader();
                    if (Dan.Exam.Count == 0)
                    {
                        Dan.Exam.Add($"g,{(split.EndsWith("red") ? value : 0)},{(split.EndsWith("gold") ? value : 0)}");
                        dangauge++;
                    }
                    else
                    {

                        string[] parts = Dan.Exam.Where(s => Exam.ExamName(s.Split(',')[0]) == EExam.Gauge).First().Split(',');
                        if (split.EndsWith("red"))
                        {
                            parts[1] = value;
                        }
                        else
                        {
                            parts[2] = value;
                        }
                        Dan.Exam[Dan.Exam.IndexOf(Dan.Exam.Where(s => Exam.ExamName(s.Split(',')[0]) == EExam.Gauge).First())] = string.Join(",", parts);
                    }
                }
                break;
        }
        if (split.StartsWith("exam"))
        {
            Dan ??= new DanHeader();
            int index = int.TryParse(split.Length > 4 ? split[4..] : "1", out index) ? index : Dan.Exam.Count + 1;
            index--; // Convert to 0-based index
            index += dangauge; // Offset by gauge exams
            if (index >= Dan.Exam.Count)
            {
                for (int i = Dan.Exam.Count; i <= index; i++)
                    Dan.Exam.Add("");
            }
            if (Exam.ExamName(Dan.Exam[index].Split(',')[0]) == Exam.ExamName(value.Split(',')[0]))
            {
                Dan.Exam[index] = value;
                return;
            }
            Dan.Exam[index] = value;
            //Dan.Exam.Add(value);
        }

        if (course >= courses.Length) return;
        var h = courses[course];
        h?.ReadCourse(line);
    }
    public void ReadCourse(string line)
    {
        string split = line.Split(':')[0].Trim();
        string value = line[(split.Length + 1)..].Trim();
        switch (split.ToLower())
        {

            case "level":
                Level = double.TryParse(value, out double level) ? level : 0;
                break;
            case "scoreinit":
                ScoreInit = double.TryParse(value.Split(',', 2)[0], out double scoreInit) ? scoreInit : 0;
                break;
            case "scorediff":
                ScoreDiff = double.TryParse(value, out double scoreDiff) ? scoreDiff : 0;
                break;
            case "total":
                Total = double.TryParse(value, out double total) ? total : 0;
                break;
            case "balloon":
                if (int.TryParse(value, out int balloon))
                {
                    Balloon.Add(balloon);
                }
                else if (value.Contains(','))
                {
                    Balloon.AddRange([.. value.Split(',').Select(s => int.TryParse(s.Trim(), out int b) ? b : 0)]);
                }
                break;
        }
    }
    public void SetWave(string file)
    {
        Wave = file;
        if (string.IsNullOrEmpty(Preview))
        {
            Preview = file;
        }

        //Length = AstrumLoom.DXLib.Sound.GetLength(WavePath);
    }

    public string WavePath
        => System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path) ?? "", Wave));

    public override string ToString() => string.IsNullOrEmpty(Title) && Level == 0
            ? "No Entry"
            : $"{Title}\n{SubTitle}\nLv.{Level}\nwave:{Wave}\ngenre:{string.Join(",", Genres)}";

    public bool NewSong => CreateTime > DateTime.Now.AddMonths(-1);
    public bool SemiNewSong => CreateTime > DateTime.Now.AddYears(-1);

    public (Color front, Color back, Color edge) GetTitleColor()
    {
        string genre = SongGenre.GetName(
            [.. Genres, .. SongGenre.FromSubtitle(SubTitle).Select(g => SongGenre.Name(g))]);
        var col = Color.Parse(SongGenre.Color(genre));
        var hsb = col.ToHSB();
        var edgecol = !string.IsNullOrEmpty(genre) ? Color.FromHSB(hsb.Hue, hsb.Saturation,
            hsb.Brightness * Easing.Ease(hsb.Saturation, 1, 0.2, 0.5)) : Color.Black;

        var front = Color.White;
        if (Difficulty == ECourse.Edit || (ECourse)course == ECourse.Edit)
        {
            var editcolor = Color.Parse("#7234d4");
            var ehsb = editcolor.ToHSB();
            double huediff = Math.Abs(hsb.Hue - ehsb.Hue) / 180.0;
            edgecol = Color.FromHSB(ehsb.Hue, ehsb.Saturation, ehsb.Brightness * Easing.Ease(huediff, 1, 1.3, 1));
            if (front.ToHSB().Saturation < 0.05)
            {

                if (hsb.Saturation < 0.05)
                {
                    col = Color.FromHSB(ehsb.Hue, ehsb.Saturation * 0.8, ehsb.Brightness);
                }
                else front = Color.FromHSB(ehsb.Hue - 16, ehsb.Saturation * Easing.Ease(huediff, 1, 0.2, 0.8), ehsb.Brightness * 1.25);
            }
        }
        //hsb = col.ToHSB();
        else if (NewSong || SemiNewSong)
        {
            var newcolor = NewSong ? Color.DeepPink : Color.DeepSkyBlue;
            var nhsb = newcolor.ToHSB();
            if (hsb.Saturation < 0.05)
            {
                col = Color.FromHSB(nhsb.Hue, nhsb.Saturation, nhsb.Brightness + 0.3);
                edgecol = Color.FromHSB(nhsb.Hue, nhsb.Saturation, nhsb.Brightness * 0.3);
                front = Color.FromHSB(nhsb.Hue, nhsb.Saturation * 0.2, nhsb.Brightness);
            }
            else
            {
                front = Color.FromHSB(nhsb.Hue, nhsb.Saturation * 0.6, nhsb.Brightness);
            }
        }

        return (front, col, edgecol);
    }
}

public class DanHeader
{
    public List<string> Exam = [];
}
