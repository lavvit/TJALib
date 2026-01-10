using AstrumLoom;

using static System.Math;

namespace TJALib.TJA;

public class Rader
{
    public Rader() { }
    public Rader(Course course)
    {
        Notes = RaderNotes(course);
        Peak = RaderPeak(course);
        Stream = RaderStream(course);
        Rhythm = RaderRhythm(course);
        Soflan = RaderSoflan(course);
        Gimmick = RaderGimmick(course);
        //Task.Run(() => SetHexa(course));
        SetHexa(course);
    }

    public double Notes { get; set; }
    public double Peak { get; set; }
    public double Stream { get; set; }
    public double Rhythm { get; set; }
    public double Soflan { get; set; }
    public double Gimmick { get; set; }
    public double Total
    {
        get
        {
            double note = Scale(Pow(Notes, 2), 40000);
            double peak = Scale(Pow(Peak, 2), 40000);
            double stream = Scale(Pow(Stream, 2), 40000);
            double rhythm = Scale(Pow(Rhythm, 3), Pow(200, 3));
            double soflan = Scale(Pow(Soflan, 3), Pow(200, 3));
            double gimmick = Scale(Pow(Gimmick, 2), 40000);
            double depth = Log2(Scale(NOTES_Amount, 20000));

            // ---- 合成 ----
            double score = Log10(0.20 * note + 0.15 * peak + 0.25 * stream
                            + 0.20 * rhythm + 0.15 * soflan + 0.05 * gimmick);
            double scorescale = Scale(score, 2.1) * 0.6;
            double depthscale = Scale(depth, 8) * 0.4;

            return scorescale + depthscale;
            #region Old
            /*
            double note = Notes;
            double pek = Peak;//Notes / 100.0 * 
            double stm = Stream;//Notes / 100.0 * 
            double bas = (note + pek + stm) / 3.0;
            if (bas == 0) return 0;

            double rhm = Rhythm;
            double sfl = Soflan;
            double gmk = Gimmick;
            double add = (rhm + sfl + gmk) / 2.0;


            double sum = bas + add * (bas / 100);
            return sum * 100.0;
            */
            #endregion
        }
    }
    public double GetTotal(ECourse course) => course switch
    {
        ECourse.Oni or ECourse.Edit => Pow(Total, 0.4),
        _ => 0,
    };
    private static double Diff(double a, double b)
        => a == 0 || b == 0 ? 1 : Max(a >= b ? (double)((decimal)a / (decimal)b) : (double)((decimal)b / (decimal)a), 0.001);
    private static bool Accept(double a, double width = 0.001, bool onlyzelo = false)
        => (onlyzelo ? Min(a, Abs(Ceiling(a) - a)) : Min(a - (int)a, Abs(a - (int)a - 1))) <= width;
    private static double Scale(double val, double reference)
        => Max(200.0 * (val / Max(1e-9, reference)), 0);
    private static double TopRate(
List<double> values,
double p = 0.95,        // 上位pの開始（0~1）
double maxWeight = 0.5, // 0=平均のみ / 1=最大のみ（最終ブレンド）
double maxTrim = 0.0   // 上位n%を削る
)
    {
        if (values == null || values.Count == 0) return 0;

        values.Sort(); // 昇順
        int n = (int)(values.Count * (1.0 - maxTrim));
        if (n < 1) return 0;

        // 上位p%の平均
        int start = (int)(values.Count * p);
        start = Min(Max(0, start), n - 1);
        double sum = 0; for (int i = start; i < n; i++) sum += values[i];
        double avgTop = sum / (n - start);

        // 最大値
        double max = values[n - 1];

        // ブレンド
        return max * maxWeight + avgTop * (1.0 - maxWeight);
    }

    #region Rader

    #region Notes
    /// <summary>
    /// 譜面の平均密度(対象ノーツの最初から最後まで) notes/s
    /// </summary>
    public double NOTES_AvgDensity = 0;
    /// <summary>
    /// ノーツ同士の平均間隔(高いほど間隔が狭い) 1/s avg
    /// </summary>
    public double NOTES_Amount = 0;
    public double RaderNotes(Course course)
    {
        if (course.Notes == 0) return 0;
        int note = course.Notes - 1;
        double length = course.ChipLenNoRoll / 1000.0;//ノーツの終点の長さ
        double maxsize = 10.5;//本家ベースの最大密度
        NOTES_AvgDensity = note / length;// Notes : 譜面のノーツ数(合計)

        NOTES_Amount = TotalNoteAmount(course);

        return Scale(NOTES_AvgDensity, maxsize);
    }
    private static double TotalNoteAmount(Course course)
    {
        var hits = course.HitChips;
        return hits.Sum(hit => double.IsNaN(hit.Weight) ? 0 : hit.Weight);
    }
    public static double MiniNotes(Chip[] chips)
    {
        if (chips.Length == 0) return 0;
        double length = (chips[^1].Time + chips[^1].Length - chips[0].Time) / 1000.0;
        if (length == 0) return 1;
        int note = chips.Length - 1;
        double avg = note / length;// Notes : 譜面のノーツ数(合計)
        double maxsize = 10.5;//本家ベースの最大密度
        return Scale(avg, maxsize);
    }
    #endregion

    #region Peak
    public double PEAK_Nps2s = 0;
    public double RaderPeak(Course course)
    {
        var hits = course.HitChips; // 1,2,3,4のみ

        // 4秒窓 p95 → NPS化
        PEAK_Nps2s = PeakCount(hits, 2000.0) / 2.0;
        double refNps = 20.0; // 基準NPS
        double nps = Scale(PEAK_Nps2s, refNps);
        return nps;
    }
    // p95 版（外れ値耐性）
    private static double PeakCount(List<Chip> chips, double windowMs)
    {
        var counts = new List<int>();
        if (chips.Count == 0) return 0;
        int j = 0;
        for (int i = 0; i < chips.Count; i++)
        {
            double left = chips[i].Time;
            double right = left + windowMs;
            while (j < chips.Count && chips[j].Time < right)
            {
                j++;
            }
            counts.Add(j - i);
        }
        return TopRate([.. counts.Select(v => (double)v)]);
    }
    public static double MiniPeak(Chip[] chips)
    {
        if (chips.Length == 0) return 0;
        double length = Min(chips[^1].Time + chips[^1].Length - chips[0].Time, 2000.0);
        if (length == 0) return 0;
        double np4 = PeakCount([.. chips], length) / (length / 1000.0);
        double refNps = 20.0; // 基準NPS
        double nps = Scale(np4, refNps);
        return nps;
    }
    #region OldVersion
    /*public double RaderPeak(Course course)
    {
        int maxnote = 0;
        double maxsize = 80.0;//本家ベースの最大ノーツ調整値
        for (int i = 0; i < 3660000 && i < course.Length - 4000; i += 100)// 3660000は6分、4秒ずつ毎ms計測
        {
            int n = 0;
            foreach (var bar in course.Lanes[0])
            {
                foreach (var chip in bar.Chips)
                {
                    //4000msの間にノーツがあるか、該当分岐か
                    if (chip.Time >= i && chip.Time < i + 4000 && (chip.Branch == 0 || chip.Branch == course.BranchList[0][chip.BranchNum].BaseLane() + 1))
                    {
                        //ノーツ(1,2,3,4)
                        if (chip.Type > ENote.None && chip.Type < ENote.Roll)
                        {
                            n++;
                        }
                    }
                }
                if (maxnote < n) maxnote = n;
            }
        }
        return maxnote * 200.0 / maxsize;
    }*/
    #endregion
    #endregion

