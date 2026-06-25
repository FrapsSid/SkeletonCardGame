#nullable enable
using System;
using System.Collections.Generic;

public sealed class ViolationComplaintLog {
    private readonly Dictionary<ComplaintCooldownKey, DateTime> cooldowns = new Dictionary<ComplaintCooldownKey, DateTime>();

    public bool IsOnCooldown(Team reportingTeam, ViolationType type, DateTime now, out DateTime cooldownUntil) {
        var key = new ComplaintCooldownKey(reportingTeam, type);
        if (cooldowns.TryGetValue(key, out cooldownUntil) && now < cooldownUntil)
            return true;

        return false;
    }

    public DateTime StartFailedComplaintCooldown(Team reportingTeam, ViolationType type, DateTime now,
        TimeSpan duration) {
        var key = new ComplaintCooldownKey(reportingTeam, type);
        DateTime cooldownUntil = now + duration;
        cooldowns[key] = cooldownUntil;
        return cooldownUntil;
    }

    public void ClearExpired(DateTime now) {
        var expired = new List<ComplaintCooldownKey>();
        foreach (var pair in cooldowns) {
            if (now >= pair.Value)
                expired.Add(pair.Key);
        }

        foreach (ComplaintCooldownKey key in expired)
            cooldowns.Remove(key);
    }

    private readonly struct ComplaintCooldownKey : IEquatable<ComplaintCooldownKey> {
        private readonly Team reportingTeam;
        private readonly ViolationType type;

        public ComplaintCooldownKey(Team reportingTeam, ViolationType type) {
            this.reportingTeam = reportingTeam;
            this.type = type;
        }

        public bool Equals(ComplaintCooldownKey other) {
            return ReferenceEquals(reportingTeam, other.reportingTeam) && type == other.type;
        }

        public override bool Equals(object? obj) {
            return obj is ComplaintCooldownKey other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return ((reportingTeam != null ? reportingTeam.GetHashCode() : 0) * 397) ^ (int)type;
            }
        }
    }
}