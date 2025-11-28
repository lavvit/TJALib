using AstrumLoom;

using TJALib.TJA;

namespace TJALib.Draw;

public class Note
{
    public static int Size = 64;

    private static Dictionary<string, Texture> NoteTexture = [];

    public static Action<double, double, ENote, int, double, int, int, int>? CustomDrawNote;
    public static void Draw(double x, double y, ENote type = ENote.None,
        int facetype = 0, double opacity = 1, int r = 0, int g = 0, int b = 0)
    {
        if (type is ENote.None or ENote.End) return;
        if (CustomDrawNote != null)
        {
            CustomDrawNote(x, y, type, facetype, opacity, r, g, b);
            return;
        }

        int size = (int)(Size / 2 * (GetSize(type) > 0 ? 0.8 : 0.5333));
        var color = NoteColor(type);

        string key = $"{type}_{facetype}_{size}_{color}" +
        $"_{r}_{g}_{b}";

        double preset = size + 1;
        if (!NoteTexture.TryGetValue(key, out var value))
        {
            value = Drawing.MakeTexture(() =>
            {
                Drawing.Circle(preset, preset, size, Color.Black, opacity: opacity);
                Drawing.Circle(preset, preset, size / 1.1, Color.White, opacity: opacity);
                Drawing.Circle(preset, preset, size / 1.25, color, opacity: opacity);
                Drawing.Circle(preset, preset, size / 1.25, Color.FromRGB(r < 256 ? r : 255, g < 256 ? g : 255, b < 256 ? b : 255), opacity: opacity, blend: BlendMode.Add);
            });
            NoteTexture[key] = value;
        }
        value.Draw(x - preset, y - preset);
    }

    public static Action<double, double, double, double, ENote, int, double, int, int, int>? CustomDrawLong;
    public static void Long(double x, double y, double endx, double endy, ENote type = ENote.None,
        int state = 0, double opacity = 1, int r = 0, int g = 0, int b = 0)
    {
        if (type is ENote.None or ENote.End) return;
        if (CustomDrawLong != null)
        {
            CustomDrawLong(x, y, endx, endy, type, state, opacity, r, g, b);
            return;
        }
        int size = (int)(Size / 2 * (GetSize(type) > 0 ? 0.8 : 0.5333));
        var basecolor = Color.Black;
        var color = NoteColor(type);
        if (state < 0) // miss
        {
            basecolor = Color.FromRGB(
                (int)(basecolor.R * 0.5),
                (int)(basecolor.G * 0.5),
                (int)(basecolor.B * 0.5));
            color = Color.FromARGB(color.A,
                (int)(color.R * 0.75),
                (int)(color.G * 0.75),
                (int)(color.B * 0.75));
        }
        if (state > 0) // push
        {
            bool ka = type is ENote.Ka or ENote.KA;
            //basecolor = Color.Red;
            r = !ka ? 255 : 32;
            g = 32;
            b = ka ? 255 : 32;
        }
        Drawing.Circle(endx, endy, size, basecolor, opacity: opacity);
        Drawing.BoxZ(x, y - size, endx, endy + size + 1, basecolor, opacity: opacity);
        Drawing.Circle(endx, endy, size / 1.1, Color.White, opacity: opacity);
        Drawing.BoxZ(x, y - size / 1.1, endx, endy + size / 1.1 + 1, Color.White, opacity: opacity);

        Drawing.Circle(endx, endy, size / 1.25, color, opacity: opacity);
        Drawing.BoxZ(x, y - size / 1.25, endx, endy + size / 1.25 + 1, color, opacity: opacity);
        Drawing.Circle(endx, endy, size, Color.FromRGB(r < 256 ? r : 255, g < 256 ? g : 255, b < 256 ? b : 255), opacity: opacity, blend: BlendMode.Add);
        Drawing.BoxZ(x, y - size, endx, endy + size + 1, Color.FromRGB(r < 256 ? r : 255, g < 256 ? g : 255, b < 256 ? b : 255), opacity: opacity, blend: BlendMode.Add);

    }
    public static Action<double, double, ENote, double, double, int, int, int>? CustomDrawBalloon;
    public static void Balloon(double x, double y, ENote type = ENote.None,
        double state = 0, double opacity = 1, int r = 0, int g = 0, int b = 0)
    {
        if (type is ENote.None or ENote.End) return;
        if (CustomDrawBalloon != null)
        {
            CustomDrawBalloon(x, y, type, state, opacity, r, g, b);
            return;
        }
        if (state > 0.0)
        {
            r = (int)(255 * state);
            g = r / 2;
        }
        int size = (int)(Size / 2 * (GetSize(type) > 0 ? 0.8 : 0.5333));
        Long(x, y, x + size * (1.0 + state), y, type, 0, opacity, r, g, b);
    }

    private static int GetSize(ENote type) => type switch
    {
        ENote.DON or ENote.KA or ENote.ROLL or ENote.Potato => 1,
        _ => 0,
    };

    public static Func<ENote, Color>? CustomNoteColor { get; set; } = null;
    public static Color NoteColor(ENote type) => CustomNoteColor != null
            ? CustomNoteColor(type)
            : type switch
            {
                ENote.Don or ENote.DON => Color.Red,
                ENote.Ka or ENote.KA => Color.Cyan,
                ENote.Roll or ENote.ROLL => Color.Yellow,
                ENote.Balloon => Color.OrangeRed,
                ENote.Potato => Color.Orange,
                _ => Color.Gray,
            };
}