    #region Stream
    public double STREAM_Scaling = 0;
    public double RaderStream(Course course)
    {
        // 1) ヒット列（分岐フィルタ＆ロール除外）
        var hits = course.HitChips; // Chip: TimeMs, Color (Don/Ka), BeatPos (小節換算)
        if (hits.Count < 2) return 0;

        double stream = 0;
        double RefPhrase = 220.0; // 帯ごとの参照値（p95相当を目安に後で合わせ込む）

        STREAM_Scaling = StreamScaling([.. hits]);
        stream = Scale(STREAM_Scaling, RefPhrase);

        // 5) 薄味の最終係数（交互率・多様度で±8%以内）
        var meta = ComputeMeta([.. hits]);
        double factor = 1.0 + 0.06 * (meta.SwitchRate - 0.5) + 0.02 * (meta.PatternDiv3 - 0.4);
        double Mul = 0.08;
        stream *= Clamp(factor, 1.0 - Mul, 1.0 + Mul);

        return stream;
    }
    private static double StreamScaling(Chip[] hits)
    {
        double[] gap = [1.0 / 4.0, 1.0 / 2.0, 1.0 / 1.0, 4.0, 32.0]; // 16分, 8分, 4分, 1小節, 8小節
        double gamma = 0.7;
        double[] scales = [0, 0, 0, 0, 0];
        for (int i = 0; i < scales.Length; i++)
        {
            // 1) 細刻みストリークへ分割し、各ストリークの“効果長”を計算
            var effLens = ExtractTierStreaks(hits, gap[i], i == 0 ? 0 : gap[i - 1], gamma);

            if (effLens.Count == 0) continue;

            // 2) 代表値 = p90 or Top-K平均（大きい方）
            double rep = Representative(effLens);
            scales[i] = rep;
        }

        double[] weight = [0.5, 0.3, 0.1, 0.05, 0.05];
        double stream = 0;
        for (int i = 0; i < scales.Length; i++)
        {
            // 3) 各帯を重み付けして合成
            stream += weight[i] * scales[i];
        }
        // 4) スケーリング
        return Max(stream, Log10(stream) * 100.0);
    }
    public static double MiniStream(Chip[] chips)
    {
        if (chips.Length == 0) return 0;
        double length = Min(chips[^1].Time + chips[^1].Length - chips[0].Time, 2000.0);
        if (length == 0) return 0;

        double stream = 0;
        double RefPhrase = 220.0; // 帯ごとの参照値（p95相当を目安に後で合わせ込む）

        double st = StreamScaling(chips);
        stream = Scale(st, RefPhrase);

        // 5) 薄味の最終係数（交互率・多様度で±8%以内）
        var meta = ComputeMeta(chips);
        double factor = 1.0 + 0.06 * (meta.SwitchRate - 0.5) + 0.02 * (meta.PatternDiv3 - 0.4);
        double Mul = 0.08;
        stream *= Clamp(factor, 1.0 - Mul, 1.0 + Mul);
        return stream;
    }
    #region Func
    // === 帯ストリーク抽出（gap範囲で“切断”、ALT/JACK＋速さを帯内で付与） ===
    private static List<double> ExtractTierStreaks(Chip[] hits, double maxGap, double minGapExclusive, double gamma)
    {
        var list = new List<double>();
        int i = 0;
        while (i < hits.Length - 1)
        {
            double min = minGapExclusive, max = maxGap;
            // このgapがこの帯に属するなら開始
            double g0 = hits[i + 1].Beat - hits[i].Beat;
            if (hits[i].BPM >= 400.0)
            {
                double lg = Pow(2.0, (int)Log(hits[i].BPM / 200, 2));
                max *= lg;
                if (min == 0) min = max / 2;
                else min *= lg;
            }
            if (!(g0 > min + 1e-9 && g0 <= max + 1e-9)) { i++; continue; }

            int j = i + 1;
            int switches = 0, curJack = 1, maxJack = 1;
            double weighted = 1.0; // 打数基準。帯内は「素直に1加算」でOK

            double sumMs = hits[j].Time - hits[j - 1].Time;
            int cntMs = 1;

            // 帯内でつながる限り伸ばす（別帯のgapが来たら切断）
            while (j < hits.Length)
            {
                double gap = hits[j].Beat - hits[j - 1].Beat;
                if (!(gap > min + 1e-9 && gap <= max + 1e-9) && sumMs > 100) break;

                weighted += (hits[j].Type >= ENote.DON ? 2 : 1) * (hits[j - 1].Type >= ENote.DON ? 2 : 1); // この帯は“打数メイン”なので1ずつ
                sumMs += hits[j].Time - hits[j - 1].Time;
                cntMs++;

                bool diff = (int)hits[j].Type % 2 != (int)hits[j - 1].Type % 2;
                if (diff) { switches++; maxJack = Max(maxJack, curJack); curJack = 1; }
                else { curJack++; maxJack = Max(maxJack, curJack); }
                j++;
            }
            maxJack = Max(maxJack, curJack);

            // 速度係数（帯ごとにγを変える）
            double speed = 1.0;
            if (cntMs > 2)
            {
                double HSI_Ref = 10.0;   // 基準NPS（= 150BPMの16分）
                double meanMs = sumMs / cntMs;
                double hsi = (meanMs > 0) ? (1000.0 / meanMs) : 0.0;
                double ratio = Max(1e-6, hsi / HSI_Ref);

                double Q_EQ = Log2(10);
                double sf = Pow(ratio, Q_EQ);
                speed = Pow(sf, gamma);
                //speed = Clamp(speed, 0.01, 2.00);
            }

            // 薄味加算（帯共通）
            int JackCap = 12;   // ジャックの逓減キャップ
            double JackAlpha = 0.75; // ジャック逓減のべき乗

            double steps = Max(1.0, j - i);   // gap数
            double switchRate = switches / steps;
            double jackEff = JackEffectiveLen(maxJack, JackCap, JackAlpha);

            double altAdd = 8.0 * switchRate;
            double jackAdd = 6.0 * (jackEff / JackCap);

            list.Add(weighted * speed + altAdd + jackAdd);

            i = Max(i + 1, j); // 進める
        }
        return list;
    }

    private static double Representative(List<double> values)
    {
        if (values == null || values.Count == 0) return 0;

        int K_TopMean = 3;      // Top-K平均（代表値）
        bool UseTopKOverP90 = true; // 代表値 = max(p90, TopK平均)

        double p90 = Percentile(values, 90);
        double topK = TopKMean(values, K_TopMean);
        return UseTopKOverP90 ? Max(p90, topK) : p90;
    }

