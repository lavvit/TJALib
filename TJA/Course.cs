using AstrumLoom;

namespace TJALib.TJA;

public class CourseData
{
    public Header Header { get; set; } = new Header();
    public List<string> Files { get; set; } = [];
    public List<string>[]? DPFiles { get; set; } = null;
}

public class Course
{
    public Header Header = new();
    public List<string>[] Files { get; set; } = [];
    public int Difficulty { get; private set; }

    // キャッシュ用フィールド
    private readonly Dictionary<string, List<Chip>>
    _chipsCache = [];

    // GogoList の内部保持とロック（外部から直接変更されないようカプセル化）
    private readonly object _gogoLock = new();
    private List<(bool On, double Time)> _gogoList = [];
    //互換のため読み取り専用ビューを提供
    public IReadOnlyList<(bool On, double Time)> GogoList => _gogoList;

    // スレッド安全なアクセサ
    public void AddGogo(bool on, double time)
    {
        lock (_gogoLock) { _gogoList.Add((on, time)); }
    }
    public void ClearGogo()
    {
        lock (_gogoLock) { _gogoList.Clear(); }
    }
    public (bool On, double Time)[] GetGogoSnapshot()
    {
        lock (_gogoLock) { return _gogoList.ToArray(); }
    }

    public Course() { }
    public Course(CourseData data, int course = 3, bool set = true)
    {
        Header = data.Header;
        Files = data.DPFiles != null ? [data.Files, data.DPFiles[0], data.DPFiles[1]] : [data.Files];
        Difficulty = course;

        if (set)
        {
            Read();
            Set();
        }
    }
    public Course Clone()
    {
        Course c = new()
        {
            Header = Header,
            Files = Files,
            Difficulty = Difficulty
        };
        c.Read();
        c.Set();
        return c;
    }

    public bool Enable
    {
        get
        {
            bool enable = false;
            for (int i = 0; i < Lanes.Length; i++)
            {
                enable |= IsEnable(i);
            }
            return enable;
        }
    }

    public bool Setted => NoteRader.Notes > 0 || Files.Length == 0;

    public bool IsDP => IsEnable(1) && IsEnable(2);
    public bool DPOnly => IsDP && !IsEnable(0);
    public bool IsBranch => BranchList[0].Count > 0;
    public bool IsEnable(int i) => i < Lanes.Length && Lanes[i].Length > 0;

    public int NoteCount(int i)
    {
        if (!IsEnable(i)) return 0;

        int n = 0;
        foreach (var lane in Lanes[i])
        {
            if (lane == null) continue;
            foreach (var chip in lane.Chips)
            {
                if (chip.Time <= Length && chip.Type > ENote.None && chip.Type < ENote.Roll && InBranch(chip)) n++;
            }
        }
        return n;
    }

    private int BarCount(int player = 0)
    {
        int n = 0;
        if (Files.Length == 0) return 0;
        var text = Files.Length > 1 && Files[player].Count > 0 ? Files[player] : Files[0];
        if (text == null || text.Count == 0) return 0;
        foreach (string line in text)
        {
            if (!line.StartsWith("#"))
            {
                foreach (char note in line)
                {
                    if (note == ',')
                    {
                        n++;
                    }
                }
            }
        }
        return n;
    }

    public string BPMText
    {
        get
        {
            double header = Header.BPM == 0 ? 120 : Header.BPM;
            string headerStr = $"{Math.Round(header, 3, MidpointRounding.AwayFromZero):0.##}";
            if (BpmList.Count == 0) return headerStr;
            if (BpmList.Count == 1) return $"{headerStr} - {Math.Round(BpmList[0].Value, 3, MidpointRounding.AwayFromZero):0.##}";
            double minBpm = Math.Round(BpmList.Min(b => b.Value), 3, MidpointRounding.AwayFromZero);
            double maxBpm = Math.Round(BpmList.Max(b => b.Value), 3, MidpointRounding.AwayFromZero);
            return $"{minBpm:0.##} - {maxBpm:0.##}";
        }
    }

    public int IsHBS { get; private set; } = 0;//1:BMS 2:HBS

    public List<(int line, int pos)>[] BalloonList = [[]];

    public int Notes { get; private set; } = 0;
    public double Length { get; set; } = 0;
    public double ChipLength { get; private set; } = 0;
    public double ChipLenNoRoll { get; private set; } = 0;

    public double RollLength { get; private set; } = 0;
    public int BalloonAmount { get; private set; } = 0;
    public int BalloonCount { get; private set; } = 0;
    public int RollCount { get; private set; } = 0;

    public Bar[][] Lanes = [];
    public List<(int line, int pos)>[] LongList = [[]];
    public List<BPM> BpmList = [];
    public List<Delay> DelayList = [];
    public List<Branch>[] BranchList = [[]];

    public Rader NoteRader = new();

    //既存の外部 API を壊さないためにプロパティはそのままにし、GetChips 内でキャッシュする
    public List<Chip> AllChips => [.. Lanes.SelectMany(l => l).SelectMany(b => b?.Chips ?? [])];
    public List<Chip> Chips => GetChips(0, 0);
    public List<Chip> HitChips => GetChips(1, 0);
    public List<Chip> RollChips => GetChips(2, 0);

    private static string _CacheKey(int type, int side, bool branch) => $"{type}_{side}_{(branch ? 1 : 0)}";

    public List<Chip> GetChips(int type = 0, int side = 0, bool branch = true)
    {
        string key = _CacheKey(type, side, branch);
        if (_chipsCache.TryGetValue(key, out var cached))
            return cached;

        // Lanes が存在しない場合は空リストを返す
        List<Chip> chips = [];
        if (Lanes.Length > side && Lanes[side] != null)
            chips = [.. Lanes[side].SelectMany(b => b.Chips)];

        if (branch) chips = [.. chips.Where(InBranch)];
        var result = type switch
        {
            1 => chips
                                .Where(c => c.Type > ENote.None && c.Type < ENote.Roll && c.Time <= Length)
                                .OrderBy(c => c.Time * 1000)
                                .ToList(),
            2 => chips
                                .Where(c => c.Type >= ENote.Roll && c.LongEnd != null && c.Time <= Length)
                                .OrderBy(c => c.Time * 1000)
                                .ToList(),
            _ => chips,
        };

        // キャッシュに保存して返す
        _chipsCache[key] = result;
        return result;
    }
    public void ClearChips() => _chipsCache.Clear();

