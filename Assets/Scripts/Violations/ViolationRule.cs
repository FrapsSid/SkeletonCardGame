#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class ViolationRule {
    private readonly List<ViolationConfirmationRule> confirmationRules;

    public ViolationRule(ViolationType type,
        TimeSpan activeDuration, TimeSpan failedComplaintCooldown,
        IEnumerable<ViolationConfirmationRule> confirmationRules,
        IEnumerable<ViolationEventSource> sources,
        bool canHappenDuringRound, bool canHappenOutsideRound, string description) {
        if (activeDuration <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(activeDuration));
        }

        if (failedComplaintCooldown < TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(failedComplaintCooldown));
        }

        Type = type;
        ActiveDuration = activeDuration;
        FailedComplaintCooldown = failedComplaintCooldown;
        this.confirmationRules = confirmationRules?.Distinct().ToList()
                                 ?? throw new ArgumentNullException(nameof(confirmationRules));
        Sources = sources?.Distinct().ToList()
                  ?? throw new ArgumentNullException(nameof(sources));
        CanHappenDuringRound = canHappenDuringRound;
        CanHappenOutsideRound = canHappenOutsideRound;
        Description = description ?? string.Empty;
    }

    public ViolationType Type { get; }
    public TimeSpan ActiveDuration { get; }
    public TimeSpan FailedComplaintCooldown { get; }
    public IReadOnlyList<ViolationConfirmationRule> ConfirmationRules => confirmationRules;
    public IReadOnlyList<ViolationEventSource> Sources { get; }
    public bool CanHappenDuringRound { get; }
    public bool CanHappenOutsideRound { get; }
    public string Description { get; }

    public DateTime GetExpirationTime(DateTime occurredAt) {
        return occurredAt + ActiveDuration;
    }

    public bool IsActive(DateTime occurredAt, DateTime now) {
        return now <= GetExpirationTime(occurredAt);
    }
}

public static class ViolationRulesDict {
    public static IReadOnlyDictionary<ViolationType, ViolationRule> CreateDefault() {
        return new Dictionary<ViolationType, ViolationRule> {
            [ViolationType.CardPeeking] = new(ViolationType.CardPeeking,
                TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30), new[] {
                    ViolationConfirmationRule.VisibilityMismatch,
                    ViolationConfirmationRule.ManualJudgeReview
                },
                new[] {
                    ViolationEventSource.CardWorldState,
                    ViolationEventSource.CardVisibility,
                    ViolationEventSource.PhysicalWorldAction
                },
                canHappenDuringRound: true,
                canHappenOutsideRound: false,
                description: "A player can physically see cards, but rules don't allow it"),

            [ViolationType.LeavingSeatDuringRound] = new(ViolationType.LeavingSeatDuringRound,
                TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30),
                new[] {
                    ViolationConfirmationRule.PhaseAndTurnState,
                    ViolationConfirmationRule.ManualJudgeReview
                },
                new[] {
                    ViolationEventSource.PlayerSeatState,
                    ViolationEventSource.MatchPhase,
                    ViolationEventSource.PhysicalWorldAction
                },
                canHappenDuringRound: true,
                canHappenOutsideRound: false,
                description: "A player leaves required place, but a round phase forbids it"),

            [ViolationType.MissedTimedTurn] = new(ViolationType.MissedTimedTurn,
                TimeSpan.FromSeconds(45),
                TimeSpan.FromSeconds(30),
                new[] {
                    ViolationConfirmationRule.PhaseAndTurnState,
                    ViolationConfirmationRule.ImmediateWorldAction
                },
                new[] {
                    ViolationEventSource.TurnState,
                    ViolationEventSource.TurnTimer,
                    ViolationEventSource.MatchPhase
                },
                canHappenDuringRound: true,
                canHappenOutsideRound: false,
                description: "The player does not complete a required turn before the allowed time ends"),

            [ViolationType.Theft] = new(ViolationType.Theft,
                TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(45), new[] {
                    ViolationConfirmationRule.OwnershipMismatch,
                    ViolationConfirmationRule.ImmediateWorldAction
                },
                new[] {
                    ViolationEventSource.PickupAction,
                    ViolationEventSource.BodyPartAction,
                    ViolationEventSource.InventoryAction,
                    ViolationEventSource.PhysicalWorldAction
                },
                canHappenDuringRound: true,
                canHappenOutsideRound: true,
                description: "A player moves cards, body parts, or items that belong to another side without rule permission")
        };
    }
}