    private static double JackEffectiveLen(int rawLen, int cap, double alpha)
    {
        double capped = Min(rawLen, cap);
        double powed = Pow(Max(1.0, rawLen), alpha);
        return Min(capped, powed);
    }
    private static double Percentile(IList<double> xs, double p)
    {
        if (xs == null || xs.Count == 0) return 0;
        double[] a = new double[xs.Count];
        for (int i = 0; i < xs.Count; i++) a[i] = xs[i];
        Array.Sort(a);
        if (a.Length == 1) return a[0];
        double rank = p / 100.0 * (a.Length - 1);
        int lo = (int)Floor(rank);
        int hi = (int)Ceiling(rank);
        double w = rank - lo;
        return a[lo] * (1 - w) + a[hi] * w;
    }
    private static double TopKMean(IList<double> xs, int k)
    {
        if (xs == null || xs.Count == 0) return 0;
        int n = xs.Count;
        if (n <= k)
        {
            double s = 0; for (int i = 0; i < n; i++) s += xs[i];
            return s / n;
        }
        double[] top = new double[k];
        int filled = 0;
        for (int i = 0; i < n; i++)
        {
            double v = xs[i];
            if (filled < k)
            {
                top[filled++] = v;
                if (filled == k) Array.Sort(top);
            }
            else if (v > top[0])
            {
                top[0] = v;
                int pos = 0;
                while (pos + 1 < k && top[pos] > top[pos + 1])
                {
                    (top[pos + 1], top[pos]) = (top[pos], top[pos + 1]);
                    pos++;
                }
            }
        }
        double sum = 0; for (int i = 0; i < k; i++) sum += top[i];
        return sum / k;
    }

    // 交互率と3-gram多様度（薄味係数用）
    private sealed class Meta { public double SwitchRate; public double PatternDiv3; }
    private static Meta ComputeMeta(Chip[] hits)
    {
        if (hits.Length < 2) return new Meta { SwitchRate = 0, PatternDiv3 = 0 };
        int switches = 0;
        for (int i = 1; i < hits.Length; i++)
        {
            bool diff = (int)hits[i].Type % 2 != (int)hits[i - 1].Type % 2;
            if (diff) switches++;
        }
        double switchRate = (double)switches / (hits.Length - 1);

        // 3-gramユニーク率
        int n = 3;
        if (hits.Length < n) return new Meta { SwitchRate = switchRate, PatternDiv3 = 0 };
        var set = new HashSet<int>();
        for (int i = 0; i <= hits.Length - n; i++)
        {
            int code = 0;
            for (int k = 0; k < n; k++)
                code = (code << 1) | ((int)hits[i + k].Type % 2 == 0 ? 1 : 0);
            set.Add(code);
        }
        int total = hits.Length - n + 1;
        double div = total > 0 ? (double)set.Count / total : 0;
        return new Meta { SwitchRate = switchRate, PatternDiv3 = div };
    }
    #endregion
    #region OldVersion
    /*public double RaderStream(Course course)
    {
        double stream = 0;
        double t = 0;
        double maxsize = 33.0;//本家(略
        foreach (var bar in course.Lanes[0])
        {
            foreach (var chip in bar.Chips)
            {
                //該当分岐、全ノーツ対象(空以外)
                if (chip.Type > ENote.None && (chip.Branch == 0 || chip.Branch == course.BranchList[0][chip.BranchNum].BaseLane() + 1))
                {
                    //単音(1~4)のみ加算
                    if (t > 0 && chip.Type < ENote.Roll)
                    {
                        double brank = Abs(chip.Time - t);
                        double dencity = 1000.0 / (Notes > 0 ? Notes : RaderNotes(course));
                        if (brank < 25.0) stream += 0.4;
                        else
                        {
                            double val = 1.0 / brank;
                            double ran = chip.BPM < 120.0 ? 30000 : 60000;//遅い場合基準を2倍
                            double r = ran / chip.BPM / bar.Measure;
                            stream += val;//if (brank < dencity) 
                            if (brank <= r / 4.0) stream += val;
                            //else if (brank <= r / 3.0) stream += val * 0.3;
                            else if (brank <= r / 2.0) stream += val * 0.2;
                            else if (brank <= r / 1.0) stream += val * 0.1;
                        }
                    }
                    t = chip.Time;
                }
            }
        }
        return stream * 200.0 / 33.0;
    }*/
    #endregion
    #endregion

    #region Rhythm
    public double RHYTHM_Standard = 0;
    public double RHYTHM_Swing = 0;
    public double RHYTHM_Poly = 0;

    public double RHYTHM_Change = 0;
    public double RHYTHM_Measure = 0;
    public double RaderRhythm(Course course)
    {
        var hits = course.HitChips;
        if (hits.Count < 2) return 0;
        double[] calc = CalcRhythm([.. hits]);
        RHYTHM_Standard = calc[0];
        //RHYTHM_Swing = calc[1];
        RHYTHM_Poly = calc[1];//GetRhythmValue([.. hits]);
        RHYTHM_Change = calc[2];
        RHYTHM_Measure = calc[3];

        double refrtm = 40;
        return Scale(calc[4], refrtm);
    }
    public static double MiniRhythm(Chip[] chips)
    {
        if (chips.Length == 0) return 0;
        double length = Min(chips[^1].Time + chips[^1].Length - chips[0].Time, 2000.0);
        if (length == 0) return 0;

        double[] calc = CalcRhythm(chips);
        double refrtm = 40;
        return Scale(calc[4], refrtm);
    }

    private static double[] CalcRhythm(Chip[] chips)
    {
        List<double> standardlist = [];
        List<double> polylist = [];
        List<double> changelist = [];
        List<double> measurelist = [];
        List<double> totallist = [];
        for (int i = 1; i < chips.Length - 1; i++)
        {
            Chip L = chips[i - 1], R = chips[i];
            double[] rt = GetRhythms(L, R);
            if (rt[0] > 0)
            {
                standardlist.Add(rt[0]);
                polylist.Add(rt[1]);
                changelist.Add(rt[2]);
                measurelist.Add(rt[3]);
                double tl = CalcRhythmTotal(rt);
                totallist.Add(tl);
            }
        }
        double standard = TopRate(standardlist);
        double poly = TopRate(polylist);
        double change = TopRate(changelist);
        double measure = TopRate(measurelist);
        double total = TopRate(totallist, 0.8, 0.5, 0.03);

        return [standard, poly, change, measure, total];
    }
    private static double CalcRhythmTotal(double[] rt)
    {
        double total =
            5.0 * Pow(rt[0], 2) + // 標準リズム
            4.0 * Pow(rt[1], 2) + // ポリリズム
            3.0 * Pow(rt[2], 2) + // BPM揺れ
            1.0 * Pow(rt[3], 2);  // 変拍子
        return total;
    }