    public List<Bar> Bars
    {
        get
        {
            if (Lanes.Length == 0) return [];
            List<Bar> bars = [];
            bars.AddRange(Lanes[0].Where(InBranch));
            return bars;
        }
    }

    public bool InBranch(Chip chip)
    {
        if (chip.Branch == 0) return true;
        if (chip.BranchHit != null) return chip.BranchHit ?? true;
        var branches = BranchList[0];
        int left = 0, right = branches.Count - 1;
        Branch? found = null;
        while (left <= right)
        {
            int mid = (left + right) / 2;
            var b = branches[mid];
            if (chip.Time < b.Start)
                right = mid - 1;
            else if (chip.Time >= b.End)
                left = mid + 1;
            else
            {
                found = b;
                break;
            }
        }
        found ??= branches.Count > 0 ? branches[0] : null;
        if (found != null && found.Now >= 0)
        {
            chip.BranchHit = chip.Branch == found.Now + 1;
            return chip.BranchHit ?? true;
        }
        return chip.Branch == (found != null ? found.BaseLane() + 1 : 1);
    }
    public bool InBranch(Bar bar)
    {
        if (bar.Branch == 0) return true;
        var branch = BranchList[0].LastOrDefault(b => bar.Time >= b.Start && bar.Time < b.End) ?? BranchList[0][0];
        return branch.Now >= 0 ? bar.Branch == branch.Now + 1 : bar.Branch == branch.BaseLane() + 1;
    }

    public override string ToString() => $"{Header.Title} {ToShortString()}";
    public string ToShortString() => $"{(ECourse)Difficulty} Lv.{Header.Level} {Notes}Notes{(Header.Designer != "" ? $" by.{Header.Designer}" : "")}";


    public void Read()
    {
        // Lanes を再構築するのでキャッシュをクリアしておく
        _chipsCache.Clear();

        if (Files.Length == 0) return;
        Lanes = Files.Length > 1 ? [new Bar[BarCount(0)], new Bar[BarCount(1)], new Bar[BarCount(2)]] : [new Bar[BarCount()]];

        LongList = new List<(int, int)>[Lanes.Length];
        BalloonList = new List<(int, int)>[Lanes.Length];
        BranchList = new List<Branch>[Lanes.Length];
        IsHBS = Header.HBS;
        for (int l = 0; l < Lanes.Length; l++)
        {
            LongList[l] = [];
            BalloonList[l] = [];
            BranchList[l] = [];
            var lane = Lanes[l];
            int n = 0, b = 0;
            List<(int line, int pos)> longs = [];
            var text = Files.Length > 1 && Files[l].Count > 0 ? Files[l] : Files[0];
            if (text == null || text.Count == 0) continue;
            foreach (string line in text)
            {
                if (!line.StartsWith("#"))
                {
                    foreach (char note in line)
                    {
                        if (note is >= '0' and <= '9')
                        {
                            n++;
                            if (b < lane.Length)
                            {
                                Chip chip = new()
                                {
                                    Type = (ENote)int.Parse(note.ToString()),
                                    Position = n,
                                    Bar = b + 1,
                                };

                                if (lane[b] == null) lane[b] = new Bar() { Number = b + 1 };
                                lane[b].Chips.Add(chip);
                                if (note == '8')
                                {
                                    if (longs.Count > 0)
                                    {
                                        var end = longs[^1];
                                        if (lane[end.line].Chips[end.pos - 1].LongEnd == null)
                                            lane[end.line].Chips[end.pos - 1].LongEnd = new Chip()
                                            {
                                                Type = lane[end.line].Chips[end.pos - 1].Type,
                                                Position = n,
                                                Bar = b + 1,
                                            };
                                    }
                                }
                                else
                                {
                                    if (note >= '5')
                                    {
                                        var nowlong = longs.Count > 0 ?
                                        lane[longs[^1].line].Chips[longs[^1].pos - 1] : null;
                                        if (nowlong == null || nowlong.Type < ENote.Roll || nowlong.LongEnd != null)
                                        {
                                            longs.Add((b, n));
                                            LongList[l].Add((b, n));
                                            if (note is '7' or '9')
                                                BalloonList[l].Add((b, n));
                                        }
                                    }
                                }
                            }

                        }
                        if (note is >= 'A' and <= 'E')
                        {
                            n++;
                            if (b < lane.Length)
                            {
                                var chip = new Chip()
                                {
                                    Position = n,
                                    Bar = b + 1,
                                };
                                switch (note)
                                {
                                    case 'A': chip.Type = ENote.Don; break;
                                    case 'B': chip.Type = ENote.Ka; break;
                                    case 'C': chip.Type = ENote.DON; break;
                                    case 'D': chip.Type = ENote.KA; break;
                                    case 'E': chip.Type = ENote.End; break;
                                }
                                if (lane[b] == null) lane[b] = new Bar() { Number = b + 1 };
                                lane[b].Chips.Add(chip);
                                if (note == 'E')
                                {
                                    if (longs.Count > 0)
                                    {
                                        var end = longs[^1];
                                        lane[end.line].Chips[end.pos - 1].LongEnd = new Chip()
                                        {
                                            Type = lane[end.line].Chips[end.pos - 1].Type,
                                            Position = n,
                                            Bar = b + 1,
                                        };
                                    }
                                }
                                else
                                {
                                    longs.Add((b, n));
                                    LongList[l].Add((b, n));
                                }
                            }

                        }
                        if (note == ',')
                        {
                            if (lane[b] == null)
                            {
                                lane[b] = new Bar() { Number = b + 1 };
                            }
                            lane[b].NoteCount = n;
                            if (lane[b].NoteCount == 0)
                            {
                                n++;
                                var chip = new Chip()
                                {
                                    Type = ENote.None,
                                    Position = n,
                                    Bar = b + 1,
                                };
                                lane[b].Chips.Add(chip);
                                lane[b].NoteCount = n;
                            }
                            n = 0;
                            b++;
                        }
                    }
                }
                else
                {
                    if (line == "#BMSCROLL")
                    {
                        IsHBS = 1;
                    }
                    else if (line == "#HBSCROLL")
                    {
                        IsHBS = 2;
                    }
                    else
                    {
                        Command command = new(line)
                        {
                            Position = n + 1,
                        };
                        if (b < lane.Length)
                        {
                            if (lane[b] == null) lane[b] = new Bar() { Number = b + 1 };
                            lane[b].Commands.Add(command);
                        }
                    }
                }
            }
        }
    }

