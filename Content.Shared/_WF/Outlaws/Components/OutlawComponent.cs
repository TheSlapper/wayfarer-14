using Robust.Shared.GameStates;

namespace Content.Shared._WF.Outlaws.Components;

/// <summary>
/// Marks a character as an outlaw. Granted automatically with the role.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OutlawComponent : Component
{
}