    private static double[] GetRhythms(Chip L, Chip R)
    {
        // 標準, ポリリズム, BPM揺れ, 変拍子
        double[] rhythm = [0, 0, 0, 0];
        if (L.Length <= 0 || R.Length <= 0) return rhythm;

        // ひとまず左基準
        double measure = Round(4.0 / L.Measure, 5);

        // 変拍子
        if (!Accept(Log2(measure), 0.01))
        {
            // 拍子の細かさを基準に加算 (1,0.5,0.333,0.25...)
            bool found = false;
            for (int q = 1; q <= 48; q++)
            {
                double p = measure * q;
                if (Accept(p, 0.01))
                {
                    // 素因数ペナルティを加算
                    double penalty = OddPrimePenalty((int)p);
                    rhythm[3] = Max(rhythm[3], penalty);
                    found = true;
                    break;
                }
            }
            // 当てはまらない場合は素因数ペナルティを加算
            if (!found)
                rhythm[3] = OddPrimePenalty((int)Round(measure));
        }
        // 左側のリズムを解析
        double beat = L.Separate;
        if (beat >= 1 && (beat <= 24 || beat % 8.0 > 0.1)) // 1~24 or 8n
        {
            if (beat > 96) // 短い間隔は対象外
            {
                // なにもしない
            }
            else if (Accept(Log2(beat), 0.01)) // 4分,8分など
            {
                rhythm[0] = Max(rhythm[0], 0.1);
            }
            else if (Accept(beat * 4.0 % 3.0, 0.01, true)) // 12分,24分
            {
                rhythm[0] = Max(rhythm[0], 1.0);
            }
            else if (Accept(beat * 4.0 % 5.0, 0.01, true)) // 10分,20分
            {
                rhythm[0] = Max(rhythm[0], 2.0);
            }
            else if (Accept(beat * 4.0 % 7.0, 0.01, true)) // 14分,28分
            {
                rhythm[0] = Max(rhythm[0], 3.0);
            }

            else if (Accept(beat * 3.0 % 4.0, 0.01, true)) // 符点8分,符点16分
            {
                rhythm[0] = Max(rhythm[0], 0.5);
            }
            else if (Accept(beat * 5.0 % 4.0, 0.01, true)) // 5符点8分,5符点16分
            {
                rhythm[0] = Max(rhythm[0], 1.0);
            }
            else if (Accept(beat * 7.0 % 4.0, 0.01, true)) // 7符点8分,7符点16分
            {
                rhythm[0] = Max(rhythm[0], 1.5);
            }
            else
            {
                var r = Rational.FromDouble(4.0 / beat, 10000, 0.0001);
                var small = Rational.FromDouble(4.0 / beat, 96, 0.005);

                int den = small.Den < r.Den ? small.Den : r.Den;
                double odd = OddPrimePenalty(den);
                if (r.Den > den * 2) odd += 0.5 * OddPrimePenalty(r.Den);
                rhythm[0] = Max(rhythm[0], Clamp(odd, 1.0, 6.0));
            }
        }

        // 両方のリズムを比較
        double dbeat = R.Separate;
        if (dbeat >= 1) // 1~24 or 8n
        {
            double dif = Round(Diff(beat * R.BPM, dbeat * L.BPM), 3);
            double ratio = Ratio(4.0 / beat, R.BPM, 4.0 / dbeat, L.BPM);
            var rL = Rational.FromDouble(4.0 / beat, 10000, 0.001);
            var rR = Rational.FromDouble(4.0 / dbeat, 10000, 0.001);

            if (dif >= 3.0 || ratio == 1) // 同じ間隔, 大きく違う場合はなにもしない
            {
                // なにもしない
            }
            else if (dif < 1.1 && dif > 0.9 && L.Measure * L.BPM == R.Measure * R.BPM) // ほぼ同じ間隔
            {
                // なにもしない
            }
            else if (beat > 96 || dbeat > 96) // 短い間隔は対象外
            {
                // なにもしない
            }
            else if (Accept(ratio % 2.0)) // 前の音符間隔の2の階乗倍の間隔の音符の数（6分→12分・10分→12分など）
            {
                rhythm[1] = Max(rhythm[1], 1.0);
            }
            else if (Accept(ratio % 1.5, 0.001, true)) // 1.5倍を基礎とした間隔（8分→付点8分・16分→24分）×3
            {
                rhythm[1] = Max(rhythm[1], 1.5);
            }
            else if (Accept(ratio % 1.333, 0.001, true)) // 4/3倍を基礎とした間隔（8分→12分・16分→48分）×2
            {
                rhythm[1] = Max(rhythm[1], 2.0);
            }
            else if (Accept(ratio % 1.6, 0.001, true)) // 5/4倍を基礎とした間隔（16分→20分など）×4
            {
                int gcd = MathExtend.GCD(rL.Den, rR.Den);
                rhythm[1] = gcd % 5 > 0 ? Max(rhythm[1], 1.25) : Max(rhythm[1], 4.0);
            }
            else if (Accept(4 / ratio % 3.5, 0.001, true)) // 4/7倍を基礎とした間隔（16分→28分など）×5
            {
                int gcd = MathExtend.GCD(rL.Den, rR.Den);
                rhythm[1] = gcd % 7 > 0 ? Max(rhythm[1], 1.333) : Max(rhythm[1], 5.0);
            }
            else if (L.Measure * L.BPM != R.Measure * R.BPM) // BPM揺れ
            {
                rhythm[2] = Max(rhythm[2], dif);
            }
            else // その他のポリ系
            {
                // 12の約数まで
                int lcm = MathExtend.LCM(rL.Den, rR.Den);
                double odd = OddPrimePenalty(lcm);
                rhythm[1] = lcm <= 12 ? Max(rhythm[1], odd / 4.0) : Max(rhythm[1], odd);
            }
        }

        double adds = Log10(1000.0 / Max(L.Length, 40));
        for (int i = 0; i < rhythm.Length; i++) rhythm[i] *= 1.0 + 0.5 * adds;
        return rhythm;
    }

    #region GC

