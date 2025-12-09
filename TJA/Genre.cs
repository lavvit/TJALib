namespace TJALib.TJA;

public enum EGenre
{
    None = 0,
    JPOP,
    Anime,
    Kids,

    Variety,
    niconico,
    Artist,
    SpiCatsArtist,

    Classic,

    Vocaloid,

    GameMusic,
    BEMANI,
    BMS,
    SEGA,
    Smartphone,
    WACCA,
    Other,
    World,
    Touhou,

    NamcoOriginal,

    Spica,
    TaikoCats,

    All,
}
public class Genre
{
    public static string TopGenre(string[] genres)
    {
        var genre = EGenre.None;
        string hitgenre = "";
        foreach (string g in genres)
        {
            var eg = Genre.Name(g);
            if (eg > genre)
            {
                genre = eg;
                hitgenre = g;
            }
        }
        return hitgenre;
    }
    public static string GetName(string[] genres, string baseGenre = "")
    {
        if (string.IsNullOrEmpty(baseGenre) || genres == null || genres.Length == 0)
        {
            return TopGenre(genres ?? []);
        }
        List<EGenre> names = [];
        foreach (string genre in genres)
        {
            var g = Name(genre);
            if (g != EGenre.None)
            {
                names.Add(g);
            }
        }
        var basegen = Name(baseGenre);
        EGenre[] uppergenre = [
            EGenre.Spica,
            EGenre.TaikoCats,
            EGenre.NamcoOriginal,
            ];
        switch (basegen)
        {
            case EGenre.JPOP:
                {
                    EGenre[] upper = [
                    EGenre.Anime,
                    EGenre.Vocaloid,
                    EGenre.Kids];
                    uppergenre = [.. uppergenre, .. upper];
                }
                break;
            case EGenre.Anime:
                {
                    EGenre[] upper = [
                    EGenre.Kids];
                    uppergenre = [.. uppergenre, .. upper];
                }
                break;
            case EGenre.Variety:
                {
                    EGenre[] upper = [
                    EGenre.Artist,
                    EGenre.niconico,
                    EGenre.BMS,
                    EGenre.Touhou,
                    EGenre.SpiCatsArtist];
                    uppergenre = [.. uppergenre, .. upper];
                }
                break;
            case EGenre.GameMusic:
                {
                    EGenre[] upper = [
                    EGenre.Vocaloid,
                    EGenre.Touhou,
                    EGenre.Classic,

                    EGenre.BEMANI,
                    EGenre.Smartphone,
                    EGenre.SEGA,
                    EGenre.WACCA,
                    EGenre.Other,
                    EGenre.World];
                    uppergenre = [.. uppergenre, .. upper];
                }
                break;
        }
        // 優先度高いジャンルがあればそちらを返す
        foreach (var g in uppergenre)
        {
            if (names.Contains(g))
            {
                return genres.FirstOrDefault(x => Name(x) == g) ?? Name(g);
            }
        }
        // 優先度高いジャンルがなければベースジャンルに該当するものを返す
        return names.Contains(basegen) ? baseGenre : genres[0];
    }
    public static string Name(EGenre type) => type switch
    {
        EGenre.JPOP => "J-POP",
        EGenre.Anime => "Anime",
        EGenre.Vocaloid => "Vocaloid",
        EGenre.Kids => "Child",
        EGenre.Variety => "Variety",
        EGenre.Classic => "Classic",
        EGenre.GameMusic => "Game",
        EGenre.NamcoOriginal => "Namco",
        EGenre.Touhou => "Touhou",
        EGenre.niconico => "niconico",
        EGenre.BEMANI => "BEMANI",
        EGenre.BMS => "BMS",
        EGenre.SEGA => "TYUUMAI",
        EGenre.WACCA => "WACCA",
        EGenre.Smartphone => "SmartPhone",
        EGenre.World => "World",
        EGenre.Other => "Other",
        EGenre.Artist => "Artist",
        EGenre.TaikoCats => "TCST",
        EGenre.Spica => "Spica",
        EGenre.SpiCatsArtist => "SCST",
        _ => "",
    };
    public static EGenre Name(string genre)
    {
        if (string.IsNullOrWhiteSpace(genre)) return EGenre.None;

        string g = genre.Trim().ToUpperInvariant();

        return g switch
        {
            "J-POP" or "ポップス" => EGenre.JPOP,
            "ANIME" or "アニメ" => EGenre.Anime,
            "GAME" or "ゲームミュージック" => EGenre.GameMusic,
            "NAMCO" or "ナムコオリジナル" => EGenre.NamcoOriginal,
            "CLASSIC" or "クラシック" => EGenre.Classic,
            "CHILD" or "どうよう" or "キッズ" => EGenre.Kids,
            "VARIETY" or "バラエティ" => EGenre.Variety,
            "VOCALOID" or "ボーカロイド" or "ボーカロイド曲" or "ＶＯＣＡＬＯＩＤ" => EGenre.Vocaloid,
            "BEMANI" => EGenre.BEMANI,
            "BMS" => EGenre.BMS,
            "TAIKOCATSSOUNDTEAM" or "TAIKOCATSSOUND" or "TAIKOCATS" or "TCST" => EGenre.TaikoCats,
            "ARTIST" or "アーティストオリジナル" => EGenre.Artist,
            "SMARTPHONE" or "スマホ音ゲー" => EGenre.Smartphone,
            "TYUUMAI" or "ゲキチュウマイ" => EGenre.SEGA,
            "WACCA" => EGenre.WACCA,
            "TOUHOU" or "東方" => EGenre.Touhou,
            "スピカオリジナル" or "SPICAオリジナル" or "SPICA" => EGenre.Spica,
            "NICONICO" => EGenre.niconico,
            "WORLD" or "海外音ゲー" => EGenre.World,
            "OTHER" or "その他音ゲー" => EGenre.Other,
            "SCST" => EGenre.SpiCatsArtist,
            _ => EGenre.None,
        };
    }