    public static double Parse(string str)
    {
        // 頑強な数値パーサ：タイポや全角文字・区切り文字の違いにも寛容にする
        // 手順（擬似コード）:
        // 1. null/空チェック -> 0 を返す
        // 2. 前後空白除去
        // 3. Unicodeの全角数字や全角小数点等を半角に正規化（例: '０'->'0', '．'->'.', '，'->','）
        // 4. 正規化後、まずは InvariantCulture で直接 TryParse（許容する形式を広めに）
        // 5. 失敗したら正規表現で最初に見つかる「数値っぽい部分」を抽出
        //    - パターン例: [-+]?\d+([.,]\d+)?([eE][-+]?\d+)? 
        // 6. 抽出した部分はカンマを小数点に統一して再度 Parse（InvariantCulture）
        // 7. それでも駄目なら現在カルチャーや NumberStyles を変えて試す
        // 8. すべて失敗したら 0 を返す
        if (string.IsNullOrWhiteSpace(str)) return 0;

        // trim
        str = str.Trim();

        // 全角→半角などの簡単な正規化マップ
        // 全角数字、全角小数点・カンマ、全角符号などを置換
        var sb = new System.Text.StringBuilder(str.Length);
        foreach (char ch in str)
        {
            // 全角数字 ０-９
            if (ch is >= '０' and <= '９')
            {
                sb.Append((char)('0' + (ch - '０')));
                continue;
            }
            // 全角英数のマイナス・プラス・小数点・カンマ
            if (ch is '．' or '。') { sb.Append('.'); continue; }
            if (ch is '，' or '、') { sb.Append(','); continue; }
            if (ch is '－' or '﹣' or '⁻') { sb.Append('-'); continue; }
            if (ch == '＋') { sb.Append('+'); continue; }
            // 全角英字をそのまま ASCII に (簡易)
            if (ch is >= 'Ａ' and <= 'Ｚ') { sb.Append((char)('A' + (ch - 'Ａ'))); continue; }
            if (ch is >= 'ａ' and <= 'ｚ') { sb.Append((char)('a' + (ch - 'ａ'))); continue; }
            sb.Append(ch);
        }
        string norm = sb.ToString();

        // まずは直接パース（小数点は '.' を想定。カンマがある場合は Invariant では失敗するので後段で処理）
        if (double.TryParse(norm, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out double dval))
            return dval;

        // 次に、数値らしい部分を抽出する（小数点がコンマになっているケースなどに対応）
        try
        {
            var m = System.Text.RegularExpressions.Regex.Match(norm, @"[-+]?\d+([.,]\d+)?([eE][-+]?\d+)?");
            if (m.Success)
            {
                string token = m.Value;
                // カンマを小数点に統一（"1,234" の千区切りを誤変換するリスクはあるが、寛容に扱うため優先）
                // もし複数カンマを含むなら、先頭以外のカンマを削るロジックも入れておく
                int commaCount = token.Count(c => c == ',');
                if (commaCount > 0)
                {
                    // "123,456" のように桁区切りで使われている場合を少しだけ考慮：
                    // - 小数点と思われる位置が1つだけで、かつその位置より右に3桁しかない場合は千区切りの可能性が高い。
                    // ただし簡潔のため、基本は最後のカンマを小数点に、それ以前は削除する。
                    if (commaCount == 1)
                    {
                        // 1つのカンマのみなら小数点と仮定して '.' に置換
                        token = token.Replace(',', '.');
                    }
                    else
                    {
                        // 複数カンマ：最後のカンマのみ小数点、それ以前のカンマは削除
                        int last = token.LastIndexOf(',');
                        string before = token[..last].Replace(",", "");
                        string after = token[(last + 1)..];
                        token = before + "." + after;
                    }
                }

                // もう一度 InvariantCulture で試す
                if (double.TryParse(token, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out dval))
                    return dval;

                // 最後の手段：現在カルチャーでも試す
                if (double.TryParse(token, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.CurrentCulture, out dval))
                    return dval;
            }
        }
        catch
        {
            // 失敗しても無視して下で 0 を返す
        }

        return 0;
    }

