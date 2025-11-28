using AstrumLoom;

namespace TJALib.TJA;

public class Bar : BarBase
{
    public int NoteCount = 1;
    public double Scroll = 1;
    public int Branch = 0;
    public double Length = 0;
    public double BeatLen = 0;
    public bool Visible = true;
    public bool Drawable = true;
    public double VisibleTime;

    public List<Chip> Chips = [];
    public List<Command> Commands = [];

    public override string ToString()
    {
        string note = "";
        foreach (var chip in Chips)
        {
            bool commanding = false;
            foreach (var command in Commands)
            {
                if (command.Position == chip.Position)
                {
                    note += $"{(!commanding ? "\n" : "")}{command.Name}\n";
                    commanding = true;
                }
            }
            note += ((int)chip.Type).ToString();
        }
        return note + ",";
    }

    public Chip[] HitChip
        => [..Chips
            .Where(c => c.Type is > ENote.None and < ENote.Roll)
            .OrderBy(c => c.Time)
            ];


    // 既存の Bar クラス内に追加
    // Cached prefix times for chips (length == Chips.Count + 1)
    public double[]? ChipTimePrefix_WithDelay = null;
    public double[]? ChipTimePrefix_NoDelay = null;

    // Compute prefix arrays; initialBpm and initialMeasure are the bar's starting bpm/measure
    public void ComputeChipTimePrefix(double initialBpm, double initialMeasure)
    {
        int n = Chips.Count;
        double[] with = new double[n + 1];
        double[] without = new double[n + 1];
        with[0] = 0.0;
        without[0] = 0.0;
        double bpm = initialBpm;
        double measure = initialMeasure;

        for (int i = 0; i < n; i++)
        {
            // Apply commands at position i+1 before computing duration for chip i
            double extraDelay = 0.0;
            foreach (var comm in Commands)
            {
                if (comm.Position == i + 1)
                {
                    string name = comm.Name;
                    string value = comm.Value;
                    switch (name)
                    {
                        case "bpmchange":
                            if (double.TryParse(value, out double nbpm)) bpm = nbpm;
                            break;
                        case "measure":
                            if (!string.IsNullOrEmpty(value))
                            {
                                if (!value.Contains('/')) value += $"/{measure}";
                                string[] maj = value.Split('/');
                                if (maj.Length >= 2)
                                {
                                    if (double.TryParse(maj[1], out double measuremom) && double.TryParse(maj[0], out double m))
                                    {
                                        measure = measuremom / (m == 0 ? 1 : m);
                                    }
                                }
                            }
                            else measure = 1;
                            break;
                        case "delay":
                            if (double.TryParse(value, out double fdel))
                            {
                                double delMs = fdel * 1000.0;
                                if (i > 0) extraDelay += delMs;
                            }
                            break;
                    }
                }
            }

            double dur = 240000.0 / bpm / (measure == 0 ? 1 : measure) / (this.NoteCount <= 0 ? 1 : this.NoteCount);
            without[i + 1] = without[i] + dur;
            with[i + 1] = with[i] + extraDelay + dur;
        }

        ChipTimePrefix_WithDelay = with;
        ChipTimePrefix_NoDelay = without;
    }

    public void CalcEquallyMeasure()
    {
        if (Chips.Count == 0 || Length <= 0 || Length == double.PositiveInfinity)
        {
            return;
        }
        double measure = Rational.FromDouble(1.0 / (60000.0 / BPM / Length), 1, 0.0001).Value;
        double aqualbpm = 60000.0 / Length * Math.Min(4.0, measure); // 4分音符4拍の長さ
        aqualbpm = Math.Round(aqualbpm, 3, MidpointRounding.AwayFromZero); // 4/4基準にしたとき
        double avgbpm = Chips.Select(c => c.BPM).Average();

        try
        {
            // BPMが極端に違う場合は補正する
            double bf = Math.Round(avgbpm / aqualbpm, 0, MidpointRounding.AwayFromZero);
            if (bf > 1) aqualbpm *= bf;
            double aquallen = 240000.0 / aqualbpm; // 4分音符4拍の長さ(平均BPM補正後)
            double measurefromlen = Math.Round(Length / aquallen * 4.0, 3); // 現在の小節の長さを4分音符換算

            // 途中でBPM変更がある場合は置き換え
            bool bpmchange = Chips.Any(c => c.BPM != BPM);
            // 現在の拍子より正確であれば置き換え
            double aqualmeasure = 4.0 / Measure; // 何拍扱いか
            Rational nr = Rational.FromDouble(aqualmeasure, 100, 0.0001);
            Rational r = Rational.FromDouble(measurefromlen, 100, 0.0001);
            if (nr.Num > 5 && r.Den + nr.Den > 2 && bpmchange)
            {
                if (r.Den < nr.Den || r.Value < nr.Value)
                {
                    double bpmbase = 60000.0 / aqualbpm * r.Value;
                    var ldif = Rational.FromDouble(bpmbase / Length, 100, 0.0001);
                    double bpm = aqualbpm * ldif.Value;

                    var ch = Chips.Where(c => c.Hittable).ToArray();
                    if (ch.Length == 0) return;
                    double offset = Math.Round(ch.FirstOrDefault()?.Time - Time ?? 0, 0);
                    foreach (var chip in ch)//
                    {
                        chip.EquallyBPM = bpm;
                        chip.EquallyMeasure = 4.0 / r.Value;
                        var p = Rational.FromDouble(offset, 200, 0.0001);
                        chip.EquallyPos = p.Value;

                        double pos = 4.0 / chip.Separate;
                        offset += pos;// p.Value;
                    }
                    BeatLen = offset;
                }
            }
        }
        catch (Exception e)
        {
            Log.Write(e);
            throw;
        }
    }

    public Bar Clone() => (Bar)this.MemberwiseClone();
}

public class Command
{
    public int Position;
    public string Str = "";
    public string Name = "";
    public string Value = "";
    public int Branch = 0;

    public Command() { }
    public Command(string str)
    {
        Str = str;
        GetName();
    }

    public void GetName()
    {
        string[] split = Str[1..].Split(' ');
        var spls = split.ToList();
        spls.RemoveAt(0);
        Name = split[0].ToLower();
        Value = string.Join(" ", spls).Trim();

        string str = Str;
        if (!str.StartsWith("#")) return;

        string s = str[1..].ToLower();
        if (s.StartsWith("scroll"))
        {
            Name = "scroll";
            Value = str.Replace("#SCROLL", "").Trim();
        }
        else if (s.StartsWith("bpmchange"))
        {
            Name = "bpmchange";
            Value = str.Replace("#BPMCHANGE", "").Trim();
        }
        else if (s.StartsWith("measure"))
        {
            Name = "measure";
            Value = str.Replace("#MEASURE", "").Trim();
        }
        else if (s.StartsWith("delay"))
        {
            Name = "delay";
            Value = str.Replace("#DELAY", "").Trim();
        }
    }

    public override string ToString() => $"{Position} #{Name.ToUpper()},{Value}";
}