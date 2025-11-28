using AstrumLoom;

namespace TJALib.TJA;

public class TJA
{
    public string FilePath = "";
    public string[] Lines = [];
    private Header[] Headers
    {
        get; set;
    } = [new(), new(), new(), new(), new()];
    public CourseData?[] Courses
    {
        get; private set;
    } = new CourseData[5];

    public TJA() { }
    public TJA(string path)
    {
        try
        {
            path = GetTJAPath(path);
            if (!File.Exists(path))
            {
                Log.Warning($"ファイルが存在しません: {path}");
                return;
            }
            FilePath = path;
            switch (Path.GetExtension(path).ToLower())
            {
                case ".tja":
                case ".tjb":
                case ".tbc":
                    {
                        //Log.Write($"TJAファイルを読み込みます: {path}");
                        Lines = Read(path);
                        //Log.Write($"TJAファイルの読み込みが完了しました: (行数: {Lines.Length})");
                        //Log.Write($"TJAファイルの解析を開始します...");
                        Load();
                        //Log.Write($"TJAファイルの解析が完了しました。");
                        for (int i = 0; i < Courses.Length; i++)
                        {
                            var c = Courses[i];
                            //if (c != null)
                            //Log.Write($"[{(ECourse)i}] 譜面: あり\n" +
                            //    $"ヘッダー:\n{c.Header}\n" +
                            //    $"譜面行数: {c.Files.Count} " +
                            //    $"DP譜面: {(c.DPFiles != null ? (c.DPFiles[0].Count > 0 ? "1P " : "") + (c.DPFiles[1].Count > 0 ? "2P" : "") : "なし")}");
                            //else
                            //Log.Write($"[{(ECourse)i}] 譜面: なし");
                        }
                    }
                    break;
                case ".tbd":
                    {
                        Lines = Read(path);
                        LoadTbd();
                    }
                    break;
                default:
                    Log.Warning($"対応していないファイル形式です: {path}");
                    return;
            }
        }
        catch (Exception ex)
        {
            Log.Write(ex);
        }
    }

    private static string[] Read(string path)
    {
        var list = Text.Read(path);
        bool endcomma = false;
        for (int i = 0; i < list.Count; i++)
        {
            //コメント削除
            if (list[i].Replace("/ /", "//").Contains("//"))
            {
                string s = list[i].Replace("/ /", "//");
                list[i] = s[..(s.IndexOf("//") == -1 ? 0 : s.IndexOf("//"))];
            }
            //バグ対策
            if (list.Count > 0 && list[i] == "," && (i == 0 || endcomma))
                list[i] = list[i].Replace(",", "0,");
            if (!string.IsNullOrEmpty(list[i].Trim()) && !list[i].StartsWith("#")) endcomma = list[i].EndsWith(",");
            if (list[i].StartsWith("#START")) endcomma = true;
        }
        return [.. list];
    }

    private void Load()
    {
        int course = 3;
        int player = 0;
        bool isStart = false;
        Header header = new()
        {
            Path = FilePath,
            CreateTime = File.GetLastWriteTime(FilePath)
        };
        List<string> lines = [];
        foreach (string line in Lines)
        {
            course = header.course;
            if (course > 4 && Headers.Length == 5)
            {
                Headers = [.. Headers, new Header(), new Header()];
                Courses = [.. Courses, null, null];
            }
            if (line.Contains(':'))
            {
                if (!isStart)
                    header.Read(line, Headers);
            }
            else if (line.StartsWith('#'))
            {
                if (line == "#BMSCROLL")
                {
                    Headers[course].HBS = 1;
                }
                else if (line == "#HBSCROLL")
                {
                    Headers[course].HBS = 2;
                }
                if (line.StartsWith("#START"))
                {
                    if (line.Split(' ').Length > 1)
                    {
                        string side = line.Split(' ')[1].ToLower().Trim();
                        player = side is "1p" or "p1" ? 1 : side is "2p" or "p2" ? 2 : 0;
                    }
                    isStart = true;
                    lines.Clear();
                    continue;
                }
                if (line.StartsWith("#END"))
                {
                    isStart = false;

                    //if (course > 4) continue;
                    CourseData data = new()
                    {
                        Header = new(header, Headers[course])
                        {
                            Path = FilePath,
                            CreateTime = header.CreateTime
                        }
                    };

                    if (player > 0)
                    {
                        data.DPFiles ??= [[], []];
                        data.DPFiles[player - 1].AddRange(lines);
                    }
                    else data.Files.AddRange(lines);
                    if (lines.Count == 0) continue;
                    lines.Clear();
                    Courses[course] = data;
                    continue;
                }
            }
            if (isStart)
            {
                lines.Add(line.Trim());
            }
        }
    }

