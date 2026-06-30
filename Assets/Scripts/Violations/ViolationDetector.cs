#nullable enable
using System;

public static class ViolationDetector {
    public static bool TryCreateCardPeekingCandidate(
        CardVisibilityContext context,
        ViolationGameMoment moment,
        DateTime occurredAt,
        ViolationActorKind actorKind,
        out ViolationCandidate? candidate) {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        candidate = null;
        if (!context.IsPhysicallyPresent || context.CanViewerLegallySee())
            return false;

        var evidence = new ViolationEvidence(
            new[] {
                ViolationEventSource.CardWorldState,
                ViolationEventSource.CardVisibility,
                ViolationEventSource.PhysicalWorldAction
            },
            moment,
            "Viewer has physical access to card information without a rule-granted visibility right.",
            ViolationObjectKind.Card,
            context.WorldObject,
            context.Card);

        candidate = new ViolationCandidate(
            ViolationType.CardPeeking,
            new ViolationActor(context.Viewer, actorKind),
            occurredAt,
            evidence,
            canBeConfirmedByCurrentEvidence: true);
        return true;
    }

    public static bool TryCreateTheftCandidate(
        TheftContext context,
        ViolationGameMoment moment,
        DateTime occurredAt,
        ViolationActorKind actorKind,
        out ViolationCandidate? candidate) {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        candidate = null;
        if (!context.IsViolation())
            return false;

        var evidence = new ViolationEvidence(
            new[] {
                ViolationEventSource.PickupAction,
                ViolationEventSource.BodyPartAction,
                ViolationEventSource.InventoryAction,
                ViolationEventSource.PhysicalWorldAction
            },
            moment,
            $"Foreign object ownership was violated by theft event '{context.EventKind}'.",
            context.GetObjectKind(),
            context.WorldObject,
            GetTheftDomainObject(context));

        candidate = new ViolationCandidate(
            ViolationType.Theft,
            new ViolationActor(context.Actor, actorKind),
            occurredAt,
            evidence,
            canBeConfirmedByCurrentEvidence: true);
        return true;
    }

    private static object? GetTheftDomainObject(TheftContext context) {
        if (context.StakeAsset != null)
            return context.StakeAsset;
        if (context.BodyPart != null)
            return context.BodyPart;
        if (context.Card != null)
            return context.Card;
        if (context.Item != null)
            return context.Item;

        return null;
    }

    public static ViolationCandidate CreateLeavingSeatCandidate(
        Skeleton offender,
        ViolationGameMoment moment,
        DateTime occurredAt,
        ViolationActorKind actorKind,
        bool isForbiddenByCurrentPhase) {
        var evidence = new ViolationEvidence(
            new[] {
                ViolationEventSource.PlayerSeatState,
                ViolationEventSource.MatchPhase,
                ViolationEventSource.PhysicalWorldAction
            },
            moment,
            "Player left the required place while the current phase forbids leaving.");

        return new ViolationCandidate(
            ViolationType.LeavingSeatDuringRound,
            new ViolationActor(offender, actorKind),
            occurredAt,
            evidence,
            isForbiddenByCurrentPhase);
    }

    public static ViolationCandidate CreateMissedTimedTurnCandidate(
        Skeleton offender,
        ViolationGameMoment moment,
        DateTime occurredAt,
        ViolationActorKind actorKind,
        bool turnTimerExpiredForCurrentPlayer) {
        var evidence = new ViolationEvidence(
            new[] {
                ViolationEventSource.TurnState,
                ViolationEventSource.TurnTimer,
                ViolationEventSource.MatchPhase
            },
            moment,
            "Current turn timer expired before the player completed a required action.");

        return new ViolationCandidate(
            ViolationType.MissedTimedTurn,
            new ViolationActor(offender, actorKind),
            occurredAt,
            evidence,
            turnTimerExpiredForCurrentPlayer);
    }
}