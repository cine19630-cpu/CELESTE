using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Porting;

public interface IBootManager
{
    bool IsBlockingGameLoop { get; }

    IReadOnlyList<string> LastValidationProblems { get; }
    string LastValidationSummary { get; }

    // Chamado no Update/Draw do Engine quando IsBlockingGameLoop = true.
    void Update(GameTime time);
    void Draw(GameTime time);

    // Revalidar Content (ex.: START/ENTER).
    bool TryRevalidate();

    // Forçar entrada em modo erro fatal com mensagem.
    void EnterFatal(string message, Exception ex = null);
}