    public static int Number(EGenre genre) => genre switch
    {
        EGenre.JPOP => 1,
        EGenre.Anime => 2,
        EGenre.Vocaloid => 8,
        EGenre.Kids => 7,
        EGenre.Variety => 4,
        EGenre.Classic => 6,
        EGenre.GameMusic => 3,
        EGenre.NamcoOriginal => 5,
        _ => 0,
    };

    public static string Color(EGenre genre) => genre switch
    {
        EGenre.JPOP => "#21a1ba",
        EGenre.Anime => "#ff9900",
        EGenre.Vocaloid => "#abb4bf",
        EGenre.Kids => "#ff5386",
        EGenre.Variety => "#8fd41f",
        EGenre.Classic => "#d1a314",
        EGenre.GameMusic => "#9d76bf",
        EGenre.NamcoOriginal => "#ff5b14",
        EGenre.BEMANI => "#4818b0",
        EGenre.BMS => "#303030",
        EGenre.TaikoCats => "#60c080",
        EGenre.Artist => "#c04860",
        EGenre.Smartphone => "#0060a0",
        EGenre.SEGA => "#60c0b0",
        EGenre.WACCA => "#ff0060",
        EGenre.Touhou => "#903080",
        EGenre.Spica => "#206050",
        EGenre.niconico => "#7f7f7f",
        EGenre.World => "#5be9dc",
        EGenre.Other => "#ffdd00",
        EGenre.SpiCatsArtist => "#77eecc",
        _ => "#a9a9a9",
    };
    public static string Color(string genre) => Color(Name(genre));

