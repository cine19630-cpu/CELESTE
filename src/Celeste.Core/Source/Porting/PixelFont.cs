using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Porting;

/// <summary>
/// Fonte pixel ultra simples (monoespaçada) para telas de erro/diagnóstico.
/// Não depende de MGCB/SpriteFont nem de assets externos.
/// Renderiza apenas um subset ASCII (texto é normalizado para remover acentos e forçado para upper).
/// </summary>
public sealed class PixelFont
{
    private readonly Texture2D pixel;
    private readonly Dictionary<char, byte[]> glyphs;

    public int GlyphW { get; }
    public int GlyphH { get; }

    public PixelFont(GraphicsDevice gd)
    {
        pixel = new Texture2D(gd, 1, 1, false, SurfaceFormat.Color);
        pixel.SetData(new[] { Color.White });

        GlyphW = 5;
        GlyphH = 7;
        glyphs = BuildGlyphs();
    }

    public static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);

        foreach (var c in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    public Vector2 Measure(string text, int scale = 2)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        text = Normalize(text);
        var lines = text.Split('\n');
        int max = 0;
        foreach (var l in lines) max = Math.Max(max, l.Length);
        return new Vector2(max * (GlyphW + 1) * scale, lines.Length * (GlyphH + 2) * scale);
    }

    public void DrawString(SpriteBatch sb, string text, Vector2 pos, Color color, int scale = 2)
    {
        if (string.IsNullOrEmpty(text))
            return;

        text = Normalize(text);
        float x0 = pos.X;
        float x = x0;
        float y = pos.Y;

        foreach (var ch in text)
        {
            if (ch == '\r') continue;

            if (ch == '\n')
            {
                x = x0;
                y += (GlyphH + 2) * scale;
                continue;
            }

            DrawChar(sb, ch, new Vector2(x, y), color, scale);
            x += (GlyphW + 1) * scale;
        }
    }

    private void DrawChar(SpriteBatch sb, char ch, Vector2 pos, Color color, int scale)
    {
        if (!glyphs.TryGetValue(ch, out var rows))
            rows = glyphs['?'];

        for (int row = 0; row < GlyphH; row++)
        {
            byte bits = rows[row];
            for (int col = 0; col < GlyphW; col++)
            {
                if (((bits >> (GlyphW - 1 - col)) & 1) != 0)
                {
                    sb.Draw(pixel, new Rectangle((int)pos.X + col * scale, (int)pos.Y + row * scale, scale, scale), color);
                }
            }
        }
    }

    private static Dictionary<char, byte[]> BuildGlyphs()
    {
        // Cada glyph é 7 linhas de 5 bits (MSB -> esquerda). Ex.: 0b01110.
        var g = new Dictionary<char, byte[]>();

        void Add(char c, params string[] rows)
        {
            var arr = new byte[7];
            for (int i = 0; i < 7; i++)
            {
                string r = rows[i];
                byte b = 0;
                for (int j = 0; j < 5; j++)
                {
                    b <<= 1;
                    b |= (byte)(r[j] == '1' ? 1 : 0);
                }
                arr[i] = b;
            }
            g[c] = arr;
        }

        Add(' ', "00000","00000","00000","00000","00000","00000","00000");
        Add('?', "01110","10001","00010","00100","00100","00000","00100");
        Add('!', "00100","00100","00100","00100","00100","00000","00100");
        Add('.', "00000","00000","00000","00000","00000","00110","00110");
        Add(':', "00000","00110","00110","00000","00110","00110","00000");
        Add('-', "00000","00000","00000","01110","00000","00000","00000");
        Add('_', "00000","00000","00000","00000","00000","00000","11111");
        Add('/', "00001","00010","00100","01000","10000","00000","00000");
        Add('\\',"10000","01000","00100","00010","00001","00000","00000");
        Add('[', "01110","01000","01000","01000","01000","01000","01110");
        Add(']', "01110","00010","00010","00010","00010","00010","01110");
        Add('(', "00010","00100","01000","01000","01000","00100","00010");
        Add(')', "01000","00100","00010","00010","00010","00100","01000");
        Add('#', "01010","11111","01010","01010","11111","01010","01010");
        Add('+', "00000","00100","00100","11111","00100","00100","00000");
        Add('=', "00000","11111","00000","11111","00000","00000","00000");

        // Dígitos
        Add('0',"01110","10001","10011","10101","11001","10001","01110");
        Add('1',"00100","01100","00100","00100","00100","00100","01110");
        Add('2',"01110","10001","00001","00010","00100","01000","11111");
        Add('3',"11110","00001","00001","01110","00001","00001","11110");
        Add('4',"00010","00110","01010","10010","11111","00010","00010");
        Add('5',"11111","10000","10000","11110","00001","00001","11110");
        Add('6',"01110","10000","10000","11110","10001","10001","01110");
        Add('7',"11111","00001","00010","00100","01000","01000","01000");
        Add('8',"01110","10001","10001","01110","10001","10001","01110");
        Add('9',"01110","10001","10001","01111","00001","00001","01110");

        // Letras A-Z (subset suficiente)
        Add('A',"01110","10001","10001","11111","10001","10001","10001");
        Add('B',"11110","10001","10001","11110","10001","10001","11110");
        Add('C',"01110","10001","10000","10000","10000","10001","01110");
        Add('D',"11100","10010","10001","10001","10001","10010","11100");
        Add('E',"11111","10000","10000","11110","10000","10000","11111");
        Add('F',"11111","10000","10000","11110","10000","10000","10000");
        Add('G',"01110","10001","10000","10111","10001","10001","01111");
        Add('H',"10001","10001","10001","11111","10001","10001","10001");
        Add('I',"01110","00100","00100","00100","00100","00100","01110");
        Add('J',"00111","00010","00010","00010","00010","10010","01100");
        Add('K',"10001","10010","10100","11000","10100","10010","10001");
        Add('L',"10000","10000","10000","10000","10000","10000","11111");
        Add('M',"10001","11011","10101","10101","10001","10001","10001");
        Add('N',"10001","11001","10101","10011","10001","10001","10001");
        Add('O',"01110","10001","10001","10001","10001","10001","01110");
        Add('P',"11110","10001","10001","11110","10000","10000","10000");
        Add('Q',"01110","10001","10001","10001","10101","10010","01101");
        Add('R',"11110","10001","10001","11110","10100","10010","10001");
        Add('S',"01111","10000","10000","01110","00001","00001","11110");
        Add('T',"11111","00100","00100","00100","00100","00100","00100");
        Add('U',"10001","10001","10001","10001","10001","10001","01110");
        Add('V',"10001","10001","10001","10001","10001","01010","00100");
        Add('W',"10001","10001","10001","10101","10101","10101","01010");
        Add('X',"10001","10001","01010","00100","01010","10001","10001");
        Add('Y',"10001","10001","01010","00100","00100","00100","00100");
        Add('Z',"11111","00001","00010","00100","01000","10000","11111");

        // Extras comuns
        Add('%',"11001","11010","00100","01000","10110","00110","00000");
        Add(',',"00000","00000","00000","00000","00110","00110","01100");
        Add(';',"00000","00110","00110","00000","00110","00110","01100");

        return g;
    }
}
