using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Celeste.Editor;

/// <summary>
/// Placeholder implementation for the in-game Map Editor.
///
/// The original Celeste codebase had editor tooling and a map editor scene.
/// This port focuses on runtime/playability. To keep debug hooks (Tab / console command)
/// compiling and behaving deterministically across platforms, we ship a minimal scene that:
///   - Clearly communicates that the editor is not bundled;
///   - Allows returning safely to the previous scene.
///
/// If you later decide to actually ship an editor, you can replace this file with the real
/// implementation and keep the same public surface.
/// </summary>
public sealed class MapEditor : Scene
{
	/// <summary>Scene to return to when closing the editor.</summary>
	public static Scene? ReturnScene { get; set; }

	private readonly AreaKey area;
	private readonly string message;

	public MapEditor()
		: this(AreaKey.Default)
	{
	}

	public MapEditor(AreaKey area)
	{
		this.area = area;
		message = $"MAP EDITOR (stub)\n\n" +
			"Esta build nao inclui o editor de mapas.\n" +
			"Isso mantem os ganchos de debug compilando sem travar o jogo.\n\n" +
			$"Area: {area.ID}  Mode: {area.Mode}\n\n" +
			"Para voltar: ESC / Clique / Botao Back.";
	}

	public override void Update()
	{
		base.Update();

		// Saidas multiplataforma (teclado, mouse e gamepad).
		if (MInput.Keyboard.Pressed(Keys.Escape) || MInput.Mouse.PressedLeftButton)
		{
			Exit();
			return;
		}

		// Em alguns targets mobile, o "Back" mapeia para Buttons.Back.
		for (int i = 0; i < MInput.GamePads.Length; i++)
		{
			var pad = MInput.GamePads[i];
			if (pad.Attached && (pad.Pressed(Buttons.Back) || pad.Pressed(Buttons.B)))
			{
				Exit();
				return;
			}
		}
	}

	public override void Render()
	{
		base.Render();

		// Fundo semitransparente para legibilidade.
		Draw.Rect(0, 0, 320, 180, Color.Black * 0.85f);

		// Fonte do jogo (se disponivel). Se nao estiver inicializada ainda, cai para nada.
		if (ActiveFont.Font != null)
		{
			ActiveFont.DrawOutline(message, new Vector2(16f, 16f), Vector2.Zero, Vector2.One, Color.White, 2f, Color.Black);
		}
	}

	private static void Exit()
	{
		// Volta para a cena anterior, quando existente; caso contrario, volta para o Overworld.
		var target = ReturnScene;
		ReturnScene = null;
		Engine.Scene = target ?? new OverworldLoader(Overworld.StartMode.MainMenu);
	}
}
