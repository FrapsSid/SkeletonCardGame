using System;
using System.Collections.Generic;
using System.Linq;

public class CombinationGenerator
{
    private const int DefaultGenerationAttempts = 64;
    private const int AutomaticInclusionSearchLimit = 5000;
    private const int SatisfiableSearchLimit = 5000;

    private readonly System.Random _random;
    private readonly int _maxGenerationAttempts;
    private readonly List<CardData> _standardDeck;

    public CombinationGenerator(int? seed = null, int maxGenerationAttempts = DefaultGenerationAttempts)
    {
        _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        _maxGenerationAttempts = Math.Max(1, maxGenerationAttempts);
        _standardDeck = BuildStandardDeck();
    }

    public RoundCombinationSet GenerateRoundCombinations()
    {
        for (int attempt = 0; attempt < _maxGenerationAttempts; attempt++)
        {
            RoundCombinationSet set = new RoundCombinationSet(
                GenerateCombination(CombinationTemplate.CreateDefault(CombinationDifficulty.Easy)),
                GenerateCombination(CombinationTemplate.CreateDefault(CombinationDifficulty.Medium)),
                GenerateCombination(CombinationTemplate.CreateDefault(CombinationDifficulty.Hard)),
                GenerateCombination(CombinationTemplate.CreateDefault(CombinationDifficulty.Anti)));

            if (ValidateCombinationSet(set))
            {
                return set;
            }
        }

        throw new InvalidOperationException("Unable to generate a valid round combination set.");
    }

    public bool ValidateCombinationSet(RoundCombinationSet set)
    {
        if (set == null) return false;

        List<(Combination combination, CombinationDifficulty difficulty)> all = set.GetAll();
        if (all.Any(entry => entry.combination == null)) return false;

        var signatures = new HashSet<string>();
        foreach ((Combination combination, CombinationDifficulty difficulty) in all)
        {
            if (!signatures.Add(GetCombinationSignature(combination))) return false;
        }

        foreach ((Combination combination, CombinationDifficulty difficulty) in set.GetScoringCombinations())
        {
            if (IsAutomaticallyIncluded(combination, set.antiCombination))
            {
                return false;
            }
        }

        return true;
    }

