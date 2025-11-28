using AstrumLoom;

namespace TJALib.TJA;

public class BPM : BPMBase
{
    public (int measure, int amount, int i) Place;

    public override string ToString() => $"{Time} {Beat}: {Value}";

    public static void Add(Course course, double time, double beat, double value) => course.BpmList.Add(new()
    {
        Time = time,
        Value = value,
        Beat = beat,
    });

    public static double GetNoteX(Course course, double time, Bar bar)
    {
        var bpm = course.BpmList.LastOrDefault(e => time >= e.Time);
        double result = bar.Beat;

        if (!course.BpmList.Any(e => e.Time >= bar.Time && e.Time < bar.Time + bar.Length))
        {
            result = Easing.Ease(time - bar.Time, bar.Length,
              bar.Beat, bar.Beat + bar.BeatLen);
        }
        else
        {
            double start = bpm != null && bpm.Time > bar.Time ? bpm.Time : bar.Time;
            double next = bar.Time + bar.Length - start;
            double sbeat = bpm != null && bpm.Time > bar.Time ? bpm.Beat : bar.Beat;
            double nbeat = bar.Beat + bar.BeatLen;
            result = Easing.Ease(time - start, next, sbeat, nbeat);
        }

        /*BPM? prev = new()
        {
            Time = course.Lanes[0].Length > 0 ? course.Lanes[0][0].Time : 0,
            Value = course.Lanes[0].Length > 0 ? course.Lanes[0][0].BPM : 120,
            Beat = 0
        };
        BPM? mid = null;
        foreach (var bpm in course.BpmList)
        {
            double start = time - prev.Time;
            double end = bpm.Time - prev.Time;
            if (time < bpm.Time)
            {
                mid = bpm;
                break;
            }
            prev = bpm;
        }
        var bar = PlayUI.Lane.NowBarLane;
        int barnum = PlayUI.Lane.NowBar;

        var last = course.BpmList.Count > 0 ? prev : null;
        double lasttime = time - (last != null ? last.Time : (course.Lanes[0].Length > 0 ? course.Lanes[0][0].Time : 0));
        double bt = 60000.0 / (last != null ? last.Value : (course.Lanes[0].Length > 0 ? course.Lanes[0][0].BPM : 120)) / bar.Measure;
        result = (last != null ? last.Beat : 0) + lasttime / bt;

        if (barnum > 0 && barnum < PlayUI.Lane.NowBars.Length && last != null)
        {
            double next = PlayUI.Lane.NowBars[barnum].Time;
            double beat = bar.Beat + (4 / bar.Measure);
            if (mid != null)
            {
                result = mid.Time - prev.Time < next ? Easing.Ease(time - prev.Time, mid.Time - prev.Time, prev.Beat, mid.Beat)
                        : Easing.Ease(time - prev.Time, next - prev.Time, prev.Beat, beat);
            }
            else if (bar.Time >= last.Time)
                result = Easing.Ease(time - bar.Time, next - bar.Time,
                  bar.Beat, beat);
        }*/

        foreach (var delay in course.DelayList)
        {
            if (time >= delay.Time)
            {
                double t = time > delay.Time + delay.Length ? delay.Time + delay.Length : time;
                double b = Easing.Ease(t - delay.Time, delay.Length, 0, delay.Target - delay.Beat);
                //result -= b;
            }
        }
        return Math.Round(result, 3);
    }
}

public class Delay
{
    public double Time;
    public double Length;
    public double Beat;
    public double Target;
}
