#nullable enable
using System;

public sealed class ViolationCandidate {
    public ViolationCandidate(
        ViolationType type,
        ViolationActor offender,
        DateTime occurredAt,
        ViolationEvidence evidence,
        bool canBeConfirmedByCurrentEvidence) {
        Id = Guid.NewGuid();
        Type = type;
        Offender = offender;
        OccurredAt = occurredAt;
        Evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
        CanBeConfirmedByCurrentEvidence = canBeConfirmedByCurrentEvidence;
    }

    public Guid Id { get; }
    public ViolationType Type { get; }
    public ViolationActor Offender { get; }
    public DateTime OccurredAt { get; }
    public ViolationEvidence Evidence { get; }
    public bool CanBeConfirmedByCurrentEvidence { get; }
}

public sealed class ViolationRecord {
    public ViolationRecord(
        ViolationCandidate candidate,
        ViolationRule rule,
        DateTime confirmedAt,
        Team? reportingTeam) {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        ConfirmedAt = confirmedAt;
        ReportingTeam = reportingTeam;
    }

    public ViolationCandidate Candidate { get; }
    public Guid Id => Candidate.Id;
    public ViolationType Type => Candidate.Type;
    public ViolationActor Offender => Candidate.Offender;
    public DateTime OccurredAt => Candidate.OccurredAt;
    public DateTime ConfirmedAt { get; }
    public DateTime ExpiresAt => Rule.GetExpirationTime(OccurredAt);
    public ViolationRule Rule { get; }
    public ViolationEvidence Evidence => Candidate.Evidence;
    public Team? ReportingTeam { get; }

    public bool IsActive(DateTime now) {
        return Rule.IsActive(OccurredAt, now);
    }
}

public sealed class ViolationComplaintResult {
    public ViolationComplaintResult(
        ViolationConfirmationStatus status,
        ViolationType type,
        Team reportingTeam,
        Skeleton accusedPlayer,
        DateTime submittedAt,
        DateTime? cooldownUntil,
        ViolationRecord? confirmedViolation) {
        Status = status;
        Type = type;
        ReportingTeam = reportingTeam;
        AccusedPlayer = accusedPlayer;
        SubmittedAt = submittedAt;
        CooldownUntil = cooldownUntil;
        ConfirmedViolation = confirmedViolation;
    }

    public ViolationConfirmationStatus Status { get; }
    public ViolationType Type { get; }
    public Team ReportingTeam { get; }
    public Skeleton AccusedPlayer { get; }
    public DateTime SubmittedAt { get; }
    public DateTime? CooldownUntil { get; }
    public ViolationRecord? ConfirmedViolation { get; }
    public bool IsSuccessful => Status == ViolationConfirmationStatus.Confirmed;
}