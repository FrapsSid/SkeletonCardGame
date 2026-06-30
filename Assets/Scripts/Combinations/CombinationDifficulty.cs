namespace Combinations
{
    public enum CombinationDifficulty
    {
        Anti = 0,
        Easy = 1,
        Medium = 2,
        Hard = 3
    }

    public static class CombinationDifficultyExtensions
    {
        public static int GetScoreValue(this CombinationDifficulty difficulty)
        {
            switch (difficulty)
            {
                case CombinationDifficulty.Easy:
                    return 1;
                case CombinationDifficulty.Medium:
                    return 2;
                case CombinationDifficulty.Hard:
                    return 3;
                default:
                    return 0;
            }
        }

        public static bool IsScoringDifficulty(this CombinationDifficulty difficulty)
        {
            return difficulty != CombinationDifficulty.Anti;
        }
    }
}
