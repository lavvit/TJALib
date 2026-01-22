using AstrumLoom;

namespace TJALib.TJA;

public class Chip : ChipBase
{
    // X方向のスクロール
    public double Scroll = 1;
    // Y方向のスクロール
    public double ImidiateScroll = 0;
    public double Speed => Math.Sqrt(Scroll * Scroll + ImidiateScroll * ImidiateScroll);

    public ENote Type;
    public bool Gogo;
    public int SE = -1;
    public Chip? LongEnd = null;

    public Chip? Next = null;

    public double EndTime
        => LongEnd != null ? LongEnd.Time : Time;

    public double Measure;
    public int Bar;
    public int Position;
    public int Face;

    public double? EquallyBPM = null;
    public double? EquallyMeasure = null;
    public double? EquallyPos = null;

    // 複合の何番目か(1始まり、1なら単独ノーツ)
    public int Composite = 1;

    public double RelativeTime;
    public double Length;

    // 小節を均等に分割したときの位置
    public double BeatMeasure;
    // 小節頭からの位置(4分音符=1.0)
    public double BeatPos;

    public bool Hit;
    public bool Miss;

    // 判定時間(特訓モード用、リセットでも変わらない)
    public double? HitTime = null;

    // あらかじめ入力を予約しておく時間(オート・リプレイ用)
    public double? ReservedTime = null;
    public List<double>? ReservedRollTime = null;

    public double VisibleTime;
    // オートが叩いたかどうか
    public bool Appeared = false;

    // 連打・風船用
    public int RollMax;
    public int RollValue;

    // 分岐用
    public int Branch;
    public int BranchNum = -1;
    public bool? BranchHit = null;

    // ロングノーツ判定用
    public bool Pushing;
    public bool LongMiss;
    public bool TopPushed;
    public int PushPos;

    public bool Hittable
        => Type is > ENote.None and not ENote.End;
    public bool Displable => Hittable && !Hit && BPM * Scroll > -1000 && BPM * Scroll < 1000000;
    public bool NonThrought => Hittable && !Hit && !Miss;

    public double Dencity => Length > 0.0 ? 1000.0 / Length : 0;

    private static bool IsLog2(double n)
    {
        double log = Math.Log2(n);
        return log - (int)log == 0;
    }

    private static bool Range(double dec, int count)
    {
        double r = Math.Round(dec * count % 2, 2, MidpointRounding.AwayFromZero);
        return r - (int)r == 0;
    }

    public void Reset()
    {
        Hit = false;
        Miss = false;
        LongMiss = false;
        Pushing = false;
        TopPushed = false;
        PushPos = 0;
        RollValue = 0;
        Appeared = false;
        BranchHit = null;
        //HitTime = null;
        ReservedTime = null;
        ReservedRollTime = null;
    }

    public override string ToString() => $"{(Branch > 0 ? $"{(EBranchLane)Branch - 1}" : "")}" +
            $"{Type,4} pos:{BeatPos,5:0.0##} {Separate:0.###}x {(int)Length}ms {(int)RelativeTime} " +
        $"bpm:{BPM:0.#} hs:{Scroll:0.##}{(ImidiateScroll != 0 ? $"+{ImidiateScroll:0.##}i" : "")} ";

    public double Separate => GetSeparate(Length);

    public double GetSeparate(double length)
    {
        if (length == 0) return 1;

        double bpm = EquallyBPM ?? BPM;
        double measure = EquallyMeasure ?? Measure;
        if (bpm == 0) bpm = BPM;
        if (measure == 0) measure = Measure;

        double ran = bpm < 120.0 ? 30000 : 60000;
        double b = ran / bpm;
        double brange = b * 4.0 / length;//1小節を4分割したものをlengthで割る
        if (brange < 1.0) return 1;

        double r = b / measure;
        double mes = 4.0 / measure;
        double range = r * 4.0 / length / mes * 4.0;

        var rt = Rational.FromDouble(range, 100, 0.002);
        return rt.Value;
    }

    public int NeedHand = 0;
    public double Weight
    {
        get
        {
            double weight = 0.0;
            int hand = NeedHand;
            if (Length == 0 || hand < 1)
            {
                weight = 1.0 * hand;
                return weight;
            }
            weight = Math.Log2(Dencity + 1);
            if (hand > 1)
            {
                weight *= 1.0 + 0.5 * (hand - 1);
            }
            // compositeに応じてweightを調整
            // (元のweightが高いほど影響を受けにくい)

            double cmp = Math.Log10(Composite);
            weight *= 1.0 + cmp / weight;

            //weight = Math.Log2(1 + Composite) / Math.Log2(2 + Composite) * weight * 2.0;

            return weight;
        }
    }

    public void SetWeight(int hand = 1)
    {
    }

    public int PosRate
    {
        get
        {
            double value = EquallyPos ?? BeatPos;
            double dec = Math.Round(value - (int)value, 4, MidpointRounding.AwayFromZero);

            var rational = Rational.FromDouble(dec, 10000, 0.002);

            return rational.Den * 4;
        }
    }

    public bool Syncopate =>
            // このノーツと次のノーツの間に長さ分の空白があるか

            false;

    public Chip[] ConnectedChips
    {
        get
        {
            var list = new List<Chip>();
            var chip = this;
            while (chip != null)
            {
                list.Add(chip);
                chip = chip.Next;
                if (chip?.Composite == 1) break;
            }
            return [.. list];
        }
    }
    public bool Equal(Chip? chip) => chip != null && (this == chip || Math.Abs(Time - chip.Time) < 0.001 && Type == chip.Type && Bar == chip.Bar && Position == chip.Position);
}

public enum ENote
{
    None = 0,
    Don = 1,
    Ka = 2,
    DON = 3,
    KA = 4,
    Roll = 5,
    ROLL = 6,
    Balloon = 7,
    End = 8,
    Potato = 9,

    //SpiCatsロングノーツ
    LDon = 10,
    LKa = 11,
    LDON = 12,
    LKA = 13,
}