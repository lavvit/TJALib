using AstrumLoom;

using TJALib.TJA;

namespace TJALib.Draw;

public class Lane
{
    public static Counter Timer = new(-2000, long.MaxValue);
    public Course Course = new();
    public int Width = 0;
    public int Height = 0;
    public int LaneVector = 1;

    public int PlaySide = 0; // 0: auto, 1: left, 2: right
    public double Scroll = 1.0;
    public int HBS = 0; // 0: off, 1: bms, 2: hbs
    public Sound Sound = new();

    public static double NowTime => Timer.Value;

    public Lane() { }

    public Lane(Course course) => Init(course);

    public virtual void Init(Course course, bool reset = true)
    {
        Sound.Stop();
        Width = (int)(AstrumCore.Width * 0.9);
        Height = (int)(AstrumCore.Height * 0.1666);
        Note.Size = Height;
        Course = course.Clone();
        Course.Reset();
        Timer.Reset();
        Sound = new(Path.Combine(Path.GetDirectoryName(course.Header.Path) ?? "", course.Header.Wave));
    }

    public virtual void Update()
    {
        if (Course == null || !Course.Enable) return;
        if (Key.Space.Push())
        {
            if (Timer.State > 0) Stop();
            else Start();
        }
        if (Key.Q.Push()) Reset();

        Timer.Tick();
        if (Timer.Value >= 0 && Timer.State > 0)
        {
            if (!Sound.Playing && Timer.Value < Sound.Length - 100)
            {
                Sound.Play();
            }
        }
    }

    public void Start()
    {
        if (Timer.Value > 0)
        {
            Sound.Play();
            Sound.Time = NowTime;
        }
        Timer.Start();
    }
    public void Stop()
    {
        Sound.Stop();
        Timer.Stop();
    }
    public void Reset()
    {
        Stop();
        Timer.Reset();
    }

    public virtual void Draw(double x, double y)
    {
        Drawing.Text(x, y - 20, $"Time: {NowTime:0}", Color.White);
        Drawing.Box(x, y, Width, Height, Color.FromRGB(48, 48, 48));
        Drawing.Circle(x + Height / 2, y + Height / 2, Height * 0.25, Color.Gray);
        Drawing.Circle(x + Height / 2, y + Height / 2, Height * 0.25 * 1.5, Color.Gray, thickness: 3);
        foreach (var chip in Course.Chips)
        {
            Note.Draw(x + Height / 2 + NoteX(chip), y + Height / 2, chip.Type);
        }
    }

    public static double NoteX(double ctime, double bpm, double scroll, int width = 0)
    {
        if (width <= 0) width = AstrumCore.Width;
        double time = ctime - NowTime;
        return time / 1000.0 * (bpm / 240.0) * width * scroll;
    }
    public static double HBSNoteX(double now, double beat, double scroll, int width = 0)
    {
        if (width <= 0) width = AstrumCore.Width;
        double time = beat - now;
        return time / 4.0 * width * scroll;
    }
    public double NoteX(Chip chip)
    {
        int vector = Width;//LaneVector % 2 == 0 ? Height : Width;
        return HBS > 0
            ? HBSNoteX(BPM.GetNoteX(Course, NowTime, CurrentBar), chip.Beat, HBS > 1 ? chip.Scroll * Scroll : Scroll, vector)
            : NoteX(chip.Time, chip.BPM, chip.Scroll * Scroll, vector);
    }
    public double NoteX(Bar bar)
    {
        int vector = Width;//LaneVector % 2 == 0 ? Height : Width;
        return HBS > 0
            ? HBSNoteX(BPM.GetNoteX(Course, NowTime, CurrentBar), bar.Beat, HBS > 1 ? bar.Scroll * Scroll : Scroll, vector)
            : NoteX(bar.Time, bar.BPM, bar.Scroll * Scroll, vector);
    }

    public double NowNoteX
    {
        get
        {
            var nowBar = CurrentBar;
            var nowchip = Course.Chips.Where(c => c.Time <= NowTime).OrderByDescending(c => c.Time).FirstOrDefault();
            var nextchip = Course.Chips.Where(c => c.Time > NowTime).OrderBy(c => c.Time).FirstOrDefault();
            if (nowchip != null && nextchip != null)
            {
                double progress = (NowTime - nowchip.Time) / (nextchip.Time - nowchip.Time);
                double beat = nowchip.Beat + (nextchip.Beat - nowchip.Beat) * progress;
                int vector = Width;//LaneVector % 2 == 0 ? Height : Width;
                return beat;
            }
            else if (nowchip != null)
            {
                double progress = (NowTime - nowchip.Time) / (nowBar.Time + nowBar.Length - nowchip.Time);
                double beat = nowchip.Beat + (nowBar.Beat + nowBar.BeatLen - nowchip.Beat) * progress;
                int vector = Width;//LaneVector % 2 == 0 ? Height : Width;
                return beat;
            }
            else
            {
                return 0;
            }
        }
    }

    public int NowBar = 0;
    public int NowBeat = 0;
    public int NowBranch = 0;

    public Bar[] NowBars
    {
        get
        {
            if (Course == null || Course.Lanes.Length == 0) return [];
            int side = PlaySide;
            if (side > 0)
            {
                if (Course.IsEnable(side))
                {
                    return Course.Lanes[side];
                }
            }
            return Course.Lanes[0];
        }
    }
    public int BarCount => Course.Bars.Max(b => b.Number);

    public Bar CurrentBar => NowBars.Length == 0 ? new Bar() : NowBar <= 0 ? NowBars[0] : NowBar >= NowBars.Length ? NowBars[^1] : NowBars[NowBar - 1];
}