    private void LoadTbd()
    {

        var header = new Header
        {
            Path = FilePath,
            CreateTime = File.GetLastWriteTime(FilePath)
        };
        var data = new CourseData { Header = header };

        List<string[]> pendingSongGauge = [];
        int examCount = 1;
        int songCount = 0;
        List<Course> courses = [];
        header.Read("Course:6", Headers); // Dan扱い固定");

        foreach (string raw in Lines)
        {
            string line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // ヘッダ互換キーは既存の Read にそのまま投げる
            if (line.StartsWith("Title:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Genre:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Image:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Designer:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("BPM:", StringComparison.OrdinalIgnoreCase))
            {
                header.Read(line, Headers); // 既存ヘッダ反映 :contentReference[oaicite:8]{index=8}
                continue;
            }

            // 段位共通ゲージ → exam1: gauge,...
            if (line.StartsWith("Gauge:", StringComparison.OrdinalIgnoreCase))
            {
                string[] p = line[6..].Split(',');
                // red, gold, type, total
                string red = p.Length > 0 ? p[0].Trim() : "0";
                string gold = p.Length > 1 ? p[1].Trim() : "0";
                string type = p.Length > 2 ? p[2].Trim() : "Normal"; // Normal/Hard/EXHard
                string total = p.Length > 3 ? p[3].Trim() : "80";
                string examstr = $"Exam{examCount}:gauge,{red},{gold},0,{type},{total}";
                //data.Files.Add(examstr); // GaugeExam 仕様に整形 :contentReference[oaicite:9]{index=9}
                header.Read(examstr, Headers); // ヘッダにも反映
                examCount++;
                continue;
            }

            // 条件ゲージ予約（曲別ゲージも） → exax: type,...
            if (line.StartsWith("Exam:", StringComparison.OrdinalIgnoreCase))
            {
                string[] p = line[5..].Split(',');
                string type = p.Length > 0 ? p[0].Trim() : "jp";
                string red = p.Length > 1 ? p[1].Trim() : "0";
                string gold = p.Length > 2 ? p[2].Trim() : "0";
                string moreless = p.Length > 3 ? p[3].Trim() : "m";
                if (red.Split(['.', '/']).Length > 1)
                {
                    string[] rspl = red.Split(['/', '.']);
                    string[] gspl = gold.Split(['/', '.']);
                    List<string> songexam = [];
                    for (int i = 0; i < Math.Min(rspl.Length, gspl.Length); i++)
                    {
                        songexam.Add($"Exam{examCount}:{type},{rspl[i]},{gspl[i]},{moreless}");
                    }
                    pendingSongGauge ??= [];
                    pendingSongGauge.Add([.. songexam]);
                }
                else
                {
                    string examstr = $"Exam{examCount}:{type},{red},{gold},{moreless}";
                    //data.Files.Add(examstr);
                    header.Read(examstr, Headers);
                }
                examCount++;
                continue;
            }

            // 曲
            if (line.StartsWith("Song:", StringComparison.OrdinalIgnoreCase))
            {
                string[] p = line[5..].Split(',');
                string songPath = p.Length > 0 ? p[0].Trim() : "";
                string courseStr = p.Length > 1 ? p[1].Trim() : "Oni";
                // secret は今は保持だけ（必要なら Header.SubTitle へ "SECRET" を付加など）

                // 参照TJAを読み込み
                if (!string.IsNullOrEmpty(songPath))
                {
                    // 引用符を除去し、スラッシュを OS の区切り文字に正規化
                    songPath = songPath.Replace("\"", "").Replace("/", Path.DirectorySeparatorChar.ToString());

                    // パスが絶対でなければ、この TJA インスタンスの FilePath のディレクトリを基点として結合
                    if (!Path.IsPathRooted(songPath))
                    {
                        songPath = Path.Combine(Path.GetDirectoryName(FilePath) ?? "", songPath);
                    }

                    // ここで相対パスの `..` を解決して絶対パスにする（例: ../../）
                    try
                    {
                        songPath = Path.GetFullPath(songPath);
                    }
                    catch (Exception)
                    {
                        // Path.GetFullPath が失敗した場合は元の文字列を残す（既存の挙動に合わせる）
                    }

                    // 結合したパスを GetTJAPath で解決
                    string resolvedPath = GetTJAPath(songPath);

                    // 解決できなければ以下を順に試す:
                    // - 結合パスそのものが存在するか
                    // - 拡張子が無ければ拡張子 .tja を付けたファイルが存在するか
                    if (!File.Exists(resolvedPath))
                    {
                        if (!Path.HasExtension(resolvedPath))
                        {
                            resolvedPath += ".tja";
                        }
                    }
                    //Log.Debug($"参照TJAパス解決: 元='{path}' → 解決='{resolvedPath}'");

                    // 最終的に得られたパス（存在しない場合は元の songPath 文字列）を使って new TJA(...) を作る
                    var child = new TJA(File.Exists(resolvedPath) ? resolvedPath : songPath);
                    int cidx = Course.GetCourse(courseStr); // 既存のコースインデックス化
                    var cc = child.GetCourse(cidx, enablesearch: false, set: false);
                    if (cc != null)
                    {
                        courses.Add(cc);
                    }
                }
                continue;
            }
        }

        foreach (var cc in courses)
        {
            string wave = Path.GetRelativePath(Path.GetDirectoryName(FilePath) ?? "", cc?.Header.WavePath ?? FilePath);
            // 曲見出し（#NEXTSONG）— 既存Danのパーサ仕様に合わせる
            // title,subtitle,genre,wave,scoreinit,scorediff,course,level
            data.Files.Add($"#NEXTSONG {cc?.Header.Title},{cc?.Header.SubTitle},{cc?.Header.Genre},{wave}," +
                $"{cc?.Header.ScoreInit},{cc?.Header.ScoreDiff},{cc?.Difficulty},{cc?.Header.Level}");

            // 曲別ゲージが予約されていれば先に挿入
            if (pendingSongGauge != null)
            {
                foreach (string[] exam in pendingSongGauge)
                {
                    if (exam.Length > songCount)
                        data.Files.Add(exam[songCount]);
                }
            }
            songCount++;
            data.Header.Balloon.AddRange(cc?.Header.Balloon ?? []);
            data.Files.Add($"#BPMCHANGE {cc?.Header.BPM}");
            data.Files.Add($"#DELAY {-cc?.Header.Offset}");
            data.Files.Add($"#GOGOEND");
            data.Files.Add($"#MEASURE 4/4");
            data.Files.Add($"#SCROLL 1");
            data.Files.Add($"#BARLINEON");
            data.Files.Add($"#RESETCOMMAND");

            // 譜面本文をそのまま追加（Dan側で1曲として束ねられる）
            if (cc != null)
                data.Files.AddRange(cc.Files[0]);
        }

        // 段位レーン（index 6）に置くと GetDan() が拾う
        if (Courses.Length == 5)
        {
            var newCourses = new CourseData?[7];
            Array.Copy(Courses, newCourses, Courses.Length);
            Courses = newCourses;

            var newHeaders = new Header[7];
            Array.Copy(Headers, newHeaders, Headers.Length);
            Headers = newHeaders;
        }
        Courses[6] = data; // :contentReference[oaicite:10]{index=10}
    }

    public Course?[] GetCourses(bool enablesearch = false, bool set = true)
    {
        var courses = new Course?[Courses.Length];
        for (int i = 0; i < Courses.Length; i++)
        {
            courses[i] = GetCourse(i, enablesearch, set);
        }
        return courses;
    }
    public Course? GetCourse(int course, bool enablesearch = false, bool set = true)
    {
        if (course < 0 || course >= Courses.Length) return null;
        var data = Courses[enablesearch ? EnableCourse(course) : course];
        return data == null ? null : new Course(data, course, set);
    }
    public Dan? GetDan()
    {
        if (Courses.Length < 6) return null;
        try
        {
            var data = Courses[6];
            return data == null ? null : new Dan(data);
        }
        catch (Exception e)
        {
            Log.Write(e);
            Log.Error("段位認識に失敗しました。");
            return null;
        }
    }

    public int EnableCourse(int level)
    {
        if (Courses.Length > 5 && Courses[6] != null) return 6;
        int dif = 0;
        bool[] enable = Enable;
        for (int i = 0; i < enable.Length; i++)
        {
            int d = SearchLane(level)[dif];
            if (enable[d]) return d;
            dif++;
        }
        return level;
    }
    public static int[] SearchLane(int level)
    {
        int[] lane = new int[5];
        switch (level)
        {
            case 0:
                lane = [0, 1, 2, 3, 4];
                break;
            case 1:
                lane = [1, 0, 2, 3, 4];
                break;
            case 2:
                lane = [2, 1, 0, 3, 4];
                break;
            case 3:
                lane = [3, 4, 2, 1, 0];
                break;
            case 4:
            case 5:
            case 6:
                lane = [4, 3, 2, 1, 0];
                break;
        }
        return lane;
    }

    public bool[] Enable
    {
        get
        {
            bool[] enable = new bool[5];
            if (Courses.Length == 7)
                return [Courses[6] != null];
            for (int i = 0; i < Courses.Length; i++)
            {
                enable[i] = Courses[i] != null;
            }
            return enable;
        }
    }

    public override string ToString()
    {
        bool[] enable = Enable;
        string[] data = new string[5];
        for (int i = 0; i < enable.Length; i++)
        {
            data[i] = enable[i] ? $"[{(ECourse)i}]" : $"[]";
        }
        return $"{Courses[EnableCourse(3)]?.Header.Title}:{string.Join("-", data)}";
    }

    public static string GetTJAPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";

        // 相対パス・`..` を解決して絶対パスに変換（失敗時は元の path を使う）
        try
        {
            path = Path.GetFullPath(path);
        }
        catch (Exception)
        {
            // 無理に例外を上げず既存の挙動に合わせる
        }

        try
        {
            if (File.Exists(path))
            {
                string ext = Path.GetExtension(path);
                if (ext.Equals(".tja", StringComparison.CurrentCultureIgnoreCase) ||
                    ext.Equals(".tjb", StringComparison.CurrentCultureIgnoreCase) ||
                    ext.Equals(".tbc", StringComparison.CurrentCultureIgnoreCase) ||
                    ext.Equals(".tbd", StringComparison.CurrentCultureIgnoreCase))
                {
                    return path;
                }
                // 存在するファイルだが拡張子が違う場合はそのまま返す（既存の挙動維持）
                Log.Warning($"TJAファイルではないファイルが指定されました: {path}");
                return "";
            }

            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, "*.tja", SearchOption.TopDirectoryOnly);
                string[] tjbfiles = Directory.GetFiles(path, "*.tjb", SearchOption.TopDirectoryOnly);
                string[] tbcFiles = Directory.GetFiles(path, "*.tbc", SearchOption.TopDirectoryOnly);
                string[] tbdFiles = Directory.GetFiles(path, "*.tbd", SearchOption.TopDirectoryOnly);
                files = [.. files, .. tjbfiles, .. tbcFiles, .. tbdFiles];
                return files.Length > 0 ? files.OrderBy(x => x).First() : "";
            }

            // パスが存在しないが拡張子がない場合、.tja を付けて試す
            if (!Path.HasExtension(path))
            {
                string tryPath = path + ".tja";
                try
                {
                    tryPath = Path.GetFullPath(tryPath);
                }
                catch { }
                if (File.Exists(tryPath)) return tryPath;
            }
        }
        catch (Exception)
        {
            // IO 関連の例外はログだけにして下位互換的に空文字を返す
        }

        Log.Warning($"TJAファイルのパス解決に失敗しました: {path}");
        return "";
    }
}
