using Robust.Shared.GameStates;

namespace Content.Shared._WF.Nutrition.Spoilage;

/// <summary>
/// Food with this spoils into poison and grows flies after sitting out too long.
/// </summary>
[RegisterComponent]
public sealed partial class FoodSpoilageComponent : Component
{
    [DataField("spoilAfter")]
    public TimeSpan SpoilAfter = TimeSpan.FromHours(24);

    [DataField]
    public TimeSpan Accumulator = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Marks food that has spoiled. Its solution is now poison and it grows flies.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpoiledFoodComponent : Component
{
}