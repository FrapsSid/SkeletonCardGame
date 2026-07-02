#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

public readonly struct ViolationActor {
    public ViolationActor(Skeleton player, ViolationActorKind kind = ViolationActorKind.Unknown) {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Team = player.team;
        Kind = kind;
    }

    public Skeleton Player { get; }
    public Team Team { get; }
    public ViolationActorKind Kind { get; }
}

public sealed class ViolationGameMoment {
    public ViolationGameMoment(
        ViolationMatchPhase phase,
        bool isRoundActive,
        Skeleton? currentTurnPlayer = null,
        int bettingRound = -1,
        string externalPhaseName = "") {
        Phase = phase;
        IsRoundActive = isRoundActive;
        CurrentTurnPlayer = currentTurnPlayer;
        BettingRound = bettingRound;
        ExternalPhaseName = externalPhaseName ?? string.Empty;
    }

    public ViolationMatchPhase Phase { get; }
    public bool IsRoundActive { get; }
    public Skeleton? CurrentTurnPlayer { get; }
    public int BettingRound { get; }
    public string ExternalPhaseName { get; }

    public static ViolationGameMoment OutOfRound { get; } =
        new ViolationGameMoment(ViolationMatchPhase.OutOfRound, false);
}

public sealed class ViolationEvidence {
    public ViolationEvidence(
        IEnumerable<ViolationEventSource> sources,
        ViolationGameMoment moment,
        string reason,
        ViolationObjectKind objectKind = ViolationObjectKind.Unknown,
        Object? worldObject = null,
        object? domainObject = null) {
        Sources = sources?.Distinct().ToList()
                  ?? throw new ArgumentNullException(nameof(sources));
        Moment = moment ?? ViolationGameMoment.OutOfRound;
        Reason = reason ?? string.Empty;
        ObjectKind = objectKind;
        WorldObject = worldObject;
        DomainObject = domainObject;
    }

    public IReadOnlyList<ViolationEventSource> Sources { get; }
    public ViolationGameMoment Moment { get; }
    public string Reason { get; }
    public ViolationObjectKind ObjectKind { get; }
    public Object? WorldObject { get; }
    public object? DomainObject { get; }
}

public sealed class CardVisibilityContext {
    public CardVisibilityContext(
        Skeleton viewer,
        Team? cardOwnerTeam,
        CardWorldZone physicalZone,
        CardVisibilityRelation relation,
        bool isPhysicallyPresent,
        bool isTemporarilyVisible,
        bool hasExplicitVisibilityRight,
        CardData? card = null,
        Object? worldObject = null) {
        Viewer = viewer ?? throw new ArgumentNullException(nameof(viewer));
        CardOwnerTeam = cardOwnerTeam;
        PhysicalZone = physicalZone;
        Relation = relation;
        IsPhysicallyPresent = isPhysicallyPresent;
        IsTemporarilyVisible = isTemporarilyVisible;
        HasExplicitVisibilityRight = hasExplicitVisibilityRight;
        Card = card;
        WorldObject = worldObject;
    }

    public Skeleton Viewer { get; }
    public Team? CardOwnerTeam { get; }
    public CardWorldZone PhysicalZone { get; }
    public CardVisibilityRelation Relation { get; }
    public bool IsPhysicallyPresent { get; }
    public bool IsTemporarilyVisible { get; }
    public bool HasExplicitVisibilityRight { get; }
    public CardData? Card { get; }
    public Object? WorldObject { get; }

    public bool CanViewerLegallySee() {
        if (!IsPhysicallyPresent)
            return false;

        if (HasExplicitVisibilityRight || IsTemporarilyVisible)
            return true;

        return Relation == CardVisibilityRelation.OwnCard
               || Relation == CardVisibilityRelation.AllyCard
               || Relation == CardVisibilityRelation.PublicCard;
    }
}

public sealed class TheftContext {
    public TheftContext(
        Skeleton actor,
        Team? assetOwnerTeam,
        TheftEventKind eventKind,
        bool isAllowedByOwnershipRules,
        StakeAsset? stakeAsset = null,
        BodyPart? bodyPart = null,
        CardData? card = null,
        IItem? item = null,
        Object? worldObject = null) {
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        AssetOwnerTeam = assetOwnerTeam;
        EventKind = eventKind;
        IsAllowedByOwnershipRules = isAllowedByOwnershipRules;
        StakeAsset = stakeAsset;
        BodyPart = bodyPart;
        Card = card;
        Item = item;
        WorldObject = worldObject;
    }

    public Skeleton Actor { get; }
    public Team? AssetOwnerTeam { get; }
    public TheftEventKind EventKind { get; }
    public bool IsAllowedByOwnershipRules { get; }
    public StakeAsset? StakeAsset { get; }
    public BodyPart? BodyPart { get; }
    public CardData? Card { get; }
    public IItem? Item { get; }
    public Object? WorldObject { get; }

    public bool IsViolation() {
        if (AssetOwnerTeam == null || Actor.team == null || AssetOwnerTeam == Actor.team)
            return false;

        if (EventKind == TheftEventKind.PickedUpBodyPartFromFloor && IsAllowedByOwnershipRules)
            return false;

        return EventKind == TheftEventKind.PickedUpOpponentCards
               || EventKind == TheftEventKind.TookBodyPartFromPlayer
               || EventKind == TheftEventKind.StoredForeignAssetInInventory
               || EventKind == TheftEventKind.PickedUpBodyPartFromFloor;
    }

    public ViolationObjectKind GetObjectKind() {
        if (Card != null)
            return ViolationObjectKind.Card;
        if (BodyPart != null)
            return ViolationObjectKind.BodyPart;
        if (StakeAsset != null)
            return ViolationObjectKind.StakeAsset;
        if (Item != null)
            return ViolationObjectKind.InventoryItem;

        return ViolationObjectKind.Unknown;
    }
}