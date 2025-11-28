using AstrumLoom;

namespace TJALib.TJA;

public class Dan
{
    public string FilePath = "";
    public Header Header = new();
    public DanSong[] Songs = [];
    public List<Exam> Exams = [];
    public EClass Class = EClass.None;

    public Dan() { }
    public Dan(CourseData data, string path = "")
    {
        FilePath = path;
        Header = data.Header;
        List<DanSong> songs = [];
        CourseData course = new();
        DanSong song = new();
        var danhead = data.Header.Dan;
        if (danhead != null)
        {
            for (int i = 0; i < danhead.Exam.Count; i++)
            {
                string examLine = danhead.Exam[i];
                //if (string.IsNullOrEmpty(examLine)) continue;
                if (Exam.ExamName(examLine.Split(',')[0].Trim()) == EExam.Gauge)
                {
                    Exams.Add(new GaugeExam(examLine));
                }
                else
                {
                    Exams.Add(new(examLine));
                }
            }
        }


        // ローカル関数: 文字列に特定のキーワードが含まれているか（大文字小文字無視）
        static bool ContainsAny(string source, params string[] keys)
        {
            if (string.IsNullOrWhiteSpace(source)) return false;
            string low = source.ToLowerInvariant();
            foreach (string k in keys)
            {
                if (low.Contains(k, StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }

        // ローカル関数: 文字列から EClass を推測（部分一致で判定）
        static EClass MapToClass(string src)
        {
            if (string.IsNullOrWhiteSpace(src)) return EClass.None;
            // 日本語・英語の候補を含め、部分一致で判定する
            if (ContainsAny(src, "復活", "挑戦")) return EClass.Gaiden;
            if (ContainsAny(src, "kyu_1", "段位-薄木", "初級", "十級", "九級", "八級", "七級", "六級")) return EClass.Kyu_1;
            if (ContainsAny(src, "kyu_2", "段位-濃級", "五級", "四級", "三級", "二級", "一級")) return EClass.Kyu_2;
            if (ContainsAny(src, "dan_1", "段位-黒", "初段", "二段", "三段", "四段", "五段")) return EClass.Dan_1;
            if (ContainsAny(src, "dan_2", "段位-赤", "六段", "七段", "八段", "九段", "十段")) return EClass.Dan_2;
            if (ContainsAny(src, "jin_1", "段位-銀", "玄人", "名人", "超人")) return EClass.Jin_1;
            if (ContainsAny(src, "jin_2", "段位-金", "達人", "皆伝")) return EClass.Jin_2;
            return ContainsAny(src, "gaiden", "段位-外伝", "外伝") ? EClass.Gaiden : EClass.None;
        }

        // Genre による判定（部分一致で OK）
        if (!string.IsNullOrEmpty(data.Header?.Genre))
        {
            var mapped = MapToClass(data.Header.Genre);
            if (mapped != EClass.None)
                Class = mapped;
        }

        // Title による判定（部分一致で OK、Title の判定は Genre の判定を上書きする）
        if (!string.IsNullOrEmpty(data.Header?.Title))
        {
            var mapped = MapToClass(data.Header.Title);
            if (mapped != EClass.None)
                Class = mapped;
        }

        int c = 3;
        foreach (string line in data.Files)
        {
            string split = line.Split(':')[0].Trim().ToLower();
            string value = !line.Equals(split, StringComparison.CurrentCultureIgnoreCase) ? line[(split.Length + 1)..].Trim() : "";

            if (line.StartsWith("#NEXTSONG"))
            {
                try
                {
                    if (course.Files.Count > 0)
                    {
                        Course cse = new(course, c, true);
                        songs.Add(new()
                        {
                            Course = cse,
                            Exams = [.. song.Exams]
                        });
                        data.Header?.Balloon.RemoveRange(0, Math.Min(cse.BalloonCount, data.Header.Balloon.Count));
                        song.Exams.Clear();
                    }
                    course = new();
                    if (data.Header != null) course.Header = new(data.Header);
                    course.Header.Path = data.Header?.Path ?? "";
                    string[] spl = line[9..].Trim().Split(',');
                    if (spl.Length > 0) course.Header.Title = spl[0];
                    if (spl.Length > 1) course.Header.SubTitle = spl[1];
                    if (spl.Length > 2)
                    {
                        course.Header.Genre = spl[2];
                        course.Header.Genres.Add(spl[2]);
                    }
                    if (spl.Length > 3) course.Header.SetWave(spl[3]);
                    if (spl.Length > 4) double.TryParse(spl[4], out course.Header.ScoreInit);
                    if (spl.Length > 5) double.TryParse(spl[5], out course.Header.ScoreDiff);
                    if (spl.Length > 6) int.TryParse(spl[6], out c);
                    if (spl.Length > 7) double.TryParse(spl[7], out course.Header.Level);

                    course.Header.course = c;
                    course.Header.Balloon = [.. data.Header?.Balloon ?? []];
                }
                catch (Exception ex)
                {
                    Log.Error($"Dan Course Parse Error: {ex.Message}");
                }
            }
            else if (split.StartsWith("exam"))
            {
                int index = int.TryParse(split.Length > 4 ? split[4..] : "1", out index) ? index : 1;
                try
                {
                    index--;
                    if (index < Header.Dan?.Exam.Count &&
                        Exam.ExamName(Header.Dan.Exam[index].Split(',')[0].Trim()) != Exam.ExamName(value.Split(',')[0].Trim()))
                    {
                        index = Header.Dan?.Exam.Count ?? Exams.Count;
                    }
                    if (index >= song.Exams.Count)
                    {
                        for (int i = song.Exams.Count; i <= index; i++)
                            song.Exams.Add(null);
                    }
                    song.Exams[index] = Exam.ExamName(value.Split(',')[0].Trim()) == EExam.Gauge
                        ? new GaugeExam(value)
                        : new(value)
                        {
                            Alone = true,
                            Index = songs.Count,
                        };
                    if (index >= Exams.Count)
                    {
                        Exams.Add(song.Exams[index] ?? new(""));
                    }
                    if (index < Exams.Count && Exams[index].Name == song.Exams[index]?.Name)
                    {
                        Exams[index].Alone = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Dan Exam Parse Error at {index}: {ex.Message}");
                }
            }
            else
            {
                course.Files.Add(line);
            }
        }
        songs.Add(new()
        {
            Course = new(course, c),
            Exams = song.Exams
        });
        if (songs.Count > 0)
        {
            Songs = songs.ToArray();
        }
    }

    public Course[] Courses => [.. Songs.Select(d => d.Course ?? new Course())];

    public int TotalNotes => Courses.Sum(c => c.Notes);

    public int GetExam(int index, bool isGold = false)
    {
        if (index < 0 || index >= Exams.Count) return 0;
        var exam = Exams[index];
        if (!exam.Alone)
        {
            return isGold ? exam.GoldNumber : exam.RedNumber;
        }
        else
        {
            int total = 0;
            foreach (var song in Songs)
            {
                var ex = song.Exams.ElementAtOrDefault(index);
                if (ex != null)
                {
                    total += isGold ? ex.GoldNumber : ex.RedNumber;
                }
            }
            return total;
        }
    }
    public string GetExamString(int index, bool isGold = false)
    {
        if (index < 0 || index >= Exams.Count) return "0";
        var exam = Exams[index];
        if (exam.Alone)
        {
            string[] parts = [];
            foreach (var song in Songs)
            {
                var ex = song.Exams.ElementAtOrDefault(index);
                if (ex != null)
                {
                    parts = [.. parts, (isGold ? ex.GoldNumber : ex.RedNumber).ToString()];
                }
            }
            return string.Join("/", parts);
        }
        return GetExam(index, isGold).ToString();
    }

    public Rader TotalRader
    {
        get
        {
            var r = new Rader();
            foreach (var rader in Courses.Select(c => c.NoteRader))
            {
                r.Notes += rader.Notes / Courses.Length;
                r.Peak += rader.Peak / Courses.Length;
                r.Stream += rader.Stream / Courses.Length;
                r.Rhythm += rader.Rhythm / Courses.Length;
                r.Soflan += rader.Soflan / Courses.Length;
                r.Gimmick += rader.Gimmick / Courses.Length;
            }
            return r;
        }
    }

    public Color Color => Class switch
    {
        EClass.Kyu_1 => Color.Parse("#f1c89c"),//#f1c89c
        EClass.Kyu_2 => Color.Parse("#fece72"),//#d18240
        EClass.Dan_1 => Color.Parse("#4aa8b7"),//#373028
        EClass.Dan_2 => Color.Parse("#f45336"),//#da1902
        EClass.Jin_1 => Color.Parse("#aabbc7"),//#a2a19d
        EClass.Jin_2 => Color.Parse("#ffda43"),//#ffd213
        EClass.Gaiden => Color.Parse("#4b9b84"),//#fff7bc
        _ => Color.Parse("#1764b3"),
    };
}

public class DanSong
{
    public Course Course = new();
    public List<Exam?> Exams = [];
}

public class Exam
{
    public EExam Name;
    public int Value;
    public int MaxSize;
    public int RedNumber;
    public int GoldNumber;
    public bool isLess;
    public ESuccess Success;

    public bool Alone;
    public int Index = -1;

    public Exam(string line)
    {
        string[] parts = line.Split(',');
        if (parts.Length > 0) Name = ExamName(parts[0].Trim());
        if (parts.Length > 1) RedNumber = Number(parts[1].Trim());
        if (parts.Length > 2) GoldNumber = Number(parts[2].Trim());
        if (parts.Length > 3) isLess = MoreLess(parts[3].Trim());
    }

    public override string ToString() => $"{Name,6} : {Value} ({RedNumber}/{GoldNumber} {(isLess ? "Less" : "More")}{(Alone ? $" {Index + 1}" : "")})";

    #region Read
    public static int GaugeType(string str)
    {
        return str.ToLower() switch
        {
            "1" or "hard" => 1,
            "2" or "exhard" => 2,
            _ => 0,
        };
    }

    public static EExam ExamName(string str)
    {
        return str.ToLower() switch
        {
            "1" or "gr" or "great" => EExam.Great,
            "2" or "gd" or "jg" or "good" => EExam.Good,
            "3" or "b" or "bad" => EExam.Bad,
            "4" or "pr" or "poor" => EExam.Poor,
            "5" or "pg" or "perfectgreat" or "jp" or "l" or "light" => EExam.Light,
            "6" or "bp" or "badpoor" or "jb" or "m" or "miss" => EExam.Miss,
            "7" or "e" or "score" => EExam.Score,
            "8" or "s" or "oldscore" => EExam.OldScore,
            "9" or "r" or "roll" => EExam.Roll,
            "10" or "h" or "hit" => EExam.Hit,
            "11" or "c" or "combo" => EExam.Combo,
            "12" or "g" or "gauge" => EExam.Gauge,
            _ => EExam.Perfect,
        };
    }

    public static string ExamName(EExam exam) => exam switch
    {
        EExam.Perfect => "良+の数",
        EExam.Great => "良の数",
        EExam.Good => "可の数",
        EExam.Bad => "不可の数",
        EExam.Poor => "不可-の数",
        EExam.Light => "良以上の数",
        EExam.Miss => "不可以下の数",
        EExam.Score => "スコア",
        EExam.OldScore => "スコア(AC15)",
        EExam.Roll => "連打数",
        EExam.Hit => "叩いた数",
        EExam.Combo => "最大コンボ",
        EExam.Gauge => "魂ゲージ",
        _ => "Unknown",
    };

    public static int Number(string str) => str.Contains('.') || string.IsNullOrEmpty(str) ? 0 : int.Parse(str);
    public static List<int> SongNumber(string str)
    {
        if (str.Contains('.'))
        {
            string[] arr = str.Split('.');
            List<int> result = [];
            for (int i = 0; i < arr.Length; i++)
            {
                result.Add(int.Parse(arr[i]));
            }
            return result;
        }
        else return [];
    }

    public static bool MoreLess(string str)
    {
        return str.ToLower() switch
        {
            "1" or "l" or "less" => true,
            _ => false,
        };
    }
    #endregion

    public double Rate
    {
        get
        {
            if (isLess)
            {
                return RedNumber == 0 ? 100.0 : Math.Max(0.0, (double)(RedNumber - Value) / RedNumber);
            }
            else
            {
                return RedNumber == 0 ? 0.0 : Math.Min(1.0, (double)Value / RedNumber);
            }
        }
    }

    public Color Color => Success switch
    {
        ESuccess.Gold => Color.Gold,
        ESuccess.Red => Color.Red,
        ESuccess.None => Color.White,
        _ => Color.DimGray,
    };
}
public class GaugeExam : Exam
{
    public int Gauge;//0:Normal,1:Hard,2:EXHard
    public double Total;

    public GaugeExam(string line) : base(line)
    {
        string[] parts = line.Split(',');
        if (parts.Length > 4) Gauge = GaugeType(parts[4].Trim());
        if (parts.Length > 5) double.TryParse(parts[5].Trim(), out Total);
    }
}

public enum EExam
{
    /// <summary> 良+ </summary>
    Perfect,
    /// <summary> 良 </summary>
    Great,
    /// <summary> 可 </summary>
    Good,
    /// <summary> 不可 </summary>
    Bad,
    /// <summary> 不可- </summary>
    Poor,
    /// <summary> 良以上 </summary>
    Light,//Perfect+Great
    /// <summary> 不可以下 </summary>
    Miss,//Bad+Poor
    /// <summary> EXスコア </summary>
    Score,
    /// <summary> AC15スコア </summary>
    OldScore,
    /// <summary> 連打 </summary>
    Roll,
    /// <summary> 叩いた数 </summary>
    Hit,
    /// <summary> 最大コンボ </summary>
    Combo,
    /// <summary> ノルマゲージ </summary>
    Gauge
}

public enum ESuccess
{
    Failed,
    None,
    Red,
    Gold
}

public enum EClass
{
    None,
    /// <summary> 薄級位 初級～六級 </summary>
    Kyu_1,
    /// <summary> 濃級位 五級～一級 </summary>
    Kyu_2,
    /// <summary> 黒段位 初段～五段 </summary>
    Dan_1,
    /// <summary> 赤段位 六段～十段 </summary>
    Dan_2,
    /// <summary> 人段位(銀) 玄人、名人、超人 </summary>
    Jin_1,
    /// <summary> 人段位(金) 達人、皆伝 </summary>
    Jin_2,
    /// <summary> 外伝段位 </summary>
    Gaiden
}
