using System;
using System.Collections.Generic;

using Player = Skeleton;

[Serializable]
public class PlayerBetState {
    public Player player;
    public DeclaredCombinationTier declaredTarget;
    public bool hasDeclaredTarget;
    public bool hasFolded;
    public List<StakeAsset> committedAssets = new List<StakeAsset>();
    public int committedValue;
    public bool hasMatchedCurrentPrice;

    public bool folded
    {
        get => hasFolded;
        set => hasFolded = value;
    }

    public PlayerBetState() {
    }

    public PlayerBetState(Player player) {
        this.player = player;
    }

    public PlayerBetState(DeclaredCombinationTier declaredTarget) {
        this.declaredTarget = declaredTarget;
        hasDeclaredTarget = true;
    }

    public PlayerBetState(DeclaredCombinationTier declaredTarget, bool folded) {
        this.declaredTarget = declaredTarget;
        hasDeclaredTarget = true;
        hasFolded = folded;
    }
}