    private static double OddPrimePenalty(int S)
    {
        if (S <= 1) return 0;
        // 素因数分解（小さい素数だけで十分）
        int e2 = 0, e3 = 0, e5 = 0, e7 = 0, e11 = 0, e13 = 0, e17 = 0, e19 = 0;
        int i = S;
        while ((i & 1) == 0) { e2++; i >>= 1; }
        while (i % 3 == 0) { e3++; i /= 3; }
        while (i % 5 == 0) { e5++; i /= 5; }
        while (i % 7 == 0) { e7++; i /= 7; }
        while (i % 11 == 0) { e11++; i /= 11; }
        while (i % 13 == 0) { e13++; i /= 13; }
        while (i % 17 == 0) { e17++; i /= 17; }
        while (i % 19 == 0) { e19++; i /= 19; }

        double ebase = 1.0;
        ebase *= Pow(1.1, e3);  // 3の冪
        ebase *= Pow(1.25, e5);  // 5の冪
        ebase *= Pow(1.333, e7);  // 7の冪
        ebase *= Pow(1.5, e11); // 11の冪
        ebase *= Pow(1.666, e13); // 13の冪
        ebase *= Pow(1.75, e17); // 17の冪
        ebase *= Pow(2.0, e19); // 19の冪

        double sum = ebase;
        // 2の冪が深すぎる場合の軽い加点（1/16超から）
        sum += 0.1 * Max(0, e2 - 4);
        if (i == 1)
            return sum; // 2,3,5,7...の素因数のみ
        sum -= 1.0;

        // 13以上の素因数がある場合
        double ls = Round(Log10(i), 2);
        return ls * 1.5 + sum;
    }
    private static int NextPow2(int x)
    {
        if (x <= 1) return 1;
        x--;               // ceil のために一旦減らす
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        return x + 1;
    }
    /// <summary>
    /// nA分とnB分の“順序に依らない”比較値。
    /// 二者のLCMを、次の2の累乗に引き上げる倍率（>=1）を返す。
    /// 例: (8,12) -> 32/24 = 1.3333333...
    /// </summary>
    private static double Ratio(double nA, double nB)
    {
        if (nA == nB) return 1.0;
        var rA = Rational.FromDouble(nA, 10000, 0.001);
        var rB = Rational.FromDouble(nB, 10000, 0.001);

        int m = MathExtend.LCM(rA, rB);
        if (m <= 1) return 1.0;

        int p2 = NextPow2(m);
        return (double)p2 / m;
    }
    /// <summary>
    /// 実時間ベース版：2進グリッドに合わせるための倍率（順序独立・BPM反映）
    /// 例: (8分, BPM=120) vs (12分, BPM=120) → 1.333...
    /// </summary>
    public static double Ratio(double nA, double bpmA, double nB, double bpmB)
    {
        if (bpmA == bpmB) return Ratio(nA, nB);

        var tA = DurationR(nA, bpmA); // KA/DA
        var tB = DurationR(nB, bpmB); // KB/DB

        int m = MathExtend.LCM(tA, tB);
        if (m <= 1) return 1.0;

        int p2 = NextPow2(m);
        return (double)p2 / m;
    }
    // n分 & BPM（少数OK, dottedなら×3/2）→ 実時間 (秒) の有理数
    private static Rational DurationR(double n, double bpm)
    {
        bpm = Abs(bpm);
        if (bpm <= 0) return new Rational(0, 1);
        var rn = Rational.FromDouble(n, 10000, 0.01);
        var rBpm = Rational.FromDouble(bpm, 10000, 0.01);
        var f = new Rational(1, 1);

        // t = (60/BPM) * (4/n) * f = (240 * f) / (BPM * n)
        // = (240 * f.Num * rBpm.Den * rn.Den) / (f.Den * rBpm.Num * rn.Num)
        int num = checked(240 * f.Num);
        int den = checked(f.Den);
        int N = checked(num * rBpm.Den);
        int D = checked(den * rBpm.Num);
        long lN = checked((long)N * rn.Den);
        long lD = checked((long)D * rn.Num);
        try
        {
            N = checked((int)lN);
            D = checked((int)lD);
            return new Rational(N, D);
        }
        catch (Exception ex)
        {
            AstrumLoom.Log.Write("DurationR計算エラー : " + ex);
            return new Rational(0, 1);
        }
    }
    #endregion
    #region Func
    public List<(Chip[] chip, double beat)> BeatJoint(List<Chip> chips)
    {
        List<(Chip[] chip, double beat)> joints = [];
        (List<Chip> chip, double beat, double point) joint = ([], 0, 0);
        for (int i = 1; i < chips.Count; i++)
        {
            Chip L = chips[i - 1], R = chips[i];
            double dBeat = R.Beat - L.Beat;
            if (dBeat <= 0) continue;

            double n16 = 4.0 / Snap(dBeat);
            if (joint.point == 0) joint.point = n16;
            else if (joint.point != n16)
            {
                if (joint.chip.Count > 0) joints.Add(([.. joint.chip], Round(joint.beat / joint.chip.Count, 5, MidpointRounding.AwayFromZero)));
                joint = ([], 0, n16);
            }
            joint.chip.Add(L);
            joint.beat += dBeat;
        }
        if (joint.chip.Count > 0) joints.Add(([.. joint.chip], Round(joint.beat / joint.chip.Count, 5, MidpointRounding.AwayFromZero)));
        return joints;
    }
    private double Snap(double x)
    {
        int bp = 1, bq = 1; double bval = 1.0, berr = double.MaxValue;
        for (int q = 1; q <= 48; q++)
        {
            int p = (int)Round(x * q);
            if (p == 0) continue;
            double v = (double)p / q;
            double err = Abs(x - v) / Max(1e-9, v);
            if (err < berr) { berr = err; bp = p; bq = q; bval = v; }
        }
        return bval;
    }
    private double GetPolyRhythm(double n16)
    {
        double n48 = Round(48 / n16, 5, MidpointRounding.AwayFromZero);
        double pl3 = Round(Log2(n16 * 1.33334), 4, MidpointRounding.AwayFromZero);//4/3
        if (pl3 * 4.0 % 1.0 == 0 || n48 * 4.0 % 3.0 == 0 || n16 * 4.0 % 3.0 == 0) // 3x系音符
        {
            return 3.0;
        }
        double pl5 = Log2(Round(n16 * 0.8, 4, MidpointRounding.AwayFromZero));//4/5
        if (pl5 * 4.0 % 1.0 == 0 || n48 * 4.0 % 5.0 == 0 || n16 * 4.0 % 5.0 == 0) // 5x系音符
        {
            return 5.0;
        }
        double pl7 = Log2(Round(n16 * 4.0 / 7.0, 4, MidpointRounding.AwayFromZero));//4/7
        return pl7 * 4.0 % 1.0 == 0 || n48 * 4.0 % 7.0 == 0 || n16 * 4.0 % 7.0 == 0 ? 7.0 : n48 * 8.0 % 1.0 < 0.01 ? 4.0 : 10.0;
    }
    #endregion
    #region OldVersion
    /*public double RaderRhythm(Course course)
    {
        var hits = course.HitChips;
        if (hits.Count < 2) return 0;

        double rhythm = 0;
        for (int i = 1; i < hits.Count - 1; i++)
        {
            Chip L = hits[i - 1], R = hits[i];
        }
        if (course.ChipLength == 0) return 0;
        double rtm = rhythm / course.ChipLength * 1000.0;
        return rtm * 200.0 / 75.0;
    }*/
    private static double GetRhythmValue(Chip[] chips)
    {
        if (chips.Length < 2) return 0;
        List<double> rhythms = [];
        for (int i = 1; i < chips.Length - 1; i++)
        {
            Chip L = chips[i - 1], R = chips[i];
            double r = GetRhythmValue(L, R);
            if (r > 0) rhythms.Add(r);
        }
        return TopRate(rhythms);
    }
    private static double GetRhythmValue(Chip L, Chip R)
    {
        double rhythm = 0;
        double mes = 4.0 / R.Measure;
        double range = R.Separate;//
        if (mes % 2 != 0 && mes % 0.5 == 0) rhythm += 1;
        if (range >= 1 && (range <= 24 || range % 8.0 > 0.1))
        {
            double add = 0;
            if (range % 4.0 < 0.1)//4分や8分
                add = 0.1;
            add = range % 2.0 < 0.1
                ? 1
                : range % 1.2 < 0.01
                ? 50
                : range % 1.25 < 0.01
                ? 40
                : range % 1.5 < 0.01
                ? 30
                : range % 1.333 < 0.01 ? 20 : range % (mes / 4.0) < 0.01 ? 0.1 : range < 2.0 ? 0.1 : range < 8.0 ? 5 : 50;

            double prev = L.Separate;
            if (prev >= 1)
            {
                double dif = Round(Diff(range * R.BPM, prev * L.BPM), 3);
                if (dif > 1)
                {
                    if (dif > 2.0)// (2打ぎみ)リズム
                        add += 5;
                    else if (dif % 1 == 0)
                        add += 10;
                    else if (dif % 1.25 == 0)
                        add += 50;
                    else if (dif % 1.333 == 0)//12,24
                        add += 40;
                    else if (dif % 1.5 == 0)//符点
                        add += 20;
                    else
                        add += 30;
                }
            }
            rhythm += add;
        }
        return rhythm * Log10(1000.0 / Max(R.Length, 50));
    }
    #endregion
    #endregion

