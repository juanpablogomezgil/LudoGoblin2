using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generador de iconos pixel-art temáticos para las habilidades y ataques de LudoGoblin.
/// Genera sprites nítidos en memoria sin dependencias externas.
/// </summary>
public static class AbilityIconFactory
{
    private static readonly Dictionary<string, Sprite> cachedIcons = new Dictionary<string, Sprite>();

    public static Sprite GetIconForAbility(AbilitySO ability)
    {
        if (ability == null) return GetDefaultIcon();

        string name = (ability.abilityName ?? ability.name ?? "").ToLowerInvariant();

        if (name.Contains("escudo") || name.Contains("defensa") || name.Contains("férreo") || name.Contains("ferreo"))
        {
            return GetOrCreateIcon("shield", DrawShield);
        }
        if (name.Contains("fuego") || name.Contains("bola") || name.Contains("flama") || name.Contains("fire"))
        {
            return GetOrCreateIcon("fireball", DrawFireball);
        }
        if (name.Contains("cura") || name.Contains("pocion") || name.Contains("poción") || name.Contains("arcana") || name.Contains("heal"))
        {
            return GetOrCreateIcon("potion", DrawPotion);
        }
        if (name.Contains("veneno") || name.Contains("toxina") || name.Contains("daga") || name.Contains("poison"))
        {
            return GetOrCreateIcon("poison", DrawPoisonDagger);
        }
        if (name.Contains("martillo") || name.Contains("demolición") || name.Contains("demolicion") || name.Contains("hammer"))
        {
            return GetOrCreateIcon("hammer", DrawWarHammer);
        }
        if (name.Contains("doble") || name.Contains("double") || name.Contains("dual"))
        {
            return GetOrCreateIcon("dual_swords", DrawDualSwords);
        }
        if (name.Contains("apuesta") || name.Contains("letal") || name.Contains("dado") || name.Contains("gamble"))
        {
            return GetOrCreateIcon("gamble", DrawGambleDice);
        }

        // Ataque base / espada por defecto
        return GetOrCreateIcon("sword", DrawSword);
    }

    public static Sprite GetDefaultIcon()
    {
        return GetOrCreateIcon("sword", DrawSword);
    }

    private static Sprite GetOrCreateIcon(string key, System.Func<Texture2D> drawFunc)
    {
        if (cachedIcons.TryGetValue(key, out Sprite existing) && existing != null)
        {
            return existing;
        }

        Texture2D tex = drawFunc();
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
        sprite.name = $"Icon_{key}";
        cachedIcons[key] = sprite;
        return sprite;
    }

    // Convierte una matriz de caracteres 16x16 a un Texture2D de 32x32 nítido
    private static Texture2D CreateFromAscii(string[] lines, Dictionary<char, Color> palette)
    {
        int h = lines.Length;
        int w = lines[0].Length;
        int scale = 2; // Escala 16x16 a 32x32

        Texture2D tex = new Texture2D(w * scale, h * scale, TextureFormat.RGBA32, false);

        for (int y = 0; y < h; y++)
        {
            string line = lines[h - 1 - y]; // Invertir Y para coordenadas de textura
            for (int x = 0; x < w; x++)
            {
                char c = (x < line.Length) ? line[x] : '.';
                Color col = palette.ContainsKey(c) ? palette[c] : Color.clear;

                for (int sy = 0; sy < scale; sy++)
                {
                    for (int sx = 0; sx < scale; sx++)
                    {
                        tex.SetPixel(x * scale + sx, y * scale + sy, col);
                    }
                }
            }
        }

        return tex;
    }

    // 1. Espada de Acero (Ataque Base)
    private static Texture2D DrawSword()
    {
        string[] ascii = new string[]
        {
            "................",
            ".......KK.......",
            "......KWWK......",
            ".....KWWSK......",
            "....KWWSK.......",
            "...KWWSK........",
            "..KWWSK.........",
            ".KWWSK..........",
            ".KWDSK..........",
            "..KGGK..........",
            "...KBK..........",
            "....KBK.........",
            ".....KGGK.......",
            "......KK........",
            "................",
            "................"
        };
        var pal = new Dictionary<char, Color>
        {
            { '.', Color.clear },
            { 'K', new Color(0.10f, 0.10f, 0.12f, 1f) },
            { 'W', new Color(0.96f, 0.98f, 1.00f, 1f) },
            { 'S', new Color(0.68f, 0.76f, 0.85f, 1f) },
            { 'D', new Color(0.38f, 0.44f, 0.54f, 1f) },
            { 'G', new Color(0.95f, 0.75f, 0.18f, 1f) },
            { 'B', new Color(0.50f, 0.28f, 0.12f, 1f) }
        };
        return CreateFromAscii(ascii, pal);
    }

