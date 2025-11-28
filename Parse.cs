namespace TJALib;

public class ChipBase
{
    /// <summary>
    /// ノーツの判定時間(ms基準)
    /// </summary>
    public double Time;
    /// <summary>
    /// ノーツの位置(1/4拍基準)
    /// </summary>
    public double Beat;

    /// <summary>
    /// ノーツのBPM
    /// </summary>
    public double BPM;

    public override string ToString() => $"{Time} {BPM}";
}
public class BarBase
{
    public int Number = 0;
    public double Time = 0;
    public double Beat = 0;
    public double BPM = 0;
    public double Measure = 1;

}

public class CourseInfo
{
    public string Title = "";
    public string Artist = "";
    public string Genre = "";
    public string Designer = "";
    public double Level = 0;
    public int Notes = 0;
    public int Length = 0;
    public override string ToString() => $"{Title} {Artist} {Genre} {Level} {Length}";
}

public class BPMBase
{
    public double Time;
    public double Value;
    public double Beat;
    //public (int measure, int amount, int i) Place;

    public override string ToString() => $"{Time} {Beat}: {Value}";
}
