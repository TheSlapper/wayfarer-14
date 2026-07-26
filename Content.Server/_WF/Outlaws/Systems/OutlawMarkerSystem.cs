using Content.Server._NF.Roles.Components;
using Content.Shared._WF.Outlaws.Components;
using Content.Shared.Cloning.Events;
using Content.Shared.Mind;
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
        SubscribeLocalEvent<OutlawComponent, CloningEvent>(OnCloned);
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        if (args.Mind.OwnedEntity is { } body && _roles.MindHasRole<NFPirateRoleComponent>(args.MindId))
            EnsureComp<OutlawComponent>(body);
    }

    private void OnCloned(Entity<OutlawComponent> ent, ref CloningEvent args)
    {
        EnsureComp<OutlawComponent>(args.CloneUid);
    }
}