    // 2. Escudo Férreo
    private static Texture2D DrawShield()
    {
        string[] ascii = new string[]
        {
            "................",
            "..KKKKKKKKKKKK..",
            "..KGGGGGGGGGGK..",
            "..KGBBBWWBBBGK..",
            "..KGLBBWWBLBGK..",
            "..KGLBBWWBLBGK..",
            "..KGWWWWWWWWGK..",
            "..KGWWWWWWWWGK..",
            "..KGLBBWWBLBGK..",
            "..KGLBBWWBLBGK..",
            "...KGLBWWBLGK...",
            "....KGLWWBLGK...",
            ".....KGLWLGK....",
            "......KGLGK.....",
            ".......KGK......",
            "........K......."
        };
        var pal = new Dictionary<char, Color>
        {
            { '.', Color.clear },
            { 'K', new Color(0.10f, 0.10f, 0.12f, 1f) },
            { 'G', new Color(0.95f, 0.72f, 0.15f, 1f) },
            { 'B', new Color(0.18f, 0.42f, 0.75f, 1f) },
            { 'L', new Color(0.32f, 0.62f, 0.92f, 1f) },
            { 'W', new Color(0.98f, 0.98f, 1.00f, 1f) }
        };
        return CreateFromAscii(ascii, pal);
    }

    // 3. Bola de Fuego
    private static Texture2D DrawFireball()
    {
        string[] ascii = new string[]
        {
            "......KK........",
            ".....KYYK.......",
            "....KYYYYKK.....",
            "...KOYYWYYOK....",
            "..KROYYWWYYORK..",
            "..KROOYYYYOORK..",
            ".KRROOOYOOORRK..",
            ".KRRROOOOOORRK..",
            ".KRRRROOOORRRK..",
            "..KRRRRRRRRRK...",
            "..KRRRRRRRRRK...",
            "...KRRRRRRRK....",
            "....KKRRRKK.....",
            "......KKK.......",
            "................",
            "................"
        };
        var pal = new Dictionary<char, Color>
        {
            { '.', Color.clear },
            { 'K', new Color(0.25f, 0.05f, 0.00f, 1f) },
            { 'R', new Color(0.85f, 0.15f, 0.10f, 1f) },
            { 'O', new Color(0.98f, 0.50f, 0.08f, 1f) },
            { 'Y', new Color(1.00f, 0.85f, 0.20f, 1f) },
            { 'W', new Color(1.00f, 0.98f, 0.85f, 1f) }
        };
        return CreateFromAscii(ascii, pal);
    }

    // 4. Poción de Curación
    private static Texture2D DrawPotion()
    {
        string[] ascii = new string[]
        {
            "......KKKK......",
            "......KCCK......",
            "......KCCK......",
            ".....KKWWKK.....",
            "....KLLGGLLK....",
            "...KLGGGGGGDK...",
            "..KLGGGGGGGGDK..",
            "..KLGGWWGGGGDK..",
            "..KLGGWWGGGGDK..",
            "..KLGGGGGGGGDK..",
            "..KLGGDGGGGGDK..",
            "...KLGGGGGGDK...",
            "....KDDDDDDDK...",
            ".....KKKKKKK....",
            "................",
            "................"
        };
        var pal = new Dictionary<char, Color>
        {
            { '.', Color.clear },
            { 'K', new Color(0.12f, 0.12f, 0.14f, 1f) },
            { 'C', new Color(0.68f, 0.42f, 0.22f, 1f) },
            { 'W', new Color(0.95f, 0.98f, 1.00f, 1f) },
            { 'L', new Color(0.65f, 0.95f, 0.75f, 1f) },
            { 'G', new Color(0.25f, 0.78f, 0.42f, 1f) },
            { 'D', new Color(0.12f, 0.45f, 0.22f, 1f) }
        };
        return CreateFromAscii(ascii, pal);
    }

