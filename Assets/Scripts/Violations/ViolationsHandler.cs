#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class ViolationsHandler {
    private readonly Dictionary<ViolationType, ViolationRule> rules;
    private readonly Dictionary<ViolationKey, ViolationCandidate> latestCandidates = new Dictionary<ViolationKey, ViolationCandidate>();
    private readonly Dictionary<ViolationKey, ViolationRecord> latestConfirmed = new Dictionary<ViolationKey, ViolationRecord>();
    private readonly List<ViolationCandidate> candidateHistory = new List<ViolationCandidate>();
    private readonly List<ViolationRecord> confirmedHistory = new List<ViolationRecord>();

    public ViolationsHandler(IEnumerable<Team>? matchTeams = null)
        : this(ViolationRulesDict.CreateDefault(), matchTeams) {
    }

    public ViolationsHandler(IReadOnlyDictionary<ViolationType, ViolationRule> rules,
        IEnumerable<Team>? matchTeams = null) {
        if (rules == null)
            throw new ArgumentNullException(nameof(rules));

        this.rules = rules.ToDictionary(pair => pair.Key, pair => pair.Value);
        if (matchTeams != null)
            WarningLog.RegisterTeams(matchTeams);
    }

    public IReadOnlyDictionary<ViolationType, ViolationRule> Rules => rules;
    public TeamWarningLog WarningLog { get; } = new TeamWarningLog();
    public ViolationComplaintLog ComplaintLog { get; } = new ViolationComplaintLog();
    public IReadOnlyList<ViolationCandidate> CandidateHistory => candidateHistory;
    public IReadOnlyList<ViolationRecord> ConfirmedHistory => confirmedHistory;

    public event Action<ViolationCandidate>? OnCandidateRecorded;
    public event Action<ViolationRecord, TeamWarningResult>? OnViolationConfirmed;
    public event Action<ViolationComplaintResult>? OnComplaintResolved;

    public void RegisterTeams(IEnumerable<Team> teams) {
        WarningLog.RegisterTeams(teams);
    }

    public ViolationCandidate RecordCandidate(ViolationCandidate candidate) {
        if (candidate == null)
            throw new ArgumentNullException(nameof(candidate));

        EnsureKnownRule(candidate.Type);
        candidateHistory.Add(candidate);
        latestCandidates[new ViolationKey(candidate.Offender.Player, candidate.Type)] = candidate;
        OnCandidateRecorded?.Invoke(candidate);
        return candidate;
    }

    public ViolationRecord RecordConfirmedViolation(ViolationCandidate candidate, Team? reportingTeam = null,
        DateTime? confirmedAt = null) {
        RecordCandidate(candidate);
        return ConfirmCandidate(candidate, reportingTeam, confirmedAt ?? DateTime.UtcNow);
    }

    public ViolationComplaintResult SubmitComplaint(
        Team reportingTeam,
        Skeleton accusedPlayer,
        ViolationType type,
        DateTime? submittedAt = null) {
        if (reportingTeam == null)
            throw new ArgumentNullException(nameof(reportingTeam));
        if (accusedPlayer == null)
            throw new ArgumentNullException(nameof(accusedPlayer));

        ViolationRule rule = EnsureKnownRule(type);
        DateTime now = submittedAt ?? DateTime.UtcNow;

        if (accusedPlayer.team == reportingTeam) {
            var sameTeamResult = new ViolationComplaintResult(
                ViolationConfirmationStatus.SameTeamComplaint,
                type,
                reportingTeam,
                accusedPlayer,
                now,
                cooldownUntil: null,
                confirmedViolation: null);
            OnComplaintResolved?.Invoke(sameTeamResult);
            return sameTeamResult;
        }

        if (ComplaintLog.IsOnCooldown(reportingTeam, type, now, out DateTime activeCooldownUntil)) {
            var cooldownResult = new ViolationComplaintResult(
                ViolationConfirmationStatus.CooldownActive,
                type,
                reportingTeam,
                accusedPlayer,
                now,
                activeCooldownUntil,
                confirmedViolation: null);
            OnComplaintResolved?.Invoke(cooldownResult);
            return cooldownResult;
        }

        ViolationCandidate? latest = GetLatestActiveCandidate(accusedPlayer, type, now);
        
        if (latest == null) {
            return FailComplaint(reportingTeam, accusedPlayer, type, now, rule, ViolationConfirmationStatus.NoActiveViolation);
        }

        if (!rule.IsActive(latest.OccurredAt, now)) {
            return FailComplaint(reportingTeam, accusedPlayer, type, now, rule, ViolationConfirmationStatus.OutsideComplaintWindow);
        }

        if (!CanConfirmInMoment(rule, latest.Evidence.Moment)) {
            return FailComplaint(reportingTeam, accusedPlayer, type, now, rule, ViolationConfirmationStatus.PhaseNotAllowed);
        }

        if (!latest.CanBeConfirmedByCurrentEvidence) {
            return FailComplaint(reportingTeam, accusedPlayer, type, now, rule, ViolationConfirmationStatus.Rejected);
        }

        var record = ConfirmCandidate(latest, reportingTeam, now);
        var confirmedResult = new ViolationComplaintResult(
            ViolationConfirmationStatus.Confirmed,
            type, reportingTeam, accusedPlayer, now,
            cooldownUntil: null, confirmedViolation: record);
        
        OnComplaintResolved?.Invoke(confirmedResult);
        
        return confirmedResult;
    }

    public ViolationCandidate? GetLatestActiveCandidate(Skeleton offender, ViolationType type, DateTime now) {
        if (offender == null)
            return null;

        var key = new ViolationKey(offender, type);
        if (!latestCandidates.TryGetValue(key, out ViolationCandidate candidate))
            return null;

        ViolationRule rule = EnsureKnownRule(type);
        return rule.IsActive(candidate.OccurredAt, now) ? candidate : null;
    }

    public ViolationRecord? GetLatestActiveConfirmedViolation(Skeleton offender, ViolationType type, DateTime now) {
        if (offender == null)
            return null;

        var key = new ViolationKey(offender, type);
        if (!latestConfirmed.TryGetValue(key, out ViolationRecord record))
            return null;

        return record.IsActive(now) ? record : null;
    }

    public IReadOnlyDictionary<ViolationType, ViolationRecord> GetActiveConfirmedViolationsByType(Skeleton offender,
        DateTime now) {
        var result = new Dictionary<ViolationType, ViolationRecord>();
        if (offender == null)
            return result;

        foreach (ViolationType type in rules.Keys) {
            ViolationRecord? record = GetLatestActiveConfirmedViolation(offender, type, now);
            if (record != null)
                result[type] = record;
        }

        return result;
    }

    public void PruneExpired(DateTime now) {
        RemoveExpired(latestCandidates, now, candidate => {
            ViolationRule rule = EnsureKnownRule(candidate.Type);
            return rule.IsActive(candidate.OccurredAt, now);
        });

        RemoveExpired(latestConfirmed, now, record => record.IsActive(now));
        ComplaintLog.ClearExpired(now);
    }

    private ViolationRecord ConfirmCandidate(ViolationCandidate candidate, Team? reportingTeam, DateTime confirmedAt) {
        ViolationRule rule = EnsureKnownRule(candidate.Type);
        if (!CanConfirmInMoment(rule, candidate.Evidence.Moment)) {
            throw new InvalidOperationException(
                $"Violation '{candidate.Type}' cannot be confirmed during moment '{candidate.Evidence.Moment.Phase}'.");
        }
        
        var key = new ViolationKey(candidate.Offender.Player, candidate.Type);
        
        if (latestConfirmed.TryGetValue(key, out ViolationRecord existingRecord)
            && existingRecord.Id == candidate.Id) {
            return existingRecord;
        }

        var record = new ViolationRecord(candidate, rule, confirmedAt, reportingTeam);

        latestConfirmed[key] = record;
        confirmedHistory.Add(record);

        TeamWarningResult warningResult = WarningLog.AddWarning(candidate.Offender.Team);
        OnViolationConfirmed?.Invoke(record, warningResult);
        return record;
    }

    private ViolationComplaintResult FailComplaint(
        Team reportingTeam,
        Skeleton accusedPlayer,
        ViolationType type,
        DateTime now,
        ViolationRule rule,
        ViolationConfirmationStatus status) {
        DateTime cooldownUntil = ComplaintLog.StartFailedComplaintCooldown(
            reportingTeam,
            type,
            now,
            rule.FailedComplaintCooldown);

        var result = new ViolationComplaintResult(
            status,
            type,
            reportingTeam,
            accusedPlayer,
            now,
            cooldownUntil,
            confirmedViolation: null);
        OnComplaintResolved?.Invoke(result);
        return result;
    }

    private ViolationRule EnsureKnownRule(ViolationType type) {
        if (!rules.TryGetValue(type, out ViolationRule rule))
            throw new InvalidOperationException($"Violation rule is not registered: {type}");

        return rule;
    }

    private static bool CanConfirmInMoment(ViolationRule rule, ViolationGameMoment? moment) {
        bool isDuringRound = moment != null && moment.IsRoundActive;
        return isDuringRound ? rule.CanHappenDuringRound : rule.CanHappenOutsideRound;
    }

    private static void RemoveExpired<TValue>(
        Dictionary<ViolationKey, TValue> records,
        DateTime now,
        Func<TValue, bool> isActive) {
        var expired = new List<ViolationKey>();
        foreach (var pair in records) {
            if (!isActive(pair.Value))
                expired.Add(pair.Key);
        }

        foreach (ViolationKey key in expired)
            records.Remove(key);
    }

    private readonly struct ViolationKey : IEquatable<ViolationKey> {
        private readonly Skeleton offender;
        private readonly ViolationType type;

        public ViolationKey(Skeleton offender, ViolationType type) {
            this.offender = offender;
            this.type = type;
        }

        public bool Equals(ViolationKey other) {
            return ReferenceEquals(offender, other.offender) && type == other.type;
        }

        public override bool Equals(object? obj) {
            return obj is ViolationKey other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return ((offender != null ? offender.GetHashCode() : 0) * 397) ^ (int)type;
            }
        }
    }
}