    public static EGenre[] FromSubtitle(string subtitle)
    {
        if (subtitle == null) return [];
        List<EGenre> genres = [];
        // J-POP ポップス
        // アニメ
        // ゲームミュージック
        // ナムコオリジナル
        {
            string[] names = [
                "太鼓の達人",
                "BNSI",
                "WADIVE RECORD",
                "太鼓 de タイムトラベル",
            ];
            foreach (string vocalo in names)
            {
                if (subtitle.Contains(vocalo, StringComparison.OrdinalIgnoreCase))
                {
                    genres.Add(EGenre.NamcoOriginal);
                    break;
                }
            }
        }
        // クラシック
        // どうよう キッズ
        {
            string[] kids = [
                "カラフルピーチ",
                "戦隊",
                "仮面ライダー",
                "しなこ",
                "竹下☆ぱらだいす",
                "メルちゃん",
                "ワンピース",
                "ONE PIECE",
                "マッシュル",
                "ポケットモンスター",
                "ちいかわ",
                "ふしぎ駄菓子屋 銭天堂",
                "鬼滅の刃",
                "アナと雪の女王",
                "はねまり",
                "スーパーマリオ",
                "となりのトトロ",
                "千と千尋の神隠し",
                "天空の城ラピュタ",
                "星のカービィ",
                "HIMAWARI",
                "HIKAKIN",
                "スプラトゥーン",
                "ちびまる子ちゃん",
                "クレヨンしんちゃん",
                "ケロポンズ",
                "妖怪ウォッチ",
                "妖怪学園Y",
                "くまモン",
                "ウルトラマン",
                "ニンジャボックス",
                "ドラゴンボール",
                "ブットバースト",
                "たまごっち",
                "カミズモード！",
                "さかなクン",
                "アニマルカイザー",
                "コロコロコミック",

                "プリキュア",
                "ポケモン",
                "ドラえもん",
                "アンパンマン",
                "きかんしゃトーマス",
                "しまじろう",
                "いないいないばあっ！",
                "おジャ魔女どれみ",
                "デジモン",
                "デジタルモンスター",
                "デュエル・マスターズ",
                "プリパラ",
                "アイカツ！",

                "おかあさんといっしょ",
                "みんなのうた",
                "AGHARTA",
                "ハピクラワールド",
                "おでんくん",
                "ミュークルドリーミー",
                "ピタゴラスイッチ",
                "みいつけた！",
                "マナル隊",
                "リリーボンボンズ",
                "かいけつゾロリ",
                "忍たま乱太郎",
                "ミミカ",

            ];
            foreach (string kid in kids)
            {
                if (subtitle.Contains(kid, StringComparison.OrdinalIgnoreCase))
                {
                    genres.Add(EGenre.Kids);
                    break;
                }
            }
        }
        // バラエティ
        // ボーカロイド ボーカロイド曲 VOCALOID
        {
            string[] vocalos = [
                "初音ミク",
                "鏡音リン",
                "鏡音レン",
                "巡音ルカ",
                "MEIKO",
                "KAITO",
                "IA",
                "結月ゆかり",
                "神威がくぽ",
                "GUMI",
                "CUL",
                "Lily",
                "SeeU",
                "結月ゆかり",
                "ONE",
                "YUZU-P"
            ];
            // feat.の後ろにボカロ名がある場合もあるのでそちらも考慮
            string featPrefix = "feat.";
            string feat = subtitle.Contains(featPrefix, StringComparison.OrdinalIgnoreCase)
                ? subtitle[(subtitle.IndexOf(featPrefix, StringComparison.OrdinalIgnoreCase) + featPrefix.Length)..]
                : "";
            foreach (string vocalo in vocalos)
            {
                if (feat.Contains(vocalo, StringComparison.OrdinalIgnoreCase))
                {
                    genres.Add(EGenre.Vocaloid);
                    break;
                }
            }
        }
        // BEMANI
        {
            string[] games = [
                "BEMANI",
                "beatmania IIDX",
                "SOUND VOLTEX",
                "DanceDanceRevolution",
                "Dance Dance Revolution",
                "pop'n music",
                "GITADORA",
                "jubeat",
                "ノスタルジア",
                "DANCE aROUND",
                "DANCERUSH",
                "REFLEC BEAT",
                "BeatStream",
                "MUSECA",
                "ミライダガッキ",
                "DanceEvolution",
                "DrumMania",
                "GuitarFreaks",
                "beatmania",
                "KEYBOARDMANIA"
            ];
            foreach (string game in games)
            {
                if (subtitle.Contains(game, StringComparison.OrdinalIgnoreCase))
                {
                    genres.Add(EGenre.BEMANI);
                    break;
                }
            }
        }
        // BMS
        // TaikoCatsSoundTeam TaikoCatsSound TaikoCats
        // アーティストオリジナル
        // スマホ音ゲー
        {
            string[] games = [
                "プロジェクトセカイ",
                "バンドリ",
                "BanG Dream!",
                "ラブライブ",
                "アイドルマスター",
                "Arcaea",
                "Cytus",
                "DEEMO",
                "Dynamix",
                "Lanota",
                "VOEZ",
                "Phigros",
                "Tone Sphere",
                "D4DJ",
                "TAKUMI³",
                "Rotaeno",
                "Muse Dash"// steam版もあるけどどっち？
            ];
            foreach (string game in games)
            {
                if (subtitle.Contains(game, StringComparison.OrdinalIgnoreCase))
                {
                    genres.Add(EGenre.Smartphone);
                    break;
                }
            }
        }
        // ゲキチュウマイ
        {
            string[] games = [
                "CHUNITHM",
                "maimai",
                "オンゲキ"
                ];
            foreach (string game in games)
            {
                if (subtitle.Contains(game, StringComparison.OrdinalIgnoreCase))
                {
                    genres.Add(EGenre.SEGA);
                    break;
                }
            }
        }
        // WACCA
        {
            if (subtitle.Contains("WACCA", StringComparison.OrdinalIgnoreCase))
            {
                genres.Add(EGenre.WACCA);
            }
        }
        // 東方
        {
            string[] touhou = [
                "東方project",
                "東方靈異伝",
                "東方封魔録",
                "東方夢時空",
                "東方幻想郷",
                "東方怪綺談",
                "東方紅魔郷",
                "東方妖々夢",
                "東方永夜抄",
                "東方花映塚",
                "東方風神録",
                "東方地霊殿",
                "東方星蓮船",
                "東方神霊廟",
                "東方輝針城",
                "東方紺珠伝",
                "東方天空璋",
                "東方鬼形獣",
                "東方ダンマクカグラ",
                "ZUN"
                ];
            foreach (string th in touhou)
            {
                if (subtitle.Contains(th, StringComparison.OrdinalIgnoreCase))
                {
                    genres.Add(EGenre.Touhou);
                    break;
                }
            }
        }
        // スピカオリジナル Spicaオリジナル Spica
        // niconico
        // 海外音ゲー
        {
            string[] games = [
                "Pump It Up",
                "In the Groove",
                "StepMania",
                "O2Jam",
                "Freestyle",
                "S4 League",
                "Audition Online",
                "Sound Space",
                "eXceed3rd",
                "DJMAX"
                ];
            foreach (string game in games)
            {
                if (subtitle.Contains(game, StringComparison.OrdinalIgnoreCase))
                {
                    genres.Add(EGenre.World);
                    break;
                }
            }
        }
        // その他音ゲー
        {
            string[] games = [
                "Groove Coaster",
                "グルーヴコースター",
                "CROSS×BEATS",
                "リズム天国",
                "太鼓のオワタツジン",
                ];
            foreach (string game in games)
            {
                if (subtitle.Contains(game, StringComparison.OrdinalIgnoreCase))
                {
                    genres.Add(EGenre.Other);
                    break;
                }
            }
        }
        // 段位道場
        // 段位-薄木 段位-濃木 段位-黒 段位-赤 段位-銀 段位-金 段位-外伝
        // Another
        // ExCats

        // SpiCatsArtist
        {
            string[] artists = [
                // 両方
                "北見恋雪",
                "DJ SHION.Y",
                "WABI",
                "マタタビ",

                // Spica
                "Cataphythm",
                "なぞのひと",
                "こいさな",
                "Alto Tabidori",
                "Prodigium",
                "Lilly",
                "Nk (Nekoribo)",
                "Center garden",
                "Rapidsystem",
                "おむにん",
                "せんせん",
                "侵略者",
                "G_key",
                "Reo2",
                "100V",
                "白羽",
                "風神",
                "花風",
                "降下二号",

                // TaikoCats
                "くりーむ",
                "Cream",
                "もこだて",
                "kuro",
                "ラヴィエ",
                "黒皇帝",
                "EDLOC",
                "独リ神",
                "SLUDGE MAXX",
                "DJ ARTHUR",
                ];
            string[] ignore = [
                "Jun Kuroda",
                ];
            foreach (string artist in artists)
            {
                if (subtitle.Contains(artist, StringComparison.OrdinalIgnoreCase))
                {
                    if (ignore.Any(i => subtitle.Contains(i, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    genres.Add(EGenre.SpiCatsArtist);
                    break;
                }
            }
        }

        return [.. genres];
    }
}