    private static double Recalc(double val) => Rational.FromDouble(val, 10000, 0.0001).Value;
    public void Set()
    {
        // 計算の前にキャッシュをクリア
        _chipsCache.Clear();

        DelayList = [];
        Length = Header.Length;

        // Set の開始時点で GogoList を初期化
        ClearGogo();

        for (int l = 0; l < Lanes.Length; l++)
        {
            if (!IsEnable(l)) continue;

            var lane = Lanes[l];
            double t = -Header.Offset * 1000.0;
            double start = -1;

            double beat = 0;
            double bpm = Header.BPM == 0 ? 120 : Header.BPM;
            double scroll = 1;
            double measure = 1;
            double measuremom = 1;
            bool barline = true;
            Branch? branch = null;
            int[] balloonlist = [.. Header.Balloon];
            int balloon = 0;
            int branchnum = 0;
            int barnum = 1;

            bool warped = false;

            for (int i = 0; i < lane.Length; i++)
            {
                var bar = lane[i];
                bar.Number = barnum;
                bar.Time = t;
                bar.Beat = beat;
                bar.BPM = bpm;
                bar.Measure = measure;
                bar.Scroll = scroll;
                bar.Visible = barline;
                bar.Branch = branchnum;
                // 事前にプレフィックスを計算しておく
                bar.ComputeChipTimePrefix(bpm, measure);

                double off = 0;

                for (int j = 0; j < bar.Chips.Count; j++)
                {
                    #region Command
                    foreach (var comm in bar.Commands)
                    {
                        if (comm.Position == j + 1)
                        {
                            string name = comm.Name;
                            string value = comm.Value;
                            double ct = bar.Time + GetTime(bar, comm.Position - 1);
                            switch (name)
                            {
                                case "scroll":
                                    scroll = Parse(value);
                                    break;
                                case "bpmchange":
                                    double prev = bpm;
                                    bpm = Parse(value);
                                    //bpm = Math.Round(bpm, 2, MidpointRounding.AwayFromZero);
                                    if (bpm != prev) BPM.Add(this, ct, beat, bpm);
                                    break;
                                case "measure":
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        if (!value.Contains('/')) value += $"/{measuremom}";
                                        string[] maj = value.Split('/');
                                        measuremom = Parse(maj[1]);
                                        double m = Parse(maj[0]);
                                        measure = measuremom / (m == 0 ? 1 : m);
                                    }
                                    else measure = 1;
                                    break;
                                case "gogostart":
                                    AddGogo(true, ct);
                                    break;
                                case "gogoend":
                                    AddGogo(false, ct);
                                    break;
                                case "barlineon":
                                    barline = true;
                                    break;
                                case "barlineoff":
                                    barline = false;
                                    break;
                                case "delay":
                                    double del = Parse(value);
                                    off = del * 1000.0;
                                    if (del > 0)
                                        DelayList.Add(new() { Time = ct, Length = del * 1000.0, Beat = beat, Target = beat + del * 1000.0 / (60000 / bpm) });
                                    break;
                                case "branchstart":
                                    branchnum = 0;
                                    bar.Branch = branchnum;
                                    if (branch != null)
                                    {
                                        if (branch.End < bar.Time)
                                        {
                                            branch.End = bar.Time;
                                        }
                                        t = bar.Time;
                                        bar.Time = t;
                                        var brstart = branch.StartBarStatus[branch.BaseLane()];
                                        bar.Beat = brstart.Beat;
                                        bar.BPM = brstart.BPM;
                                        bar.Scroll = brstart.Scroll;
                                        bar.Measure = brstart.Measure;
                                        branch = null;
                                    }
                                    var refbar = Bars.Count > 0 ? Bars.FirstOrDefault(b => b.Number == bar.Number - 1,
                                        Bars.OrderBy(b => b.Time).LastOrDefault(b => b.Time < bar.Time, bar)) : bar;
                                    branch = new(comm.Value)
                                    {
                                        Number = barnum,
                                        Start = bar.Time,
                                        JudgeTime = refbar.Time,
                                        End = bar.Time,
                                    };
                                    if (branch.StartBarStatus[0].Time == 0)
                                    {
                                        for (int b = 0; b < 3; b++)
                                        {
                                            Bar branchbar = BranchList[l].Count > 0 ?
                                            BranchList[l][^1].StartBarStatus[b] : bar.Clone();
                                            branch.StartBarStatus[b] = branchbar;
                                        }
                                    }
                                    BranchList[l].Add(branch);
                                    break;
                                case "branchend":
                                    branchnum = 0;
                                    bar.Branch = branchnum;
                                    if (branch != null)
                                    {
                                        if (branch.End < bar.Time)
                                        {
                                            branch.End = bar.Time;
                                        }
                                        if (branch.Start != branch.End) // 開始と終了が同じなら無効
                                        {
                                            t = bar.Time;
                                            beat = bar.Beat;
                                            barnum = branch.Max;
                                            bar.Number = barnum;
                                            bar.Time = t;
                                            var brstart = branch.StartBarStatus[branch.BaseLane()];
                                            bpm = brstart.BPM;
                                            measure = brstart.Measure;
                                            scroll = brstart.Scroll;
                                            bar.BPM = bpm;
                                            bar.Measure = measure;
                                            bar.Scroll = scroll;

                                            bar.Branch = branchnum;
                                        }
                                        branch = null;
                                    }
                                    break;
                                case "n":
                                case "e":
                                case "m":
                                    if (branch != null)
                                    {
                                        if (comm.Name == "n") branchnum = 1;
                                        if (comm.Name == "e") branchnum = 2;
                                        if (comm.Name == "m") branchnum = 3;
                                        bar.Branch = branchnum;

                                        if (branch.End < bar.Time)
                                        {
                                            branch.End = bar.Time;
                                        }
                                        barnum = branch.Number;
                                        bar.Number = barnum;
                                        t = branch.Start;
                                        bar.Time = t;
                                        var brstart = branch.StartBarStatus[branchnum - 1];
                                        beat = brstart.Beat;
                                        bpm = brstart.BPM;
                                        measure = brstart.Measure;
                                        scroll = brstart.Scroll;
                                        bar.BPM = bpm;
                                        bar.Measure = measure;
                                        bar.Scroll = scroll;
                                    }
                                    break;

                                    #region Gimmick

                                    #endregion
                            }
                            comm.Branch = branchnum;
                            if (j == 0)
                            {
                                if (off != 0)
                                {
                                    if (i > 0) lane[i - 1].Length += off;
                                    bar.Time = t + off;
                                    if (off < 0)
                                        beat += off / (60000 / bar.BPM);
                                    bar.Beat = beat;
                                    off = 0;
                                }
                                bar.BPM = bpm;
                                bar.Measure = measure;
                                bar.Scroll = scroll;
                                bar.Visible = barline;
                                bar.ComputeChipTimePrefix(bpm, measure);
                            }
                            bar.Measure = measure;
                        }
                    }
                    #endregion

                    var chip = bar.Chips[j];

                    chip.Bar = barnum;
                    chip.BPM = bpm;
                    chip.Scroll = scroll;
                    chip.Branch = branchnum;
                    chip.Measure = bar.Measure;
                    if (chip.Type is ENote.Balloon or ENote.Potato)
                    {
                        chip.RollMax = balloon < balloonlist.Length ? balloonlist[balloon] : 5;
                        balloon++;
                    }

                    double cipt = GetTime(bar, j);
                    double time = bar.Time + cipt;

                    chip.Time = time;
                    if (warped)
                    {
                        chip.Time += 0.0001; // 時間が同じチップが連続する場合の微調整
                        warped = false;
                    }
                    if (chip.BPM * chip.Measure < 0.0)
                    {
                        // 前のTimeを引き継ぐ
                        double prev = GetTime(bar, j - 1);
                        chip.Time = bar.Time + prev;
                        warped = true;
                    }
                    chip.Gogo = IsGogo(time);
                    chip.VisibleTime = GetGreenNumber(chip);
                    Length = Header.Length == 0 ? chip.Time : Header.Length;

                    if (start < 0) start = chip.Time;
                    if (chip.Type > ENote.None && chip.Time - start > ChipLength && chip.Time <= Length)
                    {
                        ChipLength = chip.Time - start;
                        if (chip.Type < ENote.Roll && ChipLength > ChipLenNoRoll)
                            ChipLenNoRoll = ChipLength;
                    }

                    double cibt = GetBeat(bar, j);
                    chip.Beat = bar.Beat + cibt;

                    double bt = (double)j / bar.Chips.Count;
                    chip.BeatMeasure = Math.Round(bar.Number - 1 + bt, 5, MidpointRounding.AwayFromZero);
                    chip.BeatPos = Math.Round(bt * (4 / bar.Measure), 5, MidpointRounding.AwayFromZero);

                    double wid = 4 / bar.Measure / bar.Chips.Count;
                    beat += wid;

                    if (branch != null && chip.Branch > 0)
                    {
                        branch.ChipCount[chip.Branch - 1]++;
                    }
                }

                bar.VisibleTime = GetGreenNumber(bar);
                bar.Length = GetTime(bar, bar.Chips.Count);
                t = bar.Time + bar.Length;

                bar.BeatLen = GetBeat(bar, bar.Chips.Count);
                beat = bar.Beat + bar.BeatLen;
                //if (bar.Measure * bar.BPM > 0)

                if (branch != null && branchnum > 0)
                {
                    branch.StartBarStatus[branchnum - 1] = bar.Clone();
                    branch.Lanes[branchnum - 1].Add(bar);
                }
                barnum++;
            }
            var longs = LongList[l];
            for (int i = 0; i < longs.Count; i++)
            {
                var (line, pos) = longs[i];
                if (line < lane.Length && pos - 1 < lane[line].Chips.Count)
                {
                    var chip = lane[line].Chips[pos - 1].LongEnd;
                    if (chip != null)
                    {
                        var lbar = lane[chip.Bar - 1];
                        var endchip = lane[chip.Bar - 1].Chips[chip.Position - 1];
                        chip.Time = endchip.Time;
                        chip.Beat = endchip.Beat;
                        chip.BPM = endchip.BPM;
                        chip.Scroll = endchip.Scroll;
                    }
                }
            }
        }
        if (!Enable) return;
        var chips = AllChips.Where(c => c.Hittable).ToList();
        foreach (var chip in chips)
        {
            chip.Time = chip.Time > 0.001 ? Math.Round(chip.Time, 5, MidpointRounding.AwayFromZero) : chip.Time;
            chip.BPM = Recalc(chip.BPM > 0.001 ? Math.Round(chip.BPM, 5, MidpointRounding.AwayFromZero) : chip.BPM);
            chip.Scroll = Recalc(chip.Scroll > 0.001 ? Math.Round(chip.Scroll, 5, MidpointRounding.AwayFromZero) : chip.Scroll);
            chip.Beat = Math.Round(chip.Beat, 5, MidpointRounding.AwayFromZero);
            chip.BeatMeasure = Recalc(Math.Round(chip.BeatMeasure, 5, MidpointRounding.AwayFromZero));
            chip.BeatPos = Recalc(Math.Round(chip.BeatPos, 5, MidpointRounding.AwayFromZero));
        }
        int hand = 1;
        for (int i = 1; i < chips.Count; i++)
        {
            Chip L = chips[i - 1], R = chips[i];
            L.Next = R;
            L.Length = Recalc(Math.Round(R.Time - L.Time, 5, MidpointRounding.AwayFromZero));
            R.RelativeTime = R.Time - chips[0].Time;

            if (L.Length <= 20)
            {
                L.NeedHand = hand;
                hand += 1;// L.Type >= ENote.DON ? 2 : 1;
                continue;
            }
            if (L.Type >= ENote.DON) hand++;
            L.NeedHand = hand;
            hand = 1;
        }

