using System.Collections.Generic;
using System.Threading.Tasks;
using ArcanumVivum.SpellEngine.Core;
using ArcanumVivum.SpellEngine.Database;
using ArcanumVivum.SpellEngine.Models;
using ArcanumVivum.SpellEngine.Resolvers;
using UnityEngine;

namespace ArcanumVivum.SpellEngine.Samples
{
    public sealed class BasicUsageExample : MonoBehaviour
    {
        private SpellEngineService? _engine;

        private void Awake()
        {
            var database = new InMemorySpellDatabase();
            var resolver = new RuleBasedResolver();
            _engine = new SpellEngineService(database, resolver);
        }

        public async Task CastDemoSpellAsync()
        {
            if (_engine == null)
            {
                return;
            }

            var strokes = new List<IReadOnlyList<Point2>>
            {
                new List<Point2>
                {
                    new Point2(100f, 100f),
                    new Point2(150f, 50f),
                    new Point2(200f, 100f),
                    new Point2(180f, 150f),
                    new Point2(120f, 150f)
                }
            };

            var result = await _engine.CastSpellAsync(strokes, "fire lance");
            if (result == null)
            {
                Debug.LogWarning("No spell result.");
                return;
            }

            Debug.Log($"Spell: {result.Spell.SpellName}, Type: {result.MatchType}, Mana: {result.Spell.ManaCost}");
        }
    }
}
