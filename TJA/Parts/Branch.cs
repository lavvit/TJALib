using AstrumLoom;
namespace TJALib.TJA;

public class Branch
{
    public int Number;

    public double Start;
    public double End;
    public EBranch Type;
    public double Expert;
    public double Master;

    public double Value;
    public List<double> ResetTime = [];
    public bool Hit;
    public int Force = -1;

    public int[] ChipCount = [0, 0, 0];
    public List<Bar>[] Lanes = [[], [], []];
    public Bar[] StartBarStatus = [new(), new(), new()];

    public Branch() { }
    public Branch(string str)
    {
        var span = str.AsSpan().Trim();
        int firstComma = span.IndexOf(',');
        if (firstComma < 0)
        {
            // 1項目のみ
            switch (span.ToString().ToLowerInvariant())
            {
                case "p": Type = EBranch.Precision; break;
                case "r": Type = EBranch.Roll; break;
                case "s": Type = EBranch.Score; break;
            }
            return;
        }
        // 2項目以上
        var typeSpan = span[..firstComma];
        switch (typeSpan.ToString().ToLowerInvariant())
        {
            case "p": Type = EBranch.Precision; break;
            case "r": Type = EBranch.Roll; break;
            case "s": Type = EBranch.Score; break;
        }
        int secondComma = span[(firstComma + 1)..].IndexOf(',');
        if (secondComma >= 0)
        {
            var expertSpan = span.Slice(firstComma + 1, secondComma);
            var masterSpan = span[(firstComma + 1 + secondComma + 1)..];
            double.TryParse(expertSpan.ToString(), out Expert);
            double.TryParse(masterSpan.ToString(), out Master);
        }
        else
        {
            var expertSpan = span[(firstComma + 1)..];
            double.TryParse(expertSpan.ToString(), out Expert);
        }
    }
    public double JudgeTime { get; set; }

    public int Max
        => Number + Lanes.Max(l => l.Count);

    public int Now
    {
        get
        {
            if (ForceBranch.HasValue)
                return ForceBranch.Value;
            if (Force >= 0)
                return Force;
            if (Hit)
            {
                if (Value >= Master) return 2;
                return Value >= Expert ? 1 : 0;
            }
            return -1;
        }
    }
    public int BaseLane()
    {
        if (ForceBranch.HasValue)
            return ForceBranch.Value;
        if (ChipCount[2] >= ChipCount[0] && ChipCount[2] >= ChipCount[1]) return 2;
        return ChipCount[1] >= ChipCount[0] && ChipCount[1] >= ChipCount[2] ? 1 : 0;
    }
    public int? ForceBranch
    {
        get
        {
            switch (Type)
            {
                case EBranch.Precision:
                    if (Master <= 0) return 2;
                    if (Master > 100)
                    {
                        if (Expert <= 0) return 1;
                        if (Expert > 100) return 0;
                    }
                    break;
            }
            return null;
        }
    }

    public Color Color => GetColor(Now);
    public static Color GetColor(int value)
    {
        return value switch
        {
            1 => Color.FromHex("#206070"),
            2 => Color.FromHex("#803060"),
            _ => Color.FromHex("#303030"),
        };
    }

    public override string ToString()
    {
        string state = "";
        if (ForceBranch.HasValue)
            state = $"{(EBranchLane)ForceBranch.Value} Fixed";
        else state = Force >= 0
            ? $"{(EBranchLane)Force} Setting"
            : $"{(EBranchLane)Math.Max(0, Now)} ({Type.ToString()[..1]} {Value:0.#} -> E{Expert}/M{Master})";
        return $"{(int)Start}: {state}";
    }
}

public enum EBranchLane
{
    Normal,
    Expert,
    Master,
}

public enum EBranch
{
    None,
    Precision,
    Roll,
    Score,
}