        int cmp = 0;
        int[] remain = [];
        foreach (var chip in chips)
        {
            cmp++;
            chip.Composite = cmp;

            if (chip.Next != null)
            {
                double cs = chip.Separate;
                double ns = chip.Next.Separate;
                double diff = cs > ns ? cs / Math.Max(1, ns) : ns / Math.Max(1, cs);

                for (int r = 0; r < remain.Length; r++)
                {
                    if (remain[r] > 0) remain[r]--;
                    if (remain[r] == 0)
                    {
                        cmp = 0; // 複合カウントをリセット
                        remain = [.. remain.Where((v, idx) => idx != r)];
                    }
                }

                if (diff > 1.5)
                {
                    if (ns > cs) cmp = 0; // 複合カウントをリセット
                    if (ns < cs) remain = [.. remain, 1]; // 次の音符をリセット
                }
                else if (cs < 4.0 && chip.Length > 200)
                {
                    cmp = 0; // 4分未満は単音扱い
                }
                else if (chip.Length > 600)
                {
                    cmp = 0; // 600ms以上は単音扱い
                }
            }
        }

        var bars = Lanes.Length > 0 ? Lanes[0].Where(InBranch).ToList() : [];
        foreach (var bar in bars)
        {
            bar.CalcEquallyMeasure();
        }

