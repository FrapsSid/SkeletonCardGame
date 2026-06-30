using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class PlayerBetState
{
    public DeclaredCombinationTier? declaredTarget;
    public bool HasDeclaredTarget { get => declaredTarget != null; }
    public bool hasFolded;
    public IList<StakeAsset> committedAssets = new List<StakeAsset>();
    public int committedValue = 0;
    public int AssetsValue { get => committedAssets.Sum(a => a.stakeValue); }

    public PlayerBetState(DeclaredCombinationTier? declaredTarget = null)
    {
        this.declaredTarget = declaredTarget;
    }
}