    // 5. Daga Venenosa
    private static Texture2D DrawPoisonDagger()
    {
        string[] ascii = new string[]
        {
            "...............K",
            "..............KL",
            ".............KPL",
            "............KPPS",
            "...........KPPSK",
            "..........KPPSKK",
            ".........KPPSKG.",
            "...KK...KPPSKG..",
            "...KGBKKPPSK....",
            "....KBGGPSKG....",
            "...KGBKKPSKG....",
            "..KBKK..KPPSKG..",
            ".KK......KPPLK..",
            "..........KPLK..",
            "...........KPK..",
            "............KK.."
        };
        var pal = new Dictionary<char, Color>
        {
            { '.', Color.clear },
            { 'K', new Color(0.08f, 0.12f, 0.08f, 1f) },
            { 'L', new Color(0.65f, 0.98f, 0.45f, 1f) },
            { 'P', new Color(0.25f, 0.85f, 0.18f, 1f) },
            { 'S', new Color(0.35f, 0.45f, 0.40f, 1f) },
            { 'G', new Color(0.18f, 0.45f, 0.15f, 1f) },
            { 'B', new Color(0.35f, 0.22f, 0.10f, 1f) }
        };
        return CreateFromAscii(ascii, pal);
    }

    // 6. Martillo de Demolición
    private static Texture2D DrawWarHammer()
    {
        string[] ascii = new string[]
        {
            ".........KKKKKK.",
            "........KLLLLSDK",
            ".......KLLLLSSDK",
            ".......KLGGGGSDK",
            ".......KLSSSSDDK",
            "..KK...KLSSSSDDK",
            ".KGGKK..KDDDDDDK",
            ".KHHKK...KKKKKK.",
            "..KHHK..........",
            "...KHHK.........",
            "....KHHK........",
            ".....KHHK.......",
            "......KHHK......",
            ".......KHHK.....",
            "........KK......",
            "................"
        };
        var pal = new Dictionary<char, Color>
        {
            { '.', Color.clear },
            { 'K', new Color(0.10f, 0.10f, 0.12f, 1f) },
            { 'L', new Color(0.85f, 0.88f, 0.92f, 1f) },
            { 'S', new Color(0.55f, 0.60f, 0.68f, 1f) },
            { 'D', new Color(0.28f, 0.32f, 0.38f, 1f) },
            { 'G', new Color(0.92f, 0.72f, 0.15f, 1f) },
            { 'H', new Color(0.48f, 0.25f, 0.10f, 1f) }
        };
        return CreateFromAscii(ascii, pal);
    }

    // 7. Dados / Apuesta Letal
    private static Texture2D DrawGambleDice()
    {
        string[] ascii = new string[]
        {
            "...KKKKKKK......",
            "..KYWYYYYYK.....",
            ".KYWRRYYYYYK....",
            "KYWWRRYYYYYDK...",
            "KYYYYYYYYYYDK...",
            "KYYYYYYYYYYDK...",
            ".KDDDDDDDDDK....",
            "...KKKKKKK.KKKK.",
            "...KWWWWWK.KRWK.",
            "...KWWWWWK.KRWK.",
            "...KWRRRWK.KRWK.",
            "...KWWWWWK.KRWK.",
            "...KWWWWWK.KRWK.",
            "...KKKKKKK.KKKK.",
            "................",
            "................"
        };
        var pal = new Dictionary<char, Color>
        {
            { '.', Color.clear },
            { 'K', new Color(0.12f, 0.10f, 0.05f, 1f) },
            { 'W', new Color(0.98f, 0.98f, 0.95f, 1f) },
            { 'Y', new Color(0.98f, 0.82f, 0.20f, 1f) },
            { 'R', new Color(0.85f, 0.15f, 0.15f, 1f) },
            { 'D', new Color(0.70f, 0.45f, 0.10f, 1f) }
        };
        return CreateFromAscii(ascii, pal);
    }

    // 8. Doble Espada
    private static Texture2D DrawDualSwords()
    {
        string[] ascii = new string[]
        {
            "KWWK.......KWWK.",
            ".KWWSK...KWWSK..",
            "..KWWSK.KWWSK...",
            "...KWWSKWWSK....",
            "....KWWSSWK.....",
            ".....KGGGGK.....",
            "....KBKKKBKK....",
            "...KBK...KBK....",
            "..KGGK...KGGK...",
            "...KK.....KK....",
            "................",
            "................",
            "................",
            "................",
            "................",
            "................"
        };
        var pal = new Dictionary<char, Color>
        {
            { '.', Color.clear },
            { 'K', new Color(0.10f, 0.10f, 0.12f, 1f) },
            { 'W', new Color(0.96f, 0.98f, 1.00f, 1f) },
            { 'S', new Color(0.68f, 0.76f, 0.85f, 1f) },
            { 'G', new Color(0.95f, 0.75f, 0.18f, 1f) },
            { 'B', new Color(0.50f, 0.28f, 0.12f, 1f) }
        };
        return CreateFromAscii(ascii, pal);
    }
}
