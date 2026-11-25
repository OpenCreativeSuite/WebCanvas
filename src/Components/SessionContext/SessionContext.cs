using Donkeh.UILab;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Components;

/// <summary>Publishes the session to every workspace component.</summary>
public static class SessionContext
{
    public static readonly Context<StudioSession> Value = new(null!);
}
