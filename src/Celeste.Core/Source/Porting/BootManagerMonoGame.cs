using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Celeste.Porting;

/// <summary>
/// Boot manager com tela de erro/diagnóstico renderizada no MonoGame.
/// Ele é chamado a partir de Monocle.Engine.Update/Draw quando está bloqueando o game loop.
/// </summary>
public sealed class BootManagerMonoGame : IBootManager
{
    private enum State
    {
        Running,
        ContentMissing,
        Fatal
    }

    private State state;
    private readonly bool requireAudio;

    private PixelFont font;
    private SpriteBatch sb;
    private GraphicsDevice gd;

    private string fatalMessage;
	private Exception? fatalEx;

    private double holdDiagTimer;
    private bool diagMode;

    public List<string> Problems { get; } = new();
    public string Summary { get; private set; } = "N/A";

    public bool IsBlockingGameLoop => state != State.Running;
    public IReadOnlyList<string> LastValidationProblems => Problems;
    public string LastValidationSummary => Summary;

    public BootManagerMonoGame(bool requireAudio)
    {
        this.requireAudio = requireAudio;
        state = State.ContentMissing; // será revalidado assim que Engine estiver ativo
    }

	public void EnterFatal(string message, Exception? ex = null)
    {
        fatalMessage = message ?? "ERRO FATAL";
        fatalEx = ex;
        state = State.Fatal;
        PortServices.Error("CELESTE/BOOT", $"FATAL: {fatalMessage}");
        if (fatalEx != null)
            PortServices.Exception("CELESTE/BOOT", fatalEx, "FATAL");
    }

    public bool TryRevalidate()
    {
        try
        {
            Problems.Clear();
            var res = ContentValidator.Validate(PortServices.Paths, PortServices.FileSystem, requireAudio);
            Summary = res.Summary;
            Problems.AddRange(res.Problems);

            if (res.Ok)
            {
                state = State.Running;
                PortServices.Info("CELESTE/BOOT", "CONTENT_OK_AFTER_RETRY");
                return true;
            }

            state = State.ContentMissing;
            PortServices.Warn("CELESTE/BOOT", "CONTENT_STILL_INVALID_AFTER_RETRY");
            return false;
        }
        catch (Exception ex)
        {
            EnterFatal("Falha ao validar Content", ex);
            return false;
        }
    }

    public void Update(GameTime time)
    {
        if (Engine.Instance == null)
            return;

        // Lazy-init: só quando GraphicsDevice existir.
        if (gd == null && Engine.Instance.GraphicsDevice != null)
            gd = Engine.Instance.GraphicsDevice;

        // Ativa modo diagnóstico por hold de START/ENTER.
        bool holdKey = IsHoldDiag();
        if (holdKey)
            holdDiagTimer += time.ElapsedGameTime.TotalSeconds;
        else
            holdDiagTimer = 0;

        if (holdDiagTimer >= 2.0)
        {
            holdDiagTimer = 0;
            diagMode = !diagMode;
            PortServices.Info("CELESTE/BOOT", $"DIAG_MODE={(diagMode ? "ON" : "OFF")}");
        }

        // A primeira validação deve ocorrer assim que o input estiver pronto.
        if (state == State.ContentMissing && Summary == "N/A")
            TryRevalidate();

        if (state == State.ContentMissing)
        {
            if (PressedRetry())
                TryRevalidate();

            if (PressedExit())
            {
                PortServices.Warn("CELESTE/BOOT", "USER_EXIT_FROM_ERROR_SCREEN");
                Engine.Instance.Exit();
            }
        }
        else if (state == State.Fatal)
        {
            if (PressedExit())
            {
                PortServices.Warn("CELESTE/BOOT", "USER_EXIT_FROM_FATAL_SCREEN");
                Engine.Instance.Exit();
            }
        }
    }

    public void Draw(GameTime time)
    {
        if (Engine.Instance == null || Engine.Instance.GraphicsDevice == null)
            return;

        if (gd == null)
            gd = Engine.Instance.GraphicsDevice;

        if (sb == null)
            sb = new SpriteBatch(gd);

        if (font == null)
            font = new PixelFont(gd);

        gd.Clear(Color.Black);

        sb.Begin(samplerState: SamplerState.PointClamp);

        if (state == State.ContentMissing)
            DrawContentMissing();
        else
            DrawFatal();

        if (diagMode)
            DrawDiagnosticsOverlay();

        sb.End();
    }