    #region Soflan
    public double SOFLAN_BPM = 0;
    public double SOFLAN_Scroll = 0;
    public double RaderSoflan(Course course)
    {
        var hits = course.HitChips;
        if (hits.Count < 2) return 0;
        double[] calc = CalcSoflan([.. hits], [.. course.BpmList], course.IsHBS > 0);
        SOFLAN_BPM = calc[0];
        SOFLAN_Scroll = calc[1];
        double refsof = 188;
        double soflan = Scale(SOFLAN_BPM + SOFLAN_Scroll, refsof);
        //double sfl = Log2(soflan / course.ChipLength * 1000.0);
        return soflan;
    }
    public static double MiniSoflan(Chip[] chips, BPM[] bpmlist, bool hbs = false)
    {
        if (chips.Length == 0) return 0;
        double length = Min(chips[^1].Time + chips[^1].Length - chips[0].Time, 2000.0);
        if (length == 0) return 0;

        double[] calc = CalcSoflan(chips, bpmlist, hbs);
        double refsof = 188;
        double soflan = Scale(calc[0] + calc[1], refsof);
        //double sfl = Log2(soflan / course.ChipLength * 1000.0);
        return soflan;
    }
    private static double[] CalcSoflan(Chip[] chips, BPM[] bpmlist, bool hbs = false)
    {
        List<double> bpm = [];
        List<double> scroll = [];
        double b = 0;
        double s = 0;
        double v = 1.0;
        foreach (var chip in chips)
        {
            double sof = hbs ? chip.BPM : chip.BPM * chip.Scroll;
            double per = sof * s != 0 ? Abs(sof > s ? sof / s : s / sof) : 1;

            if (chip.BPM != b && b != 0)
            {
                v = chip.BPM * b != 0 ? Diff(chip.BPM, b) : 1;
            }
            double wide = Min(per, 4.0);
            double l = chip.Length > 0 ? Log10(1000.0 / chip.Length) : 0;
            if (wide + l > 0)
            {
                if (s != 0 && per >= 1.1)
                {
                    double target = wide * (v % 1 > 0.0 ? 25 : 10) * l;
                    double p = bpmlist.Where(x => x.Time < chip.Time).Select(x => x.Time).DefaultIfEmpty(-1).Max();
                    if (p > 0 && chip.Time - p <= 2000 && per >= 1.1)
                    {
                        bpm.Add(Easing.Ease(chip.Time - p, 2000, target, 0));
                    }
                    else
                    {
                        scroll.Add(target);
                    }
                }
            }
            s = sof;
            b = chip.BPM;
        }
        double bp = TopRate(bpm);
        double hs = TopRate(scroll);
        return [bp, hs];
    }
    #endregion

    #region Gimmick
    public double GIMMICK_Roll = 0;
    public double GIMMICK_Balloon = 0;
    public double GIMMICK_Branch = 0;
    public double GIMMICK_Barline = 0;

    public double RaderGimmick(Course course)
    {
        // 1) 走査準備
        double totalMs = course.ChipLength; // 譜面長(ms)
        if (totalMs <= 0) return 0;

        // 集計
        double rollTimeMs = 0.0, bigRollTimeMs = 0.0;
        double rollScoreSum = 0.0;
        int rollCount = 0;

        double balloonScoreSum = 0.0;
        int balloonCount = 0;

        double branchScoreSum = 0.0;

        double barlineOffMs = 0.0;

        // 2) ロール／バルーンの抽出（5/6開始〜8終了 を想定）
        bool inRoll = false; bool isBigRoll = false;
        double rollStartMs = 0.0, rollEndMs = 0.0;
        var runColors = new List<int>();

        foreach (var bar in course.Lanes[0])
        {
            if (!course.InBranch(bar)) continue;
            // バーラインOFF
            if (!bar.Visible && bar.Time < course.Length)
                barlineOffMs += Abs(Min(bar.Length, 240000 / bar.BPM));

            foreach (var chip in bar.Chips)
            {
                // ロール開始
                if (chip.Type is ENote.Roll or ENote.ROLL)
                {
                    inRoll = true; isBigRoll = chip.Type == ENote.ROLL;
                    rollStartMs = chip.Time; runColors.Clear();
                    continue;
                }
                // バルーン（#7 相当）
                if (chip.RollMax > 0)
                {
                    // 次のロール終端(8)までが継続区間と仮定
                    double balStart = chip.Time;
                    double balEnd = chip.LongEnd?.Time ?? balStart; // 無ければバー終端まで
                    double durSec = Max(0.05, (balEnd - balStart) / 1000.0);

                    if (balStart < course.Length)
                    {
                        int req = chip.RollMax > 0 ? chip.RollMax : 5;
                        double dps = req / durSec;

                        const double RefDps = 12.0; // 基準DPS
                        double load = Pow(Min(3.0, Min(dps, 60) / RefDps), 1.2); // 少し非線形

                        // くす玉は係数アップ
                        if (chip.Type == ENote.Potato) load *= 1.25;

                        balloonScoreSum += load;
                    }
                    else // 譜面長超過分は軽減
                    {
                        balloonScoreSum += 0.1;
                    }
                    balloonCount++;
                    continue;
                }
                // ロール終端
                if (chip.Type == ENote.End)
                {
                    if (inRoll)
                    {
                        rollEndMs = chip.Time;
                        double durMs = Clamp(rollEndMs - rollStartMs, 0, 3000);
                        rollTimeMs += durMs;
                        if (isBigRoll) bigRollTimeMs += durMs;

                        // ロールの“負荷”は長さで：上振れ抑制 eff(L)
                        double L = Max(1.0, durMs / 100.0); // 100ms単位
                        double eff = L / Max(1.0, Log10(L));
                        // ビッグロールは係数
                        if (isBigRoll) eff *= 1.15;

                        rollScoreSum += eff;
                        rollCount++;

                        inRoll = false; isBigRoll = false;
                    }
                    continue;
                }
            }
        }

        // 3) 分岐複雑度（ブロックごとに密度差を見る）
        foreach (var br in course.BranchList[0]) // あなたの構造に合わせて
        {
            double lengthMs = br.End - br.Start; // ブロックの長さ
            // 例：N/E/M のそれぞれで（単打数 / 秒）を出して spread を計算
            var dens = new List<double>();
            foreach (var lane in br.Lanes) // Normal/Expert/Master 等
            {
                // 分岐のノーツのみをカウント
                double notes = lane.Where(b => course.InBranch(b) && b.Time >= br.Start && b.Time + b.Length < br.End)
                    .Select(b => b.HitChip).Count();
                double lenSec = Max(0.01, lengthMs / 1000.0);
                dens.Add(notes / lenSec);
            }
            if (dens.Count >= 2)
            {
                double maxd = dens.Max(), mind = dens.Min();
                if (maxd > 0)
                {
                    double spread = (maxd - mind) / maxd;  // 0..1
                    double weight = Max(0.05, Min(totalMs, lengthMs) / totalMs); // 占有率で重み
                    branchScoreSum += spread * weight * 10.0; // 係数は後で合わせ込み
                }
            }
        }

        // 4) 正規化スコア（0..200）
        // ROLL：時間割合＋長さ負荷
        double rollRatio = rollTimeMs / totalMs;               // 0..1
        GIMMICK_Roll = rollScoreSum * rollRatio; // 係数は後述
        double roll = Scale(GIMMICK_Roll, 36.0);

        // BALLOON：要求DPS合計
        GIMMICK_Balloon = balloonScoreSum;   // バルーン多い譜面で ~200
        double balloon = Scale(Log2(GIMMICK_Balloon), 6.0);

        // BRANCH：複雑度の合計
        GIMMICK_Branch = branchScoreSum;
        double branch = Scale(GIMMICK_Branch, 10.0);

        // BARLINEOFF：時間割合
        GIMMICK_Barline = Min(barlineOffMs, course.Length / 4.0) / totalMs;
        double barline = Scale(GIMMICK_Barline, 0.25);

        // 合成（ROLL 0.35 / BALLOON 0.40 / BRANCH 0.15 / BARLINE 0.10）
        double score = 0.40 * roll + 0.30 * balloon
            + 0.20 * branch + 0.10 * barline;
        return score * 1.5;
    }
    #region OldVersion
    /*public double RaderGimmick(Course course)
    {
        double gimmick = 0;
        foreach (var bar in course.Lanes[0])
        {
            foreach (var com in bar.Commands)
            {
                if (com.Branch == 0)
                {
                    gimmick += 1000.0 / course.ChipLength;
                }
            }
        }
        return gimmick * 200.0 / 5.0;
    }*/
    #endregion
    #endregion

