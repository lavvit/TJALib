namespace TJALib.TJA;

public class JPOSSCROLL
{
    public double Time;
    public double Length;
    public double Pixel;
    public bool IsHit;
}

public class NoteSpawn
{
    public double Time;
    public (bool Spawn, bool Trans) Enable;
    public (double Spawn, double Trans) Timing;
}
public class JudgeDelay
{
    public double Time;
    public int Type;
    public double Second, X, Y;
    public double Bpm;
    public double Scroll;
    public double ScrollImage;
}
public class GradationValue
{
    public int ID;
    public (bool Set, bool Enable, double Value, double Start, double End) Scroll;
    public (double Value, double Start, double End) ScrollImage;
    public (bool Set, bool Enable, JudgeDelay Value, JudgeDelay Start, JudgeDelay End) JudgeDelay;
    public (bool Set, bool Enable, double Value, double Start, double End) Size;
    public (bool Set, bool Enable, (double Width, double Height) Value, (double Width, double Height) Start, (double Width, double Height) End) BarSize;
    public (bool Set, bool Enable, (int R, int G, int B, int A) Value, (int R, int G, int B, int A) Start, (int R, int G, int B, int A) End) ColorRGB;
    public (bool Set, bool Enable, double Value, double Start, double End) Rotate;
}
public class CommandGradation
{
    public int ID;
    public double Time;
    public double Length;
    public int Type;
    public int Easing;
    public bool IsHit;
}