    private void DrawContentMissing()
    {
        var contentPath = PortServices.Paths?.ContentPath ?? "<ContentPath>";
        var header = "ARQUIVOS DO JOGO NAO ENCONTRADOS";

        // Raw string interpolada (C# 11+), evita CS1010/CS1039.
        var body = $"""
PARA INICIAR, COPIE OS ARQUIVOS DO JOGO PARA:
{contentPath}

FALTANDO/PROBLEMAS:
""";

        var footer = """
DEPOIS DE COPIAR, FECHE E ABRA O APP NOVAMENTE.
SEM TOUCH: USE GAMEPAD/TECLADO.
START/ENTER: TENTAR NOVAMENTE | BACK/ESC: SAIR
""";

        DrawBoxedText(header, new Vector2(18, 18), Color.White, 3);
        DrawBoxedText(body, new Vector2(18, 60), Color.White, 2);

        float y = 140;
        int shown = 0;
        foreach (var p in Problems)
        {
            if (shown >= 10) { font.DrawString(sb, "...", new Vector2(18, y), Color.Yellow, 2); break; }
            font.DrawString(sb, "- " + p, new Vector2(18, y), Color.Yellow, 2);
            y += 20;
            shown++;
        }

        font.DrawString(sb, footer, new Vector2(18, Math.Max(y + 10, 360)), Color.White, 2);
    }

    private void DrawFatal()
    {
        var header = "ERRO FATAL";
        var msg = fatalMessage ?? "ERRO FATAL";
        var detail = fatalEx != null ? (fatalEx.GetType().Name + ": " + fatalEx.Message) : "";

        var footer = """
BACK/ESC: SAIR
""";

        DrawBoxedText(header, new Vector2(18, 18), Color.White, 3);
        DrawBoxedText(msg, new Vector2(18, 60), Color.Yellow, 2);
        if (!string.IsNullOrEmpty(detail))
            DrawBoxedText(detail, new Vector2(18, 90), Color.Yellow, 2);

        font.DrawString(sb, footer, new Vector2(18, 430), Color.White, 2);
    }

    private void DrawDiagnosticsOverlay()
    {
        var p = PortServices.Paths;

        // Também convertido para raw string interpolada.
        var lines = $"""
DIAGNOSTICO
BASE: {p?.BaseDataPath}
CONTENT: {p?.ContentPath}
LOGS: {p?.LogsPath}
SAVE: {p?.SavePath}
CONTENT_STATUS: {Summary}
PROBLEMS: {Problems.Count}
HOLD START/ENTER 2s PARA TOGGLE
""";

        // Fundo simples translúcido
        sb.Draw(GetOverlayPixel(), new Rectangle(10, 10, 700, 210), Color.Black * 0.65f);
        font.DrawString(sb, lines, new Vector2(18, 18), Color.Lime, 2);
    }

    private Texture2D overlayPixel;
    private Texture2D GetOverlayPixel()
    {
        if (overlayPixel != null)
            return overlayPixel;

        overlayPixel = new Texture2D(gd, 1, 1);
        overlayPixel.SetData(new[] { Color.White });
        return overlayPixel;
    }

    private void DrawBoxedText(string text, Vector2 pos, Color color, int scale)
    {
        // Caixa mínima apenas para separar o texto do fundo
        var size = font.Measure(text, scale);
        sb.Draw(GetOverlayPixel(), new Rectangle((int)pos.X - 6, (int)pos.Y - 6, (int)size.X + 12, (int)size.Y + 12), Color.Black * 0.55f);
        font.DrawString(sb, text, pos, color, scale);
    }

    private static bool PressedRetry()
    {
        if (MInput.Keyboard.Pressed(Keys.Enter) || MInput.Keyboard.Pressed(Keys.Space))
            return true;

        for (int i = 0; i < 4; i++)
        {
            var pad = MInput.GamePads[i];
            if (pad.Pressed(Buttons.Start) || pad.Pressed(Buttons.A))
                return true;
        }

        return false;
    }

    private static bool PressedExit()
    {
        if (MInput.Keyboard.Pressed(Keys.Escape))
            return true;

        for (int i = 0; i < 4; i++)
        {
            var pad = MInput.GamePads[i];
            if (pad.Pressed(Buttons.Back) || pad.Pressed(Buttons.B))
                return true;
        }

        return false;
    }

    private static bool IsHoldDiag()
    {
        if (MInput.Keyboard.Check(Keys.Enter) || MInput.Keyboard.Check(Keys.Space))
            return true;

        for (int i = 0; i < 4; i++)
        {
            var pad = MInput.GamePads[i];
            if (pad.Check(Buttons.Start))
                return true;
        }
        return false;
    }
}
