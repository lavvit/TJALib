using AstrumLoom;

using static System.Math;

using TJARader = TJALib.TJA.Rader;

namespace TJALib.Draw;

public static class NoteRader
{
    public static void DrawBase(double x, double y, int size = 80, double max = 100)
    {
        double rX = size * Cos(PI / 6) * 2, rY = size * Sin(PI / 6) * 2;
        (double x, double y)[] hx =
            [
            (x, y),
            (x, y - size * 2),
            (x + rX, y - rY),
            (x + rX, y + rY),
            (x, y + size * 2),
            (x - rX, y + rY),
            (x - rX, y - rY) ];
        Drawing.Triangle(hx[0].x, hx[0].y, hx[1].x, hx[1].y, hx[2].x, hx[2].y, Color.LightGray, opacity: 0.6, thickness: 1);
        Drawing.Triangle(hx[0].x, hx[0].y, hx[2].x, hx[2].y, hx[3].x, hx[3].y, Color.LightGray, opacity: 0.6, thickness: 1);
        Drawing.Triangle(hx[0].x, hx[0].y, hx[3].x, hx[3].y, hx[4].x, hx[4].y, Color.LightGray, opacity: 0.6, thickness: 1);
        Drawing.Triangle(hx[0].x, hx[0].y, hx[4].x, hx[4].y, hx[5].x, hx[5].y, Color.LightGray, opacity: 0.6, thickness: 1);
        Drawing.Triangle(hx[0].x, hx[0].y, hx[5].x, hx[5].y, hx[6].x, hx[6].y, Color.LightGray, opacity: 0.6, thickness: 1);
        Drawing.Triangle(hx[0].x, hx[0].y, hx[6].x, hx[6].y, hx[1].x, hx[1].y, Color.LightGray, opacity: 0.6, thickness: 1);

        double rootX = size * Cos(PI / 6), rootY = size * Sin(PI / 6);
        (double x, double y)[] hexvalue =
            [
            (x, y),
            (x, y - size),
            (x + rootX, y - rootY),
            (x + rootX, y + rootY),
            (x, y + size),
            (x - rootX, y + rootY),
            (x - rootX, y - rootY) ];
        Drawing.Circle(hexvalue[0].x, hexvalue[0].y, size, Color.DimGray, opacity: 0.2, thickness: 3);
        Drawing.Triangle(hexvalue[0].x, hexvalue[0].y, hexvalue[1].x, hexvalue[1].y, hexvalue[2].x, hexvalue[2].y, Color.Black, opacity: 0.5);
        Drawing.Triangle(hexvalue[0].x, hexvalue[0].y, hexvalue[2].x, hexvalue[2].y, hexvalue[3].x, hexvalue[3].y, Color.Black, opacity: 0.5);
        Drawing.Triangle(hexvalue[0].x, hexvalue[0].y, hexvalue[3].x, hexvalue[3].y, hexvalue[4].x, hexvalue[4].y, Color.Black, opacity: 0.5);
        Drawing.Triangle(hexvalue[0].x, hexvalue[0].y, hexvalue[4].x, hexvalue[4].y, hexvalue[5].x, hexvalue[5].y, Color.Black, opacity: 0.5);
        Drawing.Triangle(hexvalue[0].x, hexvalue[0].y, hexvalue[5].x, hexvalue[5].y, hexvalue[6].x, hexvalue[6].y, Color.Black, opacity: 0.5);
        Drawing.Triangle(hexvalue[0].x, hexvalue[0].y, hexvalue[6].x, hexvalue[6].y, hexvalue[1].x, hexvalue[1].y, Color.Black, opacity: 0.5);
    }
    public static void Draw(this TJARader t, double x, double y, Color? color = null, bool mark = true, int size = 80, double max = 100, bool drawold = true)
    {
        color ??= Color.White;
        //if (SongData.NowSong.Type == EType.Score)
        {

            //if (Notes - 1 > max * 2) max = (1 + (int)((Notes - 1) / 100)) * 50;
            //if (Peak - 1 > max * 2) max = (1 + (int)((Peak - 1) / 100)) * 50;
            //if (Rhythm - 1 > max * 2) max = (1 + (int)((Rhythm - 1) / 100)) * 50;
            //if (Soflan - 1 > max * 2) max = (1 + (int)((Soflan - 1) / 100)) * 50;
            //if (Gimmick - 1 > max * 2) max = (1 + (int)((Gimmick - 1) / 100)) * 50;
            //if (Stream - 1 > max * 2) max = (1 + (int)((Stream - 1) / 100)) * 50;
            //size = (int)(size / (max / 100.0));
        }

        double rootX = size * Cos(PI / 6), rootY = size * Sin(PI / 6);
        (double x, double y)[] hexvalue =
            [
            (x, y),
            (x, y - size),
            (x + rootX, y - rootY),
            (x + rootX, y + rootY),
            (x, y + size),
            (x - rootX, y + rootY),
            (x - rootX, y - rootY) ];

        (double x, double y)[] hexrader =
            [
            (hexvalue[0].x, hexvalue[0].y),
            (hexvalue[0].x + (hexvalue[1].x - hexvalue[0].x) * (t.Notes / max), hexvalue[0].y + (hexvalue[1].y - hexvalue[0].y) * (t.Notes / max)),
            (hexvalue[0].x + (hexvalue[2].x - hexvalue[0].x) * (t.Peak / max), hexvalue[0].y + (hexvalue[2].y - hexvalue[0].y) * (t.Peak / max)),
            (hexvalue[0].x + (hexvalue[3].x - hexvalue[0].x) * (t.Rhythm / max), hexvalue[0].y + (hexvalue[3].y - hexvalue[0].y) * (t.Rhythm / max)),
            (hexvalue[0].x + (hexvalue[4].x - hexvalue[0].x) * (t.Soflan / max), hexvalue[0].y + (hexvalue[4].y - hexvalue[0].y) * (t.Soflan / max)),
            (hexvalue[0].x + (hexvalue[5].x - hexvalue[0].x) * (t.Gimmick / max), hexvalue[0].y + (hexvalue[5].y - hexvalue[0].y) * (t.Gimmick / max)),
            (hexvalue[0].x + (hexvalue[6].x - hexvalue[0].x) * (t.Stream / max), hexvalue[0].y + (hexvalue[6].y - hexvalue[0].y) * (t.Stream / max)),
        ];
        for (int i = 0; i < 6; i++)
        {
            int j = (i + 1) % 6;
            Drawing.Triangle(hexrader[0].x, hexrader[0].y, hexrader[i + 1].x, hexrader[i + 1].y, hexrader[j + 1].x, hexrader[j + 1].y, color, opacity: 0.333);
            Drawing.LineZ(hexrader[i + 1].x, hexrader[i + 1].y, hexrader[0].x, hexrader[0].y, color, opacity: 0.5, thickness: 1);
            Drawing.LineZ(hexrader[i + 1].x, hexrader[i + 1].y, hexrader[j + 1].x, hexrader[j + 1].y, Color.White, opacity: 0.75, thickness: 3);
            Drawing.LineZ(hexrader[i + 1].x, hexrader[i + 1].y, hexrader[j + 1].x, hexrader[j + 1].y, color, opacity: 1, thickness: 2);
        }
        #region Old
        if (drawold)
        {
            (double x, double y)[] oldrader =
                [
                (hexvalue[0].x, hexvalue[0].y),
            (hexvalue[0].x + (hexvalue[1].x - hexvalue[0].x) * (t.oldNOTES / max), hexvalue[0].y + (hexvalue[1].y - hexvalue[0].y) * (t.oldNOTES / max)),
            (hexvalue[0].x + (hexvalue[2].x - hexvalue[0].x) * (t.oldPEAK / max), hexvalue[0].y + (hexvalue[2].y - hexvalue[0].y) * (t.oldPEAK / max)),
            (hexvalue[0].x + (hexvalue[3].x - hexvalue[0].x) * (t.oldRHYTHM / max), hexvalue[0].y + (hexvalue[3].y - hexvalue[0].y) * (t.oldRHYTHM / max)),
            (hexvalue[0].x + (hexvalue[4].x - hexvalue[0].x) * (t.oldSOFLAN / max), hexvalue[0].y + (hexvalue[4].y - hexvalue[0].y) * (t.oldSOFLAN / max)),
            (hexvalue[0].x + (hexvalue[5].x - hexvalue[0].x) * (t.oldGIMMICK / max), hexvalue[0].y + (hexvalue[5].y - hexvalue[0].y) * (t.oldGIMMICK / max)),
            (hexvalue[0].x + (hexvalue[6].x - hexvalue[0].x) * (t.oldSTREAM / max), hexvalue[0].y + (hexvalue[6].y - hexvalue[0].y) * (t.oldSTREAM / max)),
        ];

            for (int i = 0; i < 6; i++)
            {
                int j = (i + 1) % 6;
                Drawing.LineZ(oldrader[i + 1].x, oldrader[i + 1].y, oldrader[j + 1].x, oldrader[j + 1].y, Color.White, 3);
                Drawing.LineZ(oldrader[i + 1].x, oldrader[i + 1].y, oldrader[j + 1].x, oldrader[j + 1].y, Color.Red, 1);
            }
        }
        #endregion

        if (max != 100) Drawing.Text((int)hexvalue[0].x, (int)hexvalue[0].y, max, Color.Cyan, ReferencePoint.Center);

        string[] tex = ["NOTES", "PEAK", "RHYTHM", "SOFLAN", "GIMMICK", "STREAM"];
        if (size < 60) tex = ["NT", "PK", "RT", "SL", "GM", "ST"];
        double wide = 1.0 + Drawing.TextSize(tex[0]).height / 100.0;
        for (int i = 0; i < 6; i++)
        {
            double textx = hexvalue[0].x + (hexvalue[i + 1].x - hexvalue[0].x) * wide;
            double texty = hexvalue[0].y + (hexvalue[i + 1].y - hexvalue[0].y) * wide;
            int top = t.TopRader();
            Drawing.Text(textx, texty, tex[i],
                t.Enable(i) ? top == i && mark ? Color.Yellow : Color.White : Color.Gray, ReferencePoint.Center);
            /*if (top == i && mark)
            {
                Drawing.Circle(textx - 10, texty + 8, 6, Color.Yellow);
            }*/
        }
    }
}