        Notes = NoteCount(0);
        Length = Math.Round(Length, 2);
        var c = Chips // 1,2,3,4のみ
        .Where(c => c.Type > ENote.None && c.Time <= Length && InBranch(c))
        .OrderBy(c => c.Time)
        .ToList();
        if (c.Count == 0) return;
        double len = c.Last().Time - c.First().Time;
        ChipLength = Math.Round(len, 2);
        double clen = (HitChips.LastOrDefault() ?? new()).Time - (HitChips.FirstOrDefault() ?? new()).Time;
        ChipLenNoRoll = Math.Round(len, 2);
        CalcRoll();
        NoteRader = new(this);
        SetSE();
        Calculate();
    }


    public void CalcRoll()
    {
        double length = 0.0;
        int balloon = 0;
        int count = 0;
        int ballcnt = 0;
        foreach (var note in RollChips)
        {
            if (note.RollMax == 0)
            {
                length += (note.EndTime - note.Time) / 1000.0;
            }
            else
            {
                balloon += note.RollMax;
                ballcnt++;
            }
            count++;
        }
        RollLength = Math.Round(Recalc(length), 2);
        BalloonAmount = balloon;
        RollCount = count;
        BalloonCount = ballcnt;
    }

    public double GetTime(Bar bar, int current, bool nodelay = false, bool prefix = true)
    {
        if (bar.BPM == 0 || bar.Measure == 0) return 0;
        if (bar.Chips.Count == 0)
            return 240000.0 / bar.BPM / bar.Measure;

        if (prefix)
        {
            // キャッシュがあれば O(1) で返す
            if (nodelay && bar.ChipTimePrefix_NoDelay != null)
            {
                if (current >= 0 && current < bar.ChipTimePrefix_NoDelay.Length)
                    return bar.ChipTimePrefix_NoDelay[current];
            }
            else if (!nodelay && bar.ChipTimePrefix_WithDelay != null)
            {
                if (current >= 0 && current < bar.ChipTimePrefix_WithDelay.Length)
                    return bar.ChipTimePrefix_WithDelay[current];
            }
        }

        // フォールバック（元の逐次計算）
        return GetTime(bar, current);
    }
    public double GetTime(Bar bar, int current)
    {
        if (current == 0) return 0;
        double t = 0;
        double bpm = bar.BPM;
        for (int i = 0; i <= current; i++)
        {
            #region Command
            foreach (var comm in bar.Commands)
            {
                if (comm.Position == i + 1)
                {
                    string name = comm.Name;
                    string value = comm.Value;
                    switch (name)
                    {
                        case "bpmchange":
                            if (float.TryParse(value, out float fbpm)) bpm = fbpm;
                            else double.TryParse(value, out bpm);
                            break;
                        case "delay":
                            double del = 0;
                            if (float.TryParse(value, out float fdel)) del = fdel * 1000.0;
                            else if (double.TryParse(value, out double ddel)) del = ddel * 1000.0;
                            if (i > 0) t += del;
                            break;
                    }
                }
            }
            #endregion
            if (i < current)
            {
                t += 240000.0 / bpm / bar.Measure / bar.NoteCount;
            }
        }
        return t;
    }

    public double GetBeat(Bar bar, int current)
    {
        if (current == 0) return 0;
        double beat = 0;
        double bpm = bar.BPM;
        for (int i = 0; i <= current; i++)
        {
            #region Command
            foreach (var comm in bar.Commands)
            {
                if (comm.Position == i + 1)
                {
                    string name = comm.Name;
                    string value = comm.Value;
                    switch (name)
                    {
                        case "bpmchange":
                            if (float.TryParse(value, out float fbpm)) bpm = fbpm;
                            else double.TryParse(value, out bpm);
                            break;
                        case "delay":
                            double del = 0;
                            if (float.TryParse(value, out float fdel)) del = fdel * 1000.0;
                            else if (double.TryParse(value, out double ddel)) del = ddel * 1000.0;
                            if (i > 0)
                            {
                                beat += del / (60000 / bpm);
                            }
                            break;
                    }
                }
            }
            #endregion
            if (i < current)
            {
                beat += 4.0 / bar.Measure / bar.NoteCount;
            }
        }
        return beat;
    }


    public static int GetGreenNumber(Chip chip, double plusminus = 0)
    {
        double bpm = chip.BPM;
        double scroll = chip.Scroll + plusminus;
        int[] Showms = [240000, -30000];
        int ms = scroll > 0 ? Showms[0] : Showms[1];
        int sudden = 0;
        double suddenrate = 1000.0 / (1000 - sudden);
        return (int)(ms / (bpm * scroll * suddenrate));
    }
    private static int GetGreenNumber(Bar bar, double plusminus = 0)
    {
        double bpm = bar.BPM;
        double scroll = bar.Scroll + plusminus;
        int[] Showms = [240000, -30000];
        int ms = scroll > 0 ? Showms[0] : Showms[1];
        int sudden = 0;
        double suddenrate = 1000.0 / (1000 - sudden);
        return (int)(ms / (bpm * scroll * suddenrate));
    }

    #region Set
    public void SetBalloon()
    {
        int n = 0;
        foreach (var (line, pos) in BalloonList[0])
        {
            Lanes[0][line].Chips[pos - 1].RollMax = n < Header.Balloon.Count ? Header.Balloon[n] : 5;
            n++;
        }
    }

    public void SetSE()
    {
        List<Chip> list = [];
        foreach (var bar in Lanes[0])
        {
            foreach (var chip in bar.Chips)
            {
                if (chip.Type > ENote.None) list.Add(chip);
            }
        }

        const int DATA = 3;
        int doco_count = 0;
        var sort = new ENote[7];
        double[] time = new double[7];
        double[] scroll = new double[7];
        double time_tmp;

        for (int i = 0; i < list.Count; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                if (i + (j - 3) < 0)
                {
                    sort[j] = (ENote)(-1);
                    time[j] = -1000000000;
                    scroll[j] = 1.0;
                }
                else if (i + (j - 3) >= list.Count)
                {
                    sort[j] = (ENote)(-1);
                    time[j] = 1000000000;
                    scroll[j] = 1.0;
                }
                else
                {
                    sort[j] = list[i + (j - 3)].Type;
                    time[j] = list[i + (j - 3)].Time / (15000.0 / list[i + (j - 3)].BPM);
                    scroll[j] = list[i + (j - 3)].Scroll;
                }
            }
            time_tmp = time[DATA];
            for (int j = 0; j < 7; j++)
            {
                time[j] = Math.Round((time[j] - time_tmp) * scroll[j], 3, MidpointRounding.AwayFromZero);
                if (time[j] < 0)
                {
                    time[j] *= -1;
                }
            }

            //if (ignoreSENote && list[i].IsFixedSENote) continue;

            switch (list[i].Type)
            {
                case ENote.Don:

                    //（左2より離れている｜）_右2_右ドン_右右4_右右ドン…
                    if (time[DATA - 1] > 2/* || (sort[DATA-1] != 1 && time[DATA-1] >= 2 && time[DATA-2] >= 4 && time[DATA-3] <= 5)*/ && time[DATA + 1] == 2 && time[DATA + 2] == 4 && sort[DATA + 2] == ENote.Don && time[DATA + 3] == 6 && sort[DATA + 3] == ENote.Don)
                    {
                        list[i].SE = 1;
                        doco_count = 1;
                        break;
                    }
                    //ドコドコ中_左2_右2_右ドン
                    else if (doco_count != 0 && time[DATA - 1] == 2 && time[DATA + 1] == 2 && (sort[DATA + 1] == ENote.Don || sort[DATA + 1] == ENote.Don))
                    {
                        list[i].SE = doco_count % 2 == 0 ? 1 : 2;

                        doco_count++;
                        break;
                    }
                    else
                    {
                        doco_count = 0;
                    }

                    //8分ドコドン
                    if (time[DATA - 2] >= 4.1 && time[DATA - 1] == 2 && time[DATA + 1] == 2 && time[DATA + 2] >= 4.1 && sort[DATA - 1] == ENote.Don && sort[DATA + 1] == ENote.Don)
                    {
                        if (list[i].BPM >= 120.0)
                        {
                            list[i - 1].SE = 1;
                            list[i].SE = 2;
                            list[i + 1].SE = 0;
                            break;
                        }
                        else if (list[i].BPM < 120.0)
                        {
                            list[i - 1].SE = 0;
                            list[i].SE = 0;
                            list[i + 1].SE = 0;
                            break;
                        }
                    }

                    //BPM120以下のみ
                    //8分間隔の「ドドド」→「ドンドンドン」

                    if (time[DATA - 1] >= 2 && time[DATA + 1] >= 2)
                    {
                        if (list[i].BPM < 120.0)
                        {
                            list[i].SE = 0;
                            break;
                        }
                    }

                    //ドコドコドン
                    if (time[DATA - 3] >= 3.4 && time[DATA - 2] == 2 && time[DATA - 1] == 1 && time[DATA + 1] == 1 && time[DATA + 2] == 2 && time[DATA + 3] >= 3.4 && sort[DATA - 1] == ENote.Don && sort[DATA + 1] == ENote.Don && sort[DATA + 2] == ENote.Don)
                    {
                        list[i - 2].SE = 1;
                        list[i - 1].SE = 2;
                        list[i + 0].SE = 1;
                        list[i + 1].SE = 2;
                        list[i + 2].SE = 0;
                        i += 2;
                        //break;
                    }
                    //ドコドン
                    else if (time[DATA - 2] >= 2.4 && time[DATA - 1] == 1 && time[DATA + 1] == 1 && time[DATA + 2] >= 2.4 && sort[DATA - 1] == ENote.Don && sort[DATA + 1] == ENote.Don)
                    {
                        list[i].SE = 2;
                    }
                    //右の音符が2以上離れている
                    else if (time[DATA + 1] > 2)
                    {
                        list[i].SE = 0;
                    }
                    //右の音符が1.4以上_左の音符が1.4以内
                    else if (time[DATA + 1] >= 1.4 && time[DATA - 1] <= 1.4)
                    {
                        list[i].SE = 0;
                    }
                    //右の音符が2以上_右右の音符が3以内
                    else if (time[DATA + 1] >= 2 && time[DATA + 2] <= 3)
                    {
                        list[i].SE = 0;
                    }
                    //右の音符が2以上_大音符
                    else
                    {
                        list[i].SE = time[DATA + 1] >= 2 && (sort[DATA + 1] == ENote.DON || sort[DATA + 1] == ENote.KA) ? 0 : 1;
                    }
                    break;
                case ENote.Ka:
                    doco_count = 0;

                    //BPM120以下のみ
                    //8分間隔の「ドドド」→「ドンドンドン」
                    if (time[DATA - 1] == 2 && time[DATA + 1] == 2)
                    {
                        if (list[i - 1].BPM < 120.0 && list[i].BPM < 120.0 && list[i + 1].BPM < 120.0)
                        {
                            list[i].SE = 3;
                            break;
                        }
                    }

                    //右の音符が2以上離れている
                    if (time[DATA + 1] > 2)
                    {
                        list[i].SE = 3;
                    }
                    //右の音符が1.4以上_左の音符が1.4以内
                    else if (time[DATA + 1] >= 1.4 && time[DATA - 1] <= 1.4)
                    {
                        list[i].SE = 3;
                    }
                    //右の音符が2以上_右右の音符が3以内
                    else if (time[DATA + 1] >= 2 && time[DATA + 2] <= 3)
                    {
                        list[i].SE = 3;
                    }
                    //右の音符が2以上_大音符
                    else
                    {
                        list[i].SE = time[DATA + 1] >= 2 && (sort[DATA + 1] == ENote.DON || sort[DATA + 1] == ENote.KA) ? 3 : 4;
                    }
                    break;
                default:
                    doco_count = 0;
                    break;
            }
        }
    }

    public void Calculate()
    {
        if (Header.ScoreInit + Header.ScoreDiff > 0) return;
        var (Init, Diff) = CalcDefault(GetPoint(), 4, 16);
        Header.ScoreInit = Init;
        Header.ScoreDiff = Diff;
    }


    public int Calculate(double init, double diff, double roll)
    {
        int p = 0;

        int c = 0;
        foreach (var lane in Lanes[0])
        {
            foreach (var n in lane.Chips)
            {
                if (n.Type is > ENote.None and < ENote.Roll)
                {
                    double score = init;
                    c++;
                    if (c >= 10) score += diff;
                    if (c >= 30) score += diff;
                    if (c >= 50) score += diff * 2;
                    if (c >= 100)
                        score += diff * 4;

                    if (n.Gogo)
                        score *= 1.2;
                    if (n.Type is ENote.DON or ENote.KA) score *= 2.0;
                    if (c % 100 == 0) score += 10000;
                    score = (int)(score / 10) * 10;
                    p += (int)score;
                }
            }
        }
        p += CalcRoll(roll);

        return p;
    }

    public (double Init, double Diff) CalcDefault(int target, double ratio, double roll)
    {
        double r = 0;
        int c = 0;
        double t = target;
        foreach (var lane in Lanes[0])
        {
            foreach (var n in lane.Chips)
            {
                if (n.Type is > ENote.None and < ENote.Roll)
                {
                    double score = ratio;
                    c++;
                    if (c >= 10) score += 1;
                    if (c >= 30) score += 1;
                    if (c >= 50) score += 1 * 2;
                    if (c >= 100)
                        score += 1 * 4;

                    if (n.Gogo)
                        score *= 1.2;
                    if (n.Type is ENote.DON or ENote.KA) score *= 2.0;
                    if (c % 100 == 0) t -= 10000;
                    r += score;
                }
            }
        }
        int cr = CalcRoll(roll, false);
        double d = (t - cr) / r;

        return (Math.Ceiling(ratio * d / 10) * 10, Math.Ceiling(d * 2) / 2.0);
    }

    public int CalcRoll(double roll, bool inc = true)
    {
        int p = 0;
        foreach (var lane in Lanes[0])
        {
            foreach (var n in lane.Chips)
            {
                if (n.Type >= ENote.Roll)
                {
                    double score = 0;
                    double len = n.LongEnd != null ? n.LongEnd.Time - n.Time : 0;
                    int hit = (int)(roll * len / 1000.0) + (inc ? 1 : 0);
                    for (int i = 0; i < hit; i++)
                    {
                        double s = 0;
                        if (n.RollMax > 0)
                        {
                            if (i + 1 == n.RollMax) s = 5000;
                            else if (i < n.RollMax) s = 300;
                            else break;
                        }
                        else s = 100;
                        if (IsGogo(1000.0 / roll * i)) s *= 1.2;
                        score += s;
                    }
                    if (n.Type == ENote.ROLL) score *= 2.0;
                    p += (int)score;
                }
            }
        }
        return p;
    }
    #endregion

    public void Reset()
    {
        foreach (var chip in AllChips)
        {
            chip.Reset();
        }

        foreach (var branch in BranchList.SelectMany(b => b))
        {
            branch.Force = -1;
            branch.Value = 0;
        }
        ClearChips();
    }

    public bool IsGogo(double time)
    {
        var snapshot = GetGogoSnapshot();
        bool go = false;
        foreach (var (On, Time) in snapshot)
        {
            if (time < Time) return go;
            go = On;
        }
        return go;
    }

    public int GetPoint()
    {
        int l = (int)Header.Level;
        if (l < 0) l = 0;
        return Difficulty switch
        {
            0 => 280000 + 20000 * (l > 5 ? 5 : l),
            1 => 350000 + 50000 * (l > 7 ? 7 : l),
            2 => 500000 + 50000 * (l > 8 ? 8 : l),
            _ => l >= 10 ? 1200000 : 650000 + 50000 * (l > 9 ? 9 : l),
        };
    }
    public static int GetCourse(string str)
    {
        bool ret = int.TryParse(str, out int nCourse);
        if (ret) return nCourse;
        return str.ToLower() switch
        {
            "easy" => 0,
            "normal" => 1,
            "hard" => 2,
            "oni" => 3,
            "edit" => 4,
            "tower" => 5,
            "dan" => 6,
            _ => 3,
        };
    }

    public static string GetColor(ECourse course, bool enable = true)
    {
        string[] col = ["#ff4000", "#80ff40", "#00c0ff", "#ff00c0", "#c000ff", "#804000", "#0040c0"];
        if (!enable) col = ["#802000", "#408020", "#006080", "#800060", "#600080", "#402000", "#002060"];
        return course switch
        {
            ECourse.Easy => col[0],
            ECourse.Normal => col[1],
            ECourse.Hard => col[2],
            ECourse.Oni => col[3],
            ECourse.Edit => col[4],
            ECourse.Tower => col[5],
            ECourse.Dan => col[6],
            _ => "White",
        };
    }
}
public enum ECourse
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
    Oni = 3,
    Edit = 4,
    Tower = 5,
    Dan = 6,
}
