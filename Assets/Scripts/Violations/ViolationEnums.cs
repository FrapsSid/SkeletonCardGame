public enum ViolationEventSource {
    AbstractGameState,
    MatchPhase,
    TurnState,
    TurnTimer,
    PlayerSeatState,
    CardWorldState,
    CardVisibility,
    PickupAction,
    BodyPartAction,
    InventoryAction,
    PhysicalWorldAction
}

public enum ViolationType {
    CardPeeking,
    LeavingSeatDuringRound,
    MissedTimedTurn,
    Theft
}

public enum ViolationConfirmationRule {
    ManualJudgeReview,
    VisibilityMismatch,
    OwnershipMismatch,
    PhaseAndTurnState,
    ImmediateWorldAction
}

public enum ViolationConfirmationStatus {
    Pending,
    Confirmed,
    Rejected,
    Expired,
    SameTeamComplaint,
    CooldownActive,
    NoActiveViolation,
    OutsideComplaintWindow
}

public enum ViolationActorKind {
    Unknown,
    Human,
    AI
}

public enum ViolationMatchPhase {
    OutOfRound,
    Discussion,
    ActiveRound,
    ActiveTurn,
    EndRound
}

public enum ViolationObjectKind {
    Unknown,
    Card,
    BodyPart,
    InventoryItem,
    StakeAsset
}

public enum CardWorldZone {
    Unknown,
    Deck,
    PlayerHand,
    AllyHand,
    OpponentHand,
    Table,
    World,
    Inventory,
    Discard
}

public enum CardVisibilityRelation {
    OwnCard,
    AllyCard,
    OpponentCard,
    PublicCard
}

public enum TheftEventKind {
    PickedUpOpponentCards,
    TookBodyPartFromPlayer,
    StoredForeignAssetInInventory,
    PickedUpBodyPartFromFloor
}
