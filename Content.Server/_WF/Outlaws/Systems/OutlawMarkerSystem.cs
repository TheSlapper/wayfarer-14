using Content.Server._NF.Roles.Components;
using Content.Shared._WF.Outlaws.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;

namespace Content.Server._WF.Outlaws.Systems;

/// <summary>
/// Marks a player's character as an outlaw when they get the role.
/// </summary>
public sealed class OutlawMarkerSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<MindComponent, MindGotAddedEvent>(OnMindGotAdded);
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        if (args.Mind.OwnedEntity is { } body)
            Apply(body, args.MindId);
    }

    private void OnMindGotAdded(EntityUid mindId, MindComponent comp, MindGotAddedEvent args)
    {
        Apply(args.Container, mindId);
    }

    private void Apply(EntityUid body, EntityUid mindId)
    {
        if (HasComp<GhostComponent>(body))
            return;

        if (_roles.MindHasRole<NFPirateRoleComponent>(mindId))
            EnsureComp<OutlawComponent>(body);
    }
}