    #region Other
    #endregion

    #region Satellite
    private void SetHexa(Course course)
    {
        double length = course.Length;
        //double sound = Sound.GetLength(course.Header.WavePath);
        //if (sound > 0 && sound < length) length = sound;
        var listchip = course.HitChips.ToArray();
        double lasttime = 0, firsttime = 0;
        if (listchip != null)
        {
            for (int i = listchip.Length - 1; i >= 0; i--)
            {
                if (listchip[i].Type > ENote.None)
                {
                    lasttime = listchip[i].Time;
                    break;
                }
            }
        }
        if (length > 0) lasttime = length;
        lasttime = Min(lasttime, 1000.0 * 60 * 60);

        if (listchip != null && listchip.Length > 0)
        {
            firsttime = listchip[0].Time;
            for (int i = listchip.Length - 1; i >= 0; i--)
            {
                if (listchip[i].Time < lasttime && listchip[i].Type > ENote.None && listchip[i].Type < ENote.Roll) { lasttime = listchip[i].Time; break; }
            }
        }
        int note = course.Notes;
        double averagenotes = note / ((lasttime - firsttime) / 1000.0);
        double notes = averagenotes * 20;
        if (note == 0)
        {
            return;
        }

        int secnotes = 0;
        double notetime = 0, stream = 0;
        int change = 0;
        double allbpm = 0;
        double rhythm = 0;
        double gimmick = 0;
        if (listchip != null && listchip.Length > 0)
        {
            int last = 0;
            for (int i = listchip.Length - 1; i >= 0; i--)
            {
                if (listchip[i].Time < lasttime) { last = i; break; }
            }

            #region Peak
            double seek = lasttime / 30.0;
            for (int i = 0; i < (int)(lasttime / 100); i++)
            {
                int sec = 0;
                double not = 0;
                for (int j = 0; j < last; j++)
                {
                    Chip? chip = listchip[j], next = null;
                    if (chip.Type > ENote.None && chip.Type < ENote.Roll && chip.Time >= i * 100.0 && chip.Time < i * 100.0 + seek)
                    {
                        for (int k = j + 1; k < last; k++)
                        {
                            if (listchip[k].Type is > ENote.None and < ENote.Roll)
                            {
                                next = listchip[k];
                                break;
                            }
                        }
                        sec++;
                        double t = (next != null ? next.Time : chip.Time) - chip.Time;
                        if (t < 10.0) continue;
                        t = t != 0.0 ? t : 1000.0;
                        double l = t < 100 / 3.0 ? Math.Log10(1000.0 / t) * 20.5 : 1000.0 / t;
                        if (!double.IsNaN(l)) not += l * (chip.Type >= ENote.DON ? 2 : 1);// (int)
                    }
                    else if (chip.Time >= i * 100.0 + seek) break;
                }
                double notsec = not / (sec == 0 ? 1 : sec);
                if (double.IsNaN(notsec))
                    notsec = 0;
                if (sec > secnotes)
                {
                    secnotes = sec;
                    notetime = notsec;
                }
                else if (notsec > notetime) notetime = notsec;
            }
            #endregion

            #region Stream
            double averagems = 1000.0 / averagenotes * 0.7;
            double stre = 0, st = 1, strems = 0, ms = 0;
            bool streaming = false;
            for (int i = 0; i < last - 1; i++)
            {
                Chip? chip = listchip[i], next = null;
                if (chip.Type is > ENote.None and < ENote.Roll)
                {
                    for (int k = i + 1; k < last; k++)
                    {
                        if (listchip[k].Type is > ENote.None and < ENote.Roll)
                        {
                            next = listchip[k];
                            break;
                        }
                    }
                    if (next != null)
                    {
                        double t = next.Time - chip.Time;
                        double m = chip.BPM >= 400.0 ? chip.Measure : 1.0;
                        double b = chip.BPM * m;
                        double bartime = 240000.0 / b;
                        double onp = t / bartime;
                        if (t <= averagems || onp <= 0.0625)
                        {
                            st++;
                            ms += 1000.0 / t;
                            streaming = true;
                        }
                        else if (streaming)
                        {
                            double ns = ms / (st - 1);
                            if (st > 2 && ns <= 48)
                            {
                                stre += st;
                                strems += st / 100.0 * ns * ns;
                            }
                            st = 1;
                            ms = 0;
                            streaming = false;
                        }
                    }
                }
            }
            stream = strems / note * 100;
            if (double.IsNaN(stream)) stream = 0;
            #endregion

            #region SOF-LAN
            double nowbpm = listchip[0].BPM;// * (listchip[0].BPM >= 400.0 ? listchip[0].Measure : 1.0);
            double softime = 0, sofbpm = 0;
            for (int i = 0; i < last; i++)
            {
                var chip = listchip[i];
                double m = chip.BPM >= 400.0 ? chip.Measure : 1.0;
                double b = chip.BPM * m;
                if (chip.BPM != nowbpm)
                {
                    double bpm = Math.Abs(b - nowbpm * m);
                    allbpm += bpm >= 100 ? 100 : bpm;
                    sofbpm = Math.Abs(b - nowbpm * m);
                    nowbpm = chip.BPM;
                    softime = chip.Time;
                    change++;
                }
                if (chip.Type is > ENote.None and < ENote.Roll)
                {
                    if (chip.Time >= softime && chip.Time < softime + 1000.0 && chip.Type > ENote.None && chip.Type < ENote.Roll)
                    {
                        allbpm += sofbpm * 10.0 * (1000 - (chip.Time - softime)) / 1000.0;
                    }
                }
            }
            #endregion

            #region Rhythm
            double rhm = 0;
            int count = 0;
            int rt = 1;
            bool rhming = false;
            int v = 10000;
            for (int i = 0; i < last - 1; i++)
            {
                Chip? chip = listchip[i], next = null, prev = null;
                if (chip.Type is > ENote.None and < ENote.Roll)
                {
                    for (int k = i + 1; k < last; k++)
                    {
                        if (listchip[k].Type is > ENote.None and < ENote.Roll)
                        {
                            next = listchip[k];
                            break;
                        }
                    }
                    for (int k = i - 1; k >= 0; k--)
                    {
                        if (listchip[k].Type is > ENote.None and < ENote.Roll)
                        {
                            prev = listchip[k];
                            break;
                        }
                    }
                    if (next != null && prev != null)
                    {
                        double addrhm;
                        double t = next.Time - chip.Time;
                        double m = chip.BPM >= 400.0 ? chip.Measure : 1.0;
                        double b = chip.BPM * m;
                        double bartime = 240000.0 / b;
                        double onp = Math.Round(1.0 / (t / bartime), 2, MidpointRounding.AwayFromZero);
                        double pt = chip.Time - prev.Time;
                        double pm = prev.BPM >= 400.0 ? prev.Measure : 1.0;
                        double pb = prev.BPM * pm;
                        double pbartime = 240000.0 / pb;
                        double ponp = Math.Round(1.0 / (pt / pbartime), 2, MidpointRounding.AwayFromZero);
                        if (onp != ponp && onp >= 1 && ponp >= 1)
                        {
                            double dif = MaxMeasure(onp, ponp);
                            addrhm = onp > 48 || ponp > 48 ? 0.1 : dif % 2 == 0 ? 0.5 : dif % 3 == 0 ? 1 : dif % 1.5 == 0 ? 4 : dif % (4 / 3) < 0.01 ? 3 : 8;
                            if (onp <= 8 || ponp <= 8) addrhm /= 2;
                            rhm += addrhm * v;
                        }
                        if (onp > 8)
                        {
                            rt++;
                            rhming = true;
                        }
                        else if (rhming)
                        {
                            if (rt % 2 == 0)
                            {
                                rhm += 2 * v;
                            }
                            rt = 1;
                            rhming = false;
                        }
                    }
                }
            }
            rhythm = rhm / (lasttime - firsttime);
            #endregion

            #region Gimmick
            double gim = 0;
            double scr = Math.Abs(listchip[0].Scroll);
            double scbpm = listchip[0].BPM;
            bool gogo = listchip[0].Gogo;
            count = 0;
            int charge = 0;
            for (int i = 0; i < last; i++)
            {
                var chip = listchip[i];
                double s = Math.Abs(chip.Scroll);
                if (chip.Type is > ENote.None and < ENote.Roll)
                {
                    if (s != scr)
                    {
                        double sc = Math.Abs(chip.Scroll * chip.BPM - scr * scbpm) * Math.Abs(chip.Scroll - scr);
                        gim += sc > 1000.0 ? 1000.0 : sc;// Math.Abs(sc) < 100.0 ? sc : 100.0;
                        scr = s;
                        scbpm = chip.BPM;
                        count++;
                    }
                    if (chip.LongEnd != null)
                    {
                        charge += 3;
                        var inlong = listchip.Where(c => c.Time >= chip.Time - 10 && c.Time <= chip.LongEnd.Time + 10).ToArray();
                        foreach (var c in inlong)
                        {
                            if (c.Time >= chip.Time - 10 && c.Time < chip.Time + 10)
                            {
                                charge += 6;
                            }
                            else charge += 10;
                        }
                    }
                }
                else
                {
                    if (s != scr)
                    {
                        double sc = Math.Abs(chip.Scroll - scr) * Math.Abs(chip.Scroll - scr);
                        gim += sc > 10.0 ? 10.0 : sc;// Math.Abs(sc) < 100.0 ? sc : 100.0;
                        scr = s;
                        scbpm = chip.BPM;
                    }
                }

                if (gogo != chip.Gogo)
                {
                    gim += 2.0;
                    gogo = chip.Gogo;
                }
            }
            gimmick = gim / (count + 0.5) + charge * 0.15;
            #endregion
        }
        oldNOTES = notes;// notes > 200.0 ? 200.0 : notes;
        if (secnotes > 0)
        {
            oldPEAK = notetime * 10;// notetime / bpmsec * (100.0 / notes) + secnotes * notes / 100;
            oldSTREAM = stream;//1000.0 * bpmmax / release / (streamave * streamave * 2.5) + (streamtime / 300.0);
            oldRHYTHM = rhythm;// rhythm / 100.0 + (rhythm / (notes * 2));// * (bpmmax / 120.0);// + (rhythm * 10.0 / (notes * notes));// * (100.0 / (notes * 0.5)) / 20.0;
            oldSOFLAN = allbpm > 0.0 ? allbpm / 40.0 / change + change * 2 * (notes / 100.0) : 0;
            oldGIMMICK = gimmick / 5.0;
        }
    }
    internal double oldNOTES, oldPEAK, oldSTREAM, oldRHYTHM, oldSOFLAN, oldGIMMICK;
    public static double MaxMeasure(double value1, double value2)
    {
        double v1 = Math.Round(value1, 2, MidpointRounding.AwayFromZero);
        double v2 = Math.Round(value2, 2, MidpointRounding.AwayFromZero);
        return value2 > value1 ? v2 / v1 : v1 / v2;
    }
    #endregion

