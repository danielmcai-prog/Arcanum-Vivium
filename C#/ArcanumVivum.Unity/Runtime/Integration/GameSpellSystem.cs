using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcanumVivum.SpellEngine.Core;
using ArcanumVivum.SpellEngine.Models;

namespace ArcanumVivum.SpellEngine.Integration
{
    public sealed class GameSpellSystem
    {
        private readonly SpellEngineService _spellEngine;
        private readonly Dictionary<string, long> _lastCastBySpellId = new();
        private static readonly Random Rng = new();

        public int ActiveMana { get; private set; }
        public int MaxMana { get; }

        public GameSpellSystem(SpellEngineService spellEngine, int startingMana = 100, int maxMana = 100)
        {
            _spellEngine = spellEngine;
            ActiveMana = startingMana;
            MaxMana = maxMana;
        }

        public async Task<GameCastResult> CastSymbolSpellAsync(
            IReadOnlyList<IReadOnlyList<Point2>> strokes,
            string? incantation,
            CancellationToken cancellationToken = default)
        {
            var result = await _spellEngine.CastSpellAsync(strokes, incantation, cancellationToken);
            if (result == null)
            {
                return GameCastResult.Fail("Resolver returned no spell.");
            }

            var spell = result.Spell;
            if (spell.ManaCost > ActiveMana)
            {
                return GameCastResult.Fail("Not enough mana.");
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _lastCastBySpellId.TryGetValue(spell.SpellId, out var lastCast);
            if (nowMs - lastCast < spell.Cooldown * 1000f)
            {
                return GameCastResult.Fail("Spell is on cooldown.");
            }

            ActiveMana -= spell.ManaCost;
            _lastCastBySpellId[spell.SpellId] = nowMs;

            var damage = spell.ManaCost * spell.Stability;
            var willMisfire = Rng.NextDouble() < spell.InstabilityProbability;

            return GameCastResult.Success(result, damage, willMisfire, ActiveMana);
        }

        public void RestoreMana(int amount)
        {
            ActiveMana = Math.Min(MaxMana, ActiveMana + Math.Max(0, amount));
        }
    }

    public sealed class GameCastResult
    {
        public bool IsSuccess { get; private set; }
        public string? Error { get; private set; }
        public SpellMatchResult? SpellResult { get; private set; }
        public float Damage { get; private set; }
        public bool WillMisfire { get; private set; }
        public int RemainingMana { get; private set; }

        public static GameCastResult Success(SpellMatchResult result, float damage, bool willMisfire, int remainingMana)
        {
            return new GameCastResult
            {
                IsSuccess = true,
                SpellResult = result,
                Damage = damage,
                WillMisfire = willMisfire,
                RemainingMana = remainingMana
            };
        }

        public static GameCastResult Fail(string error)
        {
            return new GameCastResult
            {
                IsSuccess = false,
                Error = error
            };
        }
    }
}
