#nullable enable
using System;

namespace Interactions
{
    public record Interaction(
        string Source,
        string Text,
        Action<InteractionType> Callback,
        bool AllowMouseButtonInteraction = true);
}