    #endregion

    public override string ToString() => $"Total:{Round(Total, 3, MidpointRounding.AwayFromZero)} \n" +
            $"Notes:{Round(Notes, 2, MidpointRounding.AwayFromZero)} \nPeak:{Round(Peak, 2, MidpointRounding.AwayFromZero)} \n" +
            $"Stream:{Round(Stream, 2, MidpointRounding.AwayFromZero)} \nRhythm:{Round(Rhythm, 2, MidpointRounding.AwayFromZero)} \n" +
            $"Sof-lan:{Round(Soflan, 2, MidpointRounding.AwayFromZero)} \nGimmick:{Round(Gimmick, 2, MidpointRounding.AwayFromZero)}";

    public bool Enable(int type) => type switch
    {
        1 => Peak > 9,
        2 => Rhythm > 9,
        3 => Soflan > 9,
        4 => Gimmick > 9,
        5 => Stream > 9,
        _ => Notes > 0,
    };

    public int TopRader()
    {
        int value = -1;
        double max = 0;
        if (Notes > max)
        {
            max = Notes;
            value = 0;
        }
        if (Peak > max)
        {
            max = Peak;
            value = 1;
        }
        if (Rhythm > max)
        {
            max = Rhythm;
            value = 2;
        }
        if (Soflan > max)
        {
            max = Soflan;
            value = 3;
        }
        if (Gimmick > max)
        {
            max = Gimmick;
            value = 4;
        }
        if (Stream > max)
        {
            max = Stream;
            value = 5;
        }
        return value;
    }
}
