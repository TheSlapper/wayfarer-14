using Content.Shared._WF.Equipment.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._WF.Equipment.Systems;

public sealed class EquipmentLockSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipmentLockComponent, ShotAttemptedEvent>(OnShootAttempt);
        SubscribeLocalEvent<EquipmentLockComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<EquipmentLockComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
        SubscribeLocalEvent<EquipmentLockComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
    }

    private void OnShootAttempt(EntityUid uid, EquipmentLockComponent comp, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled || !comp.Locks.HasFlag(EquipmentLockActions.Fire) || !Denied(comp, args.User))
            return;

        args.Cancel();
        ShowPopup(uid, comp, args.User);
    }

    private void OnMeleeAttempt(EntityUid uid, EquipmentLockComponent comp, ref AttemptMeleeEvent args)
    {
        // The melee attempt never fills in its attacker, so whoever is holding the weapon counts as the attacker.
        if (args.Cancelled || !comp.Locks.HasFlag(EquipmentLockActions.Melee) || !TryGetHolder(uid, out var holder) || !Denied(comp, holder))
            return;

        args.Cancelled = true;
        if (PopupAllowed(comp))
            args.Message = Loc.GetString(comp.Popup);
    }

    private void OnUiOpenAttempt(EntityUid uid, EquipmentLockComponent comp, ActivatableUIOpenAttemptEvent args)
    {
        // No popup here. This same event is raised while building the right-click menu, so a message would appear on menu open.
        if (args.Cancelled || !comp.Locks.HasFlag(EquipmentLockActions.OpenUi) || !Denied(comp, args.User))
            return;

        args.Cancel();
    }

    private void OnEquipAttempt(EntityUid uid, EquipmentLockComponent comp, BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled || !comp.Locks.HasFlag(EquipmentLockActions.Wear) || !Denied(comp, args.EquipTarget))
            return;

        args.Cancel();
        args.Reason = comp.Popup;
    }

    private bool Denied(EquipmentLockComponent comp, EntityUid holder)
        => _whitelist.IsWhitelistFail(comp.Whitelist, holder);

    private bool TryGetHolder(EntityUid item, out EntityUid holder)
    {
        if (_container.TryGetContainingContainer((item, null, null), out var container))
        {
            holder = container.Owner;
            return true;
        }

        holder = default;
        return false;
    }

    private void ShowPopup(EntityUid uid, EquipmentLockComponent comp, EntityUid holder)
    {
        if (PopupAllowed(comp))
            _popup.PopupClient(Loc.GetString(comp.Popup), uid, holder);
    }

    private bool PopupAllowed(EquipmentLockComponent comp)
    {
        // Without this the block often shows no message at all.
        if (!_timing.IsFirstTimePredicted)
            return false;

        var time = _timing.CurTime;
        if (time < comp.LastPopup + comp.PopupCooldown)
            return false;

        comp.LastPopup = time;
        return true;
    }
}
