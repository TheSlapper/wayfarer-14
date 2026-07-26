using Content.Shared._WF.Equipment.Systems;
using Content.Shared.Whitelist;

namespace Content.Shared._WF.Equipment.Components;

/// <summary>
/// Blocks the listed actions on this item for anyone holding it who does not match the whitelist below.
/// </summary>
[RegisterComponent]
[Access(typeof(EquipmentLockSystem))]
public sealed partial class EquipmentLockComponent : Component
{
    [DataField]
    public EquipmentLockActions Locks;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public LocId Popup = "equipment-lock-use-fail";

    public TimeSpan LastPopup;

    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(1);
}

[Flags]
public enum EquipmentLockActions : byte
{
    None = 0,
    Fire = 1 << 0,
    Melee = 1 << 1,
    Wear = 1 << 2,
    OpenUi = 1 << 3,
}
