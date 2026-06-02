using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Robust.Shared.Containers;

namespace Content.Server._Starlight.Nutrition;

/// <summary>
///     Handles flavor text for any smokable object's duration
/// </summary>
public sealed class SmokableExamineSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const string PipeBowlSlot = "bowl_slot";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SmokableComponent, ExaminedEvent>(OnSmokableExamined);
    }

    private void OnSmokableExamined(Entity<SmokableComponent> entity, ref ExaminedEvent args)
    {
        var smokable = entity.Comp;

        // Have to handle burnt above empty and seperate from unlit, because pipes are reusable and go directly from empty to unlit.
        if (smokable.State == SmokableState.Burnt)
        {
            args.PushMarkup(Loc.GetString("smokable-examine-burnt"));
            return;
        }

        if (smokable.InhaleAmount <= FixedPoint2.Zero
            || !_solutionContainerSystem.TryGetSolution(entity.Owner, smokable.Solution, out _, out var solution))
            return;

        if (smokable.State == SmokableState.Unlit)
        {
            // Count a packed-but-unlit pipe (item sitting in the bowl slot) as having something to smoke.
            var hasContents = solution.Volume > FixedPoint2.Zero
                || (_container.TryGetContainer(entity, PipeBowlSlot, out var bowl) && bowl.Count > 0);

            args.PushMarkup(Loc.GetString(hasContents
                ? "smokable-examine-unlit"
                : "smokable-examine-empty"));
            return;
        }

        var secondsLeft = solution.Volume.Float() / smokable.InhaleAmount.Float();

        var key = secondsLeft switch
        {
            > 60f => "smokable-examine-lit-full",
            > 30f => "smokable-examine-lit-half",
            _ => "smokable-examine-lit-low",
        };

        args.PushMarkup(Loc.GetString(key));
    }
}
