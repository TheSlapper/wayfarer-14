using System.Collections.Generic;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Nutrition;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.GameTicking;
using Content.Shared.Nutrition.Components;
using Content.Shared._WF.Nutrition.Spoilage;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._WF.Nutrition.Spoilage;

/// <summary>
/// Spoils food that has sat out too long, turning its solution to poison.
/// </summary>
public sealed class FoodSpoilageSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private static readonly ProtoId<ReagentPrototype> ToxinReagent = "GastroToxin";

    // Food sorted by when its next check is due. Only the soonest is looked at each tick.
    private readonly PriorityQueue<EntityUid, TimeSpan> _schedule = new();
    // Which food is currently in the queue, so nothing gets queued twice.
    private readonly HashSet<EntityUid> _scheduled = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodSpoilageComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SpoiledFoodComponent, ComponentStartup>(OnSpoiled);
        SubscribeLocalEvent<SpoiledFoodComponent, FoodSlicedEvent>(OnSpoiledSliced);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
    }

    private void OnStartup(EntityUid uid, FoodSpoilageComponent component, ComponentStartup args)
    {
        // Stagger the first check so food spawned together doesn't all check on the same tick.
        var jitter = TimeSpan.FromSeconds(_random.NextFloat(0f, (float)component.UpdateRate.TotalSeconds));
        Reschedule(uid, _timing.CurTime + jitter);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        while (_schedule.TryPeek(out _, out var due) && due <= now)
        {
            var uid = _schedule.Dequeue();
            _scheduled.Remove(uid);

            if (!TryComp<FoodSpoilageComponent>(uid, out var comp))
                continue;

            if (Paused(uid))
            {
                Reschedule(uid, now + comp.UpdateRate);
                continue;
            }

            if (!IsPreserved(uid) && !IsCold(uid))
            {
                comp.Accumulator += comp.UpdateRate;

                if (!HasComp<SpoiledFoodComponent>(uid))
                {
                    if (comp.Accumulator >= comp.SpoilAfter)
                        AddComp<SpoiledFoodComponent>(uid);
                }
                else if (TryComp<RotIntoComponent>(uid, out var rotInto)
                         && comp.Accumulator >= comp.SpoilAfter * (rotInto.Stage + 1))
                {
                    Spawn(rotInto.Entity, Transform(uid).Coordinates);
                    QueueDel(uid);
                    continue;
                }
            }

            if (HasComp<FoodSpoilageComponent>(uid))
                Reschedule(uid, now + comp.UpdateRate);
        }
    }

    private void OnSpoiled(EntityUid uid, SpoiledFoodComponent component, ComponentStartup args)
    {
        // Keep raw meat's timer running so it can later turn into rotten meat.
        if (!HasComp<RotIntoComponent>(uid))
            RemComp<FoodSpoilageComponent>(uid);

        if (!TryComp<EdibleComponent>(uid, out var edible))
            return;

        if (!_solutionContainer.TryGetSolution(uid, edible.Solution, out var soln, out var solution))
            return;

        var removed = _solutionContainer.SplitSolution(soln.Value, solution.Volume);
        _solutionContainer.TryAddReagent(soln.Value, ToxinReagent, removed.Volume);
    }

    private void OnSpoiledSliced(EntityUid uid, SpoiledFoodComponent component, ref FoodSlicedEvent args)
    {
        EnsureComp<SpoiledFoodComponent>(args.Slice);
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        _schedule.Clear();
        _scheduled.Clear();
    }

    private void Reschedule(EntityUid uid, TimeSpan time)
    {
        if (_scheduled.Add(uid))
            _schedule.Enqueue(uid, time);
    }

    private bool IsPreserved(EntityUid uid)
    {
        return _container.TryGetContainingContainer((uid, null, null), out var container)
            && HasComp<AntiRottingContainerComponent>(container.Owner);
    }

    private bool IsCold(EntityUid uid)
    {
        var air = _atmosphere.GetContainingMixture((uid, Transform(uid)));
        return air != null && air.Temperature < Atmospherics.T0C + 0.85f;
    }
}
