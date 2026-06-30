using System.Collections.Generic;
using UnityEngine;
using Combinations.Rules;
namespace Combinations.Tests
{
    /// <summary>
    /// Комплексные тесты всех 22 комбинаций
    /// По 2 теста на каждую: положительный и отрицательный
    /// </summary>
    public class TestsCombinations : MonoBehaviour
    {
        private int _totalTests = 0;
        private int _passedTests = 0;
        private void Start()
        {
            Debug.Log("=== КОМПЛЕКСНОЕ ТЕСТИРОВАНИЕ ВСЕХ 22 КОМБИНАЦИЙ ===\n");
            // Последовательности (3)
            TestThreeInARow();
            TestFourInARow();
            TestFiveInARow();
            // Мастные последовательности (3)
            TestSuitedLadder3();
            TestSuitedLadder4();
            TestStraightFlush();
            // Группы рангов (5)
            TestThreeOfAKind();
            TestTwoPair();
            TestDoubleStep();
            TestTripleStep();
            TestFullHouse();
            // Сложные формы (4)
            TestDrawBridge();
            TestBrokenSeal();
            TestSkewTower();
            TestDoubleFork();
            // Цветовые комбинации (2)
            TestRedBlackCastle();
            TestTwoMasks();
            // Продвинутые источники (5)
            TestHinge();
            TestPincers();
            TestTwoPairSplit();
            TestSuitCross();
            TestReflection();
            Debug.Log($"\n\n=== ИТОГОВЫЕ РЕЗУЛЬТАТЫ ===");
            Debug.Log($"Пройдено: {_passedTests}/{_totalTests} тестов");

            if (_passedTests == _totalTests)
            {
                Debug.Log("✅ ВСЕ ТЕСТЫ УСПЕШНО ПРОЙДЕНЫ!");
            }
            else
            {
                Debug.LogError($"❌ ПРОВАЛЕНО: {_totalTests - _passedTests} тестов");
            }
        }
        #region Последовательности
        private void TestThreeInARow()
        {
            var combo = new Combination(new ThreeInARow());
            // Тест 1: ПРОХОДИТ - 7-8-9 (2 из руки, 1 со стола)
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Nine, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7-8-9 (схема 2+1)");
            // Тест 2: НЕ ПРОХОДИТ - 7-8-10 (не последовательность)
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Ten, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "7-8-10 (пропущена 9)");
        }
        private void TestFourInARow()
        {
            var combo = new Combination(new FourInARow());
            // Тест 1: ПРОХОДИТ - 5-6-7-8 (2+2)
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Five, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Six, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Eight, CardPool.Table)
            };
            RunTest(combo, validCards, true, "5-6-7-8 (схема 2+2)");
            // Тест 2: НЕ ПРОХОДИТ - все из разных источников
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Five, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Six, CardPool.Table),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Player2Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Eight, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "5-6-7-8 (невалидная схема источников)");
        }
        private void TestFiveInARow()
        {
            var combo = new Combination(new FiveInARow());
            // Тест 1: ПРОХОДИТ - 6-7-8-9-10 (схема 3+2)
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Six, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Ten, CardPool.Table)
            };
            RunTest(combo, validCards, true, "6-7-8-9-10 (схема 3+2)");
            // Тест 2: НЕ ПРОХОДИТ - 6-7-8-9-J (не последовательность)
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Six, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Jack, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "6-7-8-9-J (пропущена 10)");
        }
        #endregion
        #region Мастные последовательности
        private void TestSuitedLadder3()
        {
            var combo = new Combination(new SuitedLadder3());
            // Тест 1: ПРОХОДИТ - 7♥-8♥-9♥
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Nine, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7♥-8♥-9♥ (одна масть)");
            // Тест 2: НЕ ПРОХОДИТ - разные масти
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Nine, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "7♥-8♣-9♥ (разные масти)");
        }
        private void TestSuitedLadder4()
        {
            var combo = new Combination(new SuitedLadder4());
            // Тест 1: ПРОХОДИТ - 7♠-8♠-9♠-10♠
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Spades, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Ten, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7♠-8♠-9♠-10♠ (одна масть)");
            // Тест 2: НЕ ПРОХОДИТ - последовательность, но разные масти
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Spades, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Ten, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "7♠-8♥-9♠-10♠ (разные масти)");
        }
        private void TestStraightFlush()
        {
            var combo = new Combination(new StraightFlush());
            // Тест 1: ПРОХОДИТ - 4♦-5♦-6♦-7♦-8♦
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Diamonds, CardValue.Four, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Five, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Six, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Table)
            };
            RunTest(combo, validCards, true, "4♦-5♦-6♦-7♦-8♦ (стрит-флеш)");
            // Тест 2: НЕ ПРОХОДИТ - последовательность одной масти, но неправильные источники
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Diamonds, CardValue.Four, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Five, CardPool.Table),
                new CardWithPool(CardSuit.Diamonds, CardValue.Six, CardPool.Player2Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Player1Hand)
            };
            RunTest(combo, invalidCards, false, "4♦-5♦-6♦-7♦-8♦ (невалидная схема 3+2)");
        }
        #endregion
        #region Группы рангов
        private void TestThreeOfAKind()
        {
            var combo = new Combination(new ThreeOfAKind());
            // Тест 1: ПРОХОДИТ - 7-7-7
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7-7-7 (тройка)");
            // Тест 2: НЕ ПРОХОДИТ - три разных ранга
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Nine, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "7-8-9 (не тройка)");
        }
        private void TestTwoPair()
        {
            var combo = new Combination(new TwoPair());
            // Тест 1: ПРОХОДИТ - 7-7 + Q-Q
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Queen, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Queen, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7-7 + Q-Q (две пары)");
            // Тест 2: НЕ ПРОХОДИТ - четыре одинаковых (не две пары)
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Seven, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "7-7-7-7 (каре, не две пары)");
        }
        private void TestDoubleStep()
        {
            var combo = new Combination(new DoubleStep());
            // Тест 1: ПРОХОДИТ - 7-7 + 8-8
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Eight, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7-7 + 8-8 (двойная ступень)");
            // Тест 2: НЕ ПРОХОДИТ - две пары, но не последовательных рангов
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Queen, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Queen, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "7-7 + Q-Q (не последовательные ранги)");
        }
        private void TestTripleStep()
        {
            var combo = new Combination(new TripleStep());
            // Тест 1: ПРОХОДИТ - 6-6 + 7-7 + 8-8
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Six, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Six, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Player2Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Seven, CardPool.Player2Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Eight, CardPool.Table),
                new CardWithPool(CardSuit.Clubs, CardValue.Eight, CardPool.Table)
            };
            RunTest(combo, validCards, true, "6-6 + 7-7 + 8-8 (тройная ступень)");
            // Тест 2: НЕ ПРОХОДИТ - три пары, но с пропуском
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Six, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Six, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Player2Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Seven, CardPool.Player2Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Clubs, CardValue.Nine, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "6-6 + 7-7 + 9-9 (пропущена 8)");
        }
        private void TestFullHouse()
        {
            var combo = new Combination(new FullHouse());
            // Тест 1: ПРОХОДИТ - 7-7-7 + Q-Q
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Queen, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Queen, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7-7-7 + Q-Q (фулл)");
            // Тест 2: НЕ ПРОХОДИТ - только тройка
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Queen, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.King, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "7-7-7 + Q + K (нет пары)");
        }
        #endregion
        #region Сложные формы
        private void TestDrawBridge()
        {
            var combo = new Combination(new DrawBridge());
            // Тест 1: ПРОХОДИТ - 4-5-6 + 8-9-10 (пропущена 7)
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Four, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Five, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Six, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Eight, CardPool.Table),
                new CardWithPool(CardSuit.Clubs, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Ten, CardPool.Table)
            };
            RunTest(combo, validCards, true, "4-5-6 + 8-9-10 (разводной мост)");
            // Тест 2: НЕ ПРОХОДИТ - пропущено 2 ранга
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Four, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Five, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Six, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Clubs, CardValue.Ten, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Jack, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "4-5-6 + 9-10-J (пропущено 2 ранга)");
        }
        private void TestBrokenSeal()
        {
            var combo = new Combination(new BrokenSeal());
            // Тест 1: ПРОХОДИТ - 7-7 + 8-9-10
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Hearts, CardValue.Ten, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7-7 + 8-9-10 (сломанная печать)");
            // Тест 2: НЕ ПРОХОДИТ - пара не в начале
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Hearts, CardValue.Ten, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "7 + 8-8 + 9-10 (пара не в начале)");
        }
        private void TestSkewTower()
        {
            var combo = new Combination(new SkewTower());
            // Тест 1: ПРОХОДИТ - 7-7 + 8-8-9
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Eight, CardPool.Table),
                new CardWithPool(CardSuit.Hearts, CardValue.Nine, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7-7 + 8-8-9 (косая башня)");
            // Тест 2: НЕ ПРОХОДИТ - неправильный паттерн
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Hearts, CardValue.Ten, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "7-7 + 9-9-10 (пропущена 8)");
        }
        private void TestDoubleFork()
        {
            var combo = new Combination(new DoubleFork());
            // Тест 1: ПРОХОДИТ - 7-7 + 8 + 9-9
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Hearts, CardValue.Nine, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7-7 + 8 + 9-9 (двойная вилка)");
            // Тест 2: НЕ ПРОХОДИТ - неправильный паттерн
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Eight, CardPool.Table),
                new CardWithPool(CardSuit.Hearts, CardValue.Ten, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "7-7 + 8-8 + 10 (не двойная вилка)");
        }
        #endregion
        #region Цветовые комбинации
        private void TestRedBlackCastle()
        {
            var combo = new Combination(new RedBlackCastle());
            // Тест 1: ПРОХОДИТ - 2 красные + 2 черные семерки
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Seven, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7♥ 7♦ + 7♣ 7♠ (красно-черный замок)");
            // Тест 2: НЕ ПРОХОДИТ - 3 красные + 1 черная
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Seven, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "3 красные + 1 черная (неправильный баланс)");
        }
        private void TestTwoMasks()
        {
            var combo = new Combination(new TwoMasks());
            // Тест 1: ПРОХОДИТ - красная пара 7 + черная пара 8
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Eight, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Eight, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7♥ 7♦ + 8♣ 8♠ (две маски)");
            // Тест 2: НЕ ПРОХОДИТ - ранги не соседние
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Ten, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Ten, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "7♥ 7♦ + 10♣ 10♠ (ранги не соседние)");
        }
        #endregion
        #region Продвинутые источники
        private void TestHinge()
        {
            var combo = new Combination(new Hinge());
            // Тест 1: ПРОХОДИТ - центральная пара 8-8 + внешние 6-7-9-10
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Six, CardPool.Table),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Clubs, CardValue.Ten, CardPool.Table)
            };
            RunTest(combo, validCards, true, "6-7-9-10 + 8-8 (шарнир)");
            // Тест 2: НЕ ПРОХОДИТ - неправильный паттерн
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Six, CardPool.Table),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Nine, CardPool.Table),
                new CardWithPool(CardSuit.Clubs, CardValue.Ten, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "6-7-7-8-9-10 (неправильный паттерн)");
        }
        private void TestPincers()
        {
            var combo = new Combination(new Pincers());
            // Тест 1: ПРОХОДИТ - пара 8-8 + фланги 7 и 9
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Clubs, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Nine, CardPool.Table)
            };
            RunTest(combo, validCards, true, "7 + 8-8 + 9 (клещи)");
            // Тест 2: НЕ ПРОХОДИТ - неправильный паттерн
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Eight, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Nine, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "7-7-8-9 (не клещи)");
        }
        private void TestTwoPairSplit()
        {
            var combo = new Combination(new TwoPairSplit());
            // Тест 1: ПРОХОДИТ - раскол (7+Q) + (7+Q)
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Queen, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Queen, CardPool.Table)
            };
            RunTest(combo, validCards, true, "(7+Q) + (7+Q) (две пары с расколом)");
            // Тест 2: НЕ ПРОХОДИТ - обычные две пары (не раскол)
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Queen, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Queen, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "(7-7) + (Q-Q) (не раскол)");
        }
        private void TestSuitCross()
        {
            var combo = new Combination(new SuitCross());
            // Тест 1: ПРОХОДИТ - диагональ (7♠+Q♥) + (7♥+Q♠)
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Spades, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Queen, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Spades, CardValue.Queen, CardPool.Table)
            };
            RunTest(combo, validCards, true, "(7♠+Q♥) + (7♥+Q♠) (перекрест мастей)");
            // Тест 2: НЕ ПРОХОДИТ - не диагональ
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Spades, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Queen, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Hearts, CardValue.Queen, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "(7♠+Q♠) + (7♥+Q♥) (не диагональ)");
        }
        private void TestReflection()
        {
            var combo = new Combination(new Reflection());
            // Тест 1: ПРОХОДИТ - (5-7-9) + (5-7-9)
            var validCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Five, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Nine, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Five, CardPool.Table),
                new CardWithPool(CardSuit.Hearts, CardValue.Seven, CardPool.Table),
                new CardWithPool(CardSuit.Clubs, CardValue.Nine, CardPool.Table)
            };
            RunTest(combo, validCards, true, "(5-7-9) + (5-7-9) (отражение)");
            // Тест 2: НЕ ПРОХОДИТ - неправильный шаг
            var invalidCards = new List<CardWithPool>
            {
                new CardWithPool(CardSuit.Hearts, CardValue.Five, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Clubs, CardValue.Six, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Diamonds, CardValue.Seven, CardPool.Player1Hand),
                new CardWithPool(CardSuit.Spades, CardValue.Five, CardPool.Table),
                new CardWithPool(CardSuit.Hearts, CardValue.Six, CardPool.Table),
                new CardWithPool(CardSuit.Clubs, CardValue.Seven, CardPool.Table)
            };
            RunTest(combo, invalidCards, false, "(5-6-7) + (5-6-7) (шаг 1, не 2)");
        }
        #endregion
        #region Утилиты
        private void RunTest(Combination combo, List<CardWithPool> cards, bool shouldPass, string description)
        {
            _totalTests++;

            bool result = combo.IsSatisfied(cards);
            bool testPassed = (result == shouldPass);
            string expectedStr = shouldPass ? "✓ ДОЛЖНА ПРОЙТИ" : "✗ НЕ ДОЛЖНА ПРОЙТИ";
            string actualStr = result ? "✓ ПРОШЛА" : "✗ НЕ ПРОШЛА";
            string statusIcon = testPassed ? "✅" : "❌";
            Debug.Log($"{statusIcon} [{combo.DisplayName}] {description}");
            Debug.Log($"   Ожидание: {expectedStr} | Результат: {actualStr}");
            if (!testPassed)
            {
                Debug.LogError($"   ТЕСТ ПРОВАЛЕН!");
            }
            else
            {
                _passedTests++;
            }
        }
        #endregion
    }
}
