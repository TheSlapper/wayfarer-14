using Content.Shared._WF.Nutrition.Spoilage;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._WF.Nutrition.Spoilage;

public sealed class SpoiledFoodVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private static readonly ResPath FliesSprite = new("Objects/Misc/flies.rsi");
    private const string FliesState = "flies";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpoiledFoodComponent, ComponentStartup>(OnSpoiled);
    }

    private void OnSpoiled(EntityUid uid, SpoiledFoodComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.LayerMapReserve((uid, sprite), SpoiledFoodVisualLayers.Flies);
        _sprite.LayerSetRsi((uid, sprite), SpoiledFoodVisualLayers.Flies, FliesSprite);
        _sprite.LayerSetRsiState((uid, sprite), SpoiledFoodVisualLayers.Flies, FliesState);
        _sprite.LayerSetVisible((uid, sprite), SpoiledFoodVisualLayers.Flies, true);
    }
}

public enum SpoiledFoodVisualLayers : byte
{
    Flies
}