    public Combination GenerateCombination(CombinationTemplate template)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));

        for (int attempt = 0; attempt < _maxGenerationAttempts; attempt++)
        {
            Combination candidate = GenerateTemplateCandidate(template);
            if (candidate != null && CanCombinationBeSatisfied(candidate))
            {
                return candidate;
            }
        }

        int cardCount = _random.Next(template.minCardCount, template.maxCardCount + 1);

        switch (template.difficulty)
        {
            case CombinationDifficulty.Easy:
                return GenerateEasyCombination(cardCount);
            case CombinationDifficulty.Medium:
            case CombinationDifficulty.Anti:
                return GenerateMediumCombination(cardCount);
            case CombinationDifficulty.Hard:
                return GenerateHardCombination(cardCount);
            default:
                return GenerateEasyCombination(cardCount);
        }
    }

    private Combination GenerateTemplateCandidate(CombinationTemplate template)
    {
        if (template.possibleRules == null || template.possibleRules.Count == 0)
        {
            return null;
        }

        int cardCount = _random.Next(template.minCardCount, template.maxCardCount + 1);
        int modifierCount = Math.Max(1, template.modifierCount);
        List<CombinationRuleType> selectedTypes = PickRuleTypes(template.possibleRules, modifierCount);
        var rules = new List<CombinationRule>(selectedTypes.Count);

        foreach (CombinationRuleType ruleType in selectedTypes)
        {
            CombinationRuleParameterRange range = template.GetRange(ruleType) ?? CreateFallbackRange(ruleType, cardCount);
            rules.Add(range.CreateRule(_random, cardCount));
        }

        if (rules.All(rule => rule.ParamN <= 0))
        {
            rules.Add(new CombinationRule(CombinationRuleType.ExactCardCount, cardCount));
        }

        return new Combination(rules);
    }

    private List<CombinationRuleType> PickRuleTypes(List<CombinationRuleType> possibleRules, int count)
    {
        var pool = new List<CombinationRuleType>(possibleRules);
        var selected = new List<CombinationRuleType>(count);

        while (selected.Count < count && pool.Count > 0)
        {
            int index = _random.Next(pool.Count);
            selected.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return selected;
    }

    private CombinationRuleParameterRange CreateFallbackRange(CombinationRuleType ruleType, int cardCount)
    {
        switch (ruleType)
        {
            case CombinationRuleType.SumEquals:
                return new CombinationRuleParameterRange(ruleType, 0, 0, cardCount * 5, cardCount * 9, false, true);
            case CombinationRuleType.SumGreaterThan:
                return new CombinationRuleParameterRange(ruleType, 0, 0, cardCount * 5, cardCount * 10, false, true);
            case CombinationRuleType.SumLessThan:
                return new CombinationRuleParameterRange(ruleType, 0, 0, cardCount * 7, cardCount * 12, false, true);
            case CombinationRuleType.ContainsRank:
                return new CombinationRuleParameterRange(ruleType, 0, 0, 2, 14, false, true);
            case CombinationRuleType.SameRank:
            case CombinationRuleType.AllDifferentSuits:
                return new CombinationRuleParameterRange(ruleType, Math.Min(cardCount, 4), Math.Min(cardCount, 4));
            default:
                return new CombinationRuleParameterRange(ruleType, cardCount, cardCount);
        }
    }

    private Combination GenerateEasyCombination(int cardCount)
    {
        var options = new List<Func<Combination>>
        {
            () => NewCombination(new CombinationRule(CombinationRuleType.SameRank, Math.Min(cardCount, 4))),
            () => NewCombination(new CombinationRule(CombinationRuleType.SameSuit, cardCount)),
            () => NewCombination(new CombinationRule(CombinationRuleType.Sequence, cardCount)),
            () => NewCombination(new CombinationRule(CombinationRuleType.AllDifferentRanks, cardCount)),
            () => NewCombination(new CombinationRule(CombinationRuleType.AllDifferentSuits, Math.Min(cardCount, 4)))
        };

        return Pick(options)();
    }

    private Combination GenerateMediumCombination(int cardCount)
    {
        int rank = RandomRank();
        int lowSum = cardCount * 5;
        int highSum = cardCount * 7;

        var options = new List<Func<Combination>>
        {
            () => NewCombination(
                new CombinationRule(CombinationRuleType.SameSuit, cardCount),
                new CombinationRule(CombinationRuleType.AllDifferentRanks, cardCount)),

            () => NewCombination(
                new CombinationRule(CombinationRuleType.Sequence, cardCount),
                new CombinationRule(CombinationRuleType.SumGreaterThan, paramValue: lowSum)),

            () => NewCombination(
                new CombinationRule(CombinationRuleType.SameSuit, cardCount),
                new CombinationRule(CombinationRuleType.ContainsRank, paramValue: rank)),

            () => NewCombination(
                new CombinationRule(CombinationRuleType.SameRank, Math.Min(cardCount, 4)),
                new CombinationRule(CombinationRuleType.SumGreaterThan, paramValue: lowSum)),

            () => NewCombination(
                new CombinationRule(CombinationRuleType.AllDifferentRanks, cardCount),
                new CombinationRule(CombinationRuleType.SumLessThan, paramValue: highSum))
        };

        if (cardCount <= 4)
        {
            options.Add(() => NewCombination(
                new CombinationRule(CombinationRuleType.AllDifferentSuits, cardCount),
                new CombinationRule(CombinationRuleType.SumGreaterThan, paramValue: lowSum)));
        }

        return Pick(options)();
    }

    private Combination GenerateHardCombination(int cardCount)
    {
        int rank = RandomRank();
        int lowSum = cardCount * 7;

        var options = new List<Func<Combination>>
        {
            () => NewCombination(
                new CombinationRule(CombinationRuleType.Sequence, cardCount),
                new CombinationRule(CombinationRuleType.SameSuit, cardCount),
                new CombinationRule(CombinationRuleType.SumGreaterThan, paramValue: lowSum)),

            () => NewCombination(
                new CombinationRule(CombinationRuleType.SameSuit, cardCount),
                new CombinationRule(CombinationRuleType.AllDifferentRanks, cardCount),
                new CombinationRule(CombinationRuleType.SumGreaterThan, paramValue: lowSum)),

            () => NewCombination(
                new CombinationRule(CombinationRuleType.Sequence, cardCount),
                new CombinationRule(CombinationRuleType.AllDifferentRanks, cardCount),
                new CombinationRule(CombinationRuleType.SumGreaterThan, paramValue: lowSum)),

            () => NewCombination(
                new CombinationRule(CombinationRuleType.SameSuit, cardCount),
                new CombinationRule(CombinationRuleType.AllDifferentRanks, cardCount),
                new CombinationRule(CombinationRuleType.ContainsRank, paramValue: rank))
        };

        if (cardCount <= 4)
        {
            options.Add(() => NewCombination(
                new CombinationRule(CombinationRuleType.Sequence, cardCount),
                new CombinationRule(CombinationRuleType.AllDifferentSuits, cardCount),
                new CombinationRule(CombinationRuleType.SumGreaterThan, paramValue: lowSum)));
        }

        return Pick(options)();
    }

    private Combination NewCombination(params CombinationRule[] rules)
    {
        return new Combination(rules.ToList());
    }

    private string GetCombinationSignature(Combination combination)
    {
        if (combination == null) return string.Empty;

        return string.Join("|", combination.Rules
            .Select(rule => $"{rule.Type}:{rule.ParamN}:{rule.ParamValue}")
            .OrderBy(value => value));
    }

    private Func<Combination> Pick(List<Func<Combination>> options)
    {
        return options[_random.Next(options.Count)];
    }

    private int RandomRank()
    {
        return _random.Next(2, 15);
    }

    private bool IsAutomaticallyIncluded(Combination source, Combination target)
    {
        bool foundSourceMatch = false;
        int checkedCandidates = 0;
        var candidate = new List<CardData>(source.RequiredCardCount);

        bool hasWitness = FindSourceWithoutTarget(source, target, 0, candidate, ref foundSourceMatch, ref checkedCandidates);
        return foundSourceMatch && !hasWitness;
    }

    private bool CanCombinationBeSatisfied(Combination combination)
    {
        int checkedCandidates = 0;
        var candidate = new List<CardData>(combination.RequiredCardCount);
        return FindAnyMatch(combination, 0, candidate, ref checkedCandidates);
    }

    private bool FindAnyMatch(
        Combination combination,
        int startIndex,
        List<CardData> candidate,
        ref int checkedCandidates)
    {
        if (checkedCandidates >= SatisfiableSearchLimit)
        {
            return false;
        }

        if (candidate.Count == combination.RequiredCardCount)
        {
            checkedCandidates++;
            return combination.IsSatisfied(candidate);
        }

        int cardsNeeded = combination.RequiredCardCount - candidate.Count;
        if (_standardDeck.Count - startIndex < cardsNeeded) return false;

        for (int i = startIndex; i < _standardDeck.Count; i++)
        {
            candidate.Add(_standardDeck[i]);

            if (FindAnyMatch(combination, i + 1, candidate, ref checkedCandidates))
            {
                return true;
            }

            candidate.RemoveAt(candidate.Count - 1);
        }

        return false;
    }

    private bool FindSourceWithoutTarget(
        Combination source,
        Combination target,
        int startIndex,
        List<CardData> candidate,
        ref bool foundSourceMatch,
        ref int checkedCandidates)
    {
        if (checkedCandidates >= AutomaticInclusionSearchLimit)
        {
            return false;
        }

        if (candidate.Count == source.RequiredCardCount)
        {
            checkedCandidates++;
            if (!source.IsSatisfied(candidate)) return false;

            foundSourceMatch = true;
            return !target.IsSatisfied(candidate);
        }

        int cardsNeeded = source.RequiredCardCount - candidate.Count;
        if (_standardDeck.Count - startIndex < cardsNeeded) return false;

        for (int i = startIndex; i < _standardDeck.Count; i++)
        {
            candidate.Add(_standardDeck[i]);

            if (FindSourceWithoutTarget(source, target, i + 1, candidate, ref foundSourceMatch, ref checkedCandidates))
            {
                return true;
            }

            candidate.RemoveAt(candidate.Count - 1);
        }

        return false;
    }

    private List<CardData> BuildStandardDeck()
    {
        var cards = new List<CardData>();

        foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardValue value in Enum.GetValues(typeof(CardValue)))
            {
                cards.Add(new CardData(suit, value, true));
            }
        }

        return cards;
    }
}
