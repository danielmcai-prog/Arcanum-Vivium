/**
 * ARCANUM VIVUM — Core Spell Engine
 * Pure JavaScript spell casting engine (no UI dependencies)
 * 
 * Usage:
 * import { SpellEngine, castSpell, FeatureExtractor } from '@arcanum/spell-engine';
 * 
 * const engine = new SpellEngine(database);
 * const result = await engine.castSpell({ strokes, incantation });
 */

// ─── 1. FEATURE EXTRACTOR ───────────────────────────────────
export const FeatureExtractor = {
  // Ramer–Douglas–Peucker path simplification
  rdp(points, epsilon = 2.0) {
    if (points.length < 3) return points;
    let maxDist = 0, idx = 0;
    const end = points.length - 1;
    for (let i = 1; i < end; i++) {
      const d = this.perpendicularDist(points[i], points[0], points[end]);
      if (d > maxDist) { maxDist = d; idx = i; }
    }
    if (maxDist > epsilon) {
      const l = this.rdp(points.slice(0, idx + 1), epsilon);
      const r = this.rdp(points.slice(idx), epsilon);
      return [...l.slice(0, -1), ...r];
    }
    return [points[0], points[end]];
  },

  perpendicularDist(p, a, b) {
    const dx = b.x - a.x, dy = b.y - a.y;
    if (dx === 0 && dy === 0) return Math.hypot(p.x - a.x, p.y - a.y);
    const t = ((p.x - a.x) * dx + (p.y - a.y) * dy) / (dx * dx + dy * dy);
    return Math.hypot(p.x - (a.x + t * dx), p.y - (a.y + t * dy));
  },

  normalise(strokes) {
    const all = strokes.flat();
    if (!all.length) return strokes;
    const xs = all.map(p => p.x), ys = all.map(p => p.y);
    const minX = Math.min(...xs), maxX = Math.max(...xs);
    const minY = Math.min(...ys), maxY = Math.max(...ys);
    const cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
    const scale = Math.max(maxX - minX, maxY - minY) || 1;
    return strokes.map(s => s.map(p => ({
      x: ((p.x - cx) / scale) * 64,
      y: ((p.y - cy) / scale) * 64
    })));
  },

  extract(strokes) {
    if (!strokes || strokes.flat().length < 2) return null;
    const norm = this.normalise(strokes.map(s => this.rdp(s, 2)));
    const all = norm.flat();

    const sym = this._symmetry(all);
    const enclosure = this._enclosure(norm);
    const spirality = this._spirality(all);
    const angularity = this._angularity(norm);

    const totalLen = norm.reduce((acc, s) => {
      let l = 0;
      for (let i = 1; i < s.length; i++)
        l += Math.hypot(s[i].x - s[i-1].x, s[i].y - s[i-1].y);
      return acc + l;
    }, 0);
    const complexity = Math.min(1, totalLen / 400);

    const pointCount = this._detectPoints(all);
    const dirBias = this._directionBias(all);
    const intersections = this._intersectionCount(norm);
    const strokeCount = norm.length;

    const xs = all.map(p => p.x), ys = all.map(p => p.y);
    const w = Math.max(...xs) - Math.min(...xs) || 1;
    const h = Math.max(...ys) - Math.min(...ys) || 1;
    const aspectRatio = w / h;

    const compactness = this._compactness(all);

    return {
      symmetry: sym,
      enclosure,
      spirality,
      angularity,
      complexity,
      pointCount,
      dirBias,
      intersections,
      strokeCount,
      aspectRatio,
      compactness,
      normStrokes: norm,
    };
  },

  _symmetry(pts) {
    if (!pts.length) return 0;
    let score = 0;
    for (const p of pts) {
      const mirror = { x: -p.x, y: p.y };
      const nearest = pts.reduce((best, q) => {
        const d = Math.hypot(q.x - mirror.x, q.y - mirror.y);
        return d < best.d ? { d, q } : best;
      }, { d: Infinity });
      score += Math.max(0, 1 - nearest.d / 20);
    }
    return score / pts.length;
  },

  _enclosure(strokes) {
    for (const s of strokes) {
      if (s.length < 4) continue;
      const first = s[0], last = s[s.length - 1];
      const d = Math.hypot(first.x - last.x, first.y - last.y);
      if (d < 12) return 1;
    }
    if (strokes.length > 1) {
      const all = strokes.flat();
      const first = all[0], last = all[all.length - 1];
      if (Math.hypot(first.x - last.x, first.y - last.y) < 15) return 0.7;
    }
    return 0;
  },

  _spirality(pts) {
    if (pts.length < 6) return 0;
    let angles = [];
    for (let i = 1; i < pts.length; i++) {
      angles.push(Math.atan2(pts[i].y - pts[i-1].y, pts[i].x - pts[i-1].x));
    }
    let totalTurn = 0;
    for (let i = 1; i < angles.length; i++) {
      let d = angles[i] - angles[i-1];
      if (d > Math.PI) d -= 2 * Math.PI;
      if (d < -Math.PI) d += 2 * Math.PI;
      totalTurn += d;
    }
    return Math.min(1, Math.abs(totalTurn) / (2 * Math.PI * 2));
  },

  _angularity(strokes) {
    let sharp = 0, total = 0;
    for (const s of strokes) {
      for (let i = 1; i < s.length - 1; i++) {
        const a = Math.atan2(s[i].y - s[i-1].y, s[i].x - s[i-1].x);
        const b = Math.atan2(s[i+1].y - s[i].y, s[i+1].x - s[i].x);
        let diff = Math.abs(a - b);
        if (diff > Math.PI) diff = 2 * Math.PI - diff;
        if (diff > 0.5) sharp++;
        total++;
      }
    }
    return total ? sharp / total : 0;
  },

  _detectPoints(pts) {
    if (pts.length < 5) return 0;
    let extrema = 0;
    for (let i = 1; i < pts.length - 1; i++) {
      const d1 = Math.hypot(pts[i].x, pts[i].y);
      const d0 = Math.hypot(pts[i-1].x, pts[i-1].y);
      const d2 = Math.hypot(pts[i+1].x, pts[i+1].y);
      if (d1 > d0 && d1 > d2 && d1 > 15) extrema++;
    }
    return Math.min(extrema, 12);
  },

  _directionBias(pts) {
    if (!pts.length) return 0;
    const cx = pts.reduce((s, p) => s + p.x, 0) / pts.length;
    const cy = pts.reduce((s, p) => s + p.y, 0) / pts.length;
    return Math.atan2(cy, cx) / Math.PI;
  },

  _intersectionCount(strokes) {
    let count = 0;
    const segs = [];
    for (const s of strokes)
      for (let i = 0; i < s.length - 1; i++)
        segs.push([s[i], s[i+1]]);
    for (let i = 0; i < segs.length; i++)
      for (let j = i + 2; j < segs.length; j++)
        if (this._segIntersect(segs[i][0], segs[i][1], segs[j][0], segs[j][1]))
          count++;
    return Math.min(count, 20);
  },

  _segIntersect(a, b, c, d) {
    const det = (b.x-a.x)*(d.y-c.y) - (b.y-a.y)*(d.x-c.x);
    if (Math.abs(det) < 0.001) return false;
    const t = ((c.x-a.x)*(d.y-c.y) - (c.y-a.y)*(d.x-c.x)) / det;
    const u = -((a.x-c.x)*(b.y-a.y) - (a.y-c.y)*(b.x-a.x)) / det;
    return t > 0.01 && t < 0.99 && u > 0.01 && u < 0.99;
  },

  _compactness(pts) {
    if (pts.length < 3) return 0;
    const xs = pts.map(p => p.x), ys = pts.map(p => p.y);
    const area = (Math.max(...xs) - Math.min(...xs)) * (Math.max(...ys) - Math.min(...ys));
    let perim = 0;
    for (let i = 1; i < pts.length; i++)
      perim += Math.hypot(pts[i].x - pts[i-1].x, pts[i].y - pts[i-1].y);
    return perim > 0 ? Math.min(1, (4 * Math.PI * area) / (perim * perim)) : 0;
  }
};

// ─── 2. EMBEDDING GENERATOR ─────────────────────────────────
export const EmbeddingGenerator = {
  generate(features, incantationText = "") {
    if (!features) return new Float32Array(32);
    const {
      symmetry, enclosure, spirality, angularity,
      complexity, pointCount, dirBias, intersections,
      strokeCount, aspectRatio, compactness
    } = features;

    const textVec = this._textEmbedding(incantationText);

    const symVec = new Float32Array([
      symmetry,
      enclosure,
      spirality,
      angularity,
      complexity,
      Math.min(1, pointCount / 8),
      dirBias,
      Math.min(1, intersections / 10),
      Math.min(1, strokeCount / 5),
      Math.min(1, Math.abs(aspectRatio - 1)),
      compactness,
      symmetry * enclosure,
      spirality * complexity,
      angularity * pointCount / 8,
      enclosure * (1 - spirality),
      (1 - symmetry) * complexity,
    ]);

    const out = new Float32Array(32);
    for (let i = 0; i < 16; i++) out[i] = symVec[i] * 0.6 + (textVec[i] || 0) * 0.4;
    for (let i = 0; i < 16; i++) out[i + 16] = (textVec[i + 16] || 0) * 0.8 + symVec[i % 16] * 0.2;
    return this._normalize(out);
  },

  _textEmbedding(text) {
    const v = new Float32Array(32);
    if (!text) return v;
    const words = text.toLowerCase().split(/\s+/);
    const lexicon = {
      star: [0,0,0,0,0,0,0.8,0,0,0,0,0,0,0,0,0, 0.9,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0],
      fire: [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0.8,0,0,0,0,0,0,0,0,0,0,0,0,0,0],
      ice:  [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0.8,0,0,0,0,0,0,0,0,0,0,0,0,0],
      water:[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0.8,0,0,0,0,0,0,0,0,0,0,0,0],
      wind: [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0.8,0,0,0,0,0,0,0,0,0,0,0],
      earth:[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0.8,0,0,0,0,0,0,0,0,0,0],
      void: [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0.8,0,0,0,0,0,0,0,0,0],
      eye:  [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0.8,0,0,0,0,0,0,0,0],
      bomb:    [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0.9,0,0,0,0,0,0,0],
      cluster: [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0.7,0,0,0,0,0,0],
      frozen:  [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0.7,0,0,0,0,0,0,0,0.8,0,0,0,0,0],
      heaven:  [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0.8,0,0,0,0,0,0,0,0,0,0,0.7,0,0,0,0],
      abyss:   [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0.9,0,0,0,0,0,0.8,0,0,0],
      gravity: [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0.8,0,0],
      collapse:[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0.9,0,0],
      spiral:  [0,0.7,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0.6,0,0,0,0,0,0,0,0,0,0.8,0],
      chaos:   [0,0,0,0.7,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.9],
    };
    for (const w of words) {
      if (lexicon[w]) {
        for (let i = 0; i < 32; i++) v[i] = Math.max(v[i], lexicon[w][i]);
      } else {
        let h = 0;
        for (let c of w) h = (h * 31 + c.charCodeAt(0)) & 0xffffffff;
        v[((h >>> 0) % 16) + 16] = Math.max(v[((h >>> 0) % 16) + 16], 0.3);
      }
    }
    return v;
  },

  _normalize(v) {
    const mag = Math.sqrt(v.reduce((s, x) => s + x * x, 0)) || 1;
    return new Float32Array(Array.from(v).map(x => x / mag));
  },

  cosine(a, b) {
    let dot = 0;
    for (let i = 0; i < Math.min(a.length, b.length); i++) dot += a[i] * b[i];
    return dot;
  }
};

// ─── 3. BALANCING SYSTEM ────────────────────────────────────
export const BalancingSystem = {
  compute(features, resolverOutput) {
    if (!features) return { mana_cost: 10, stability: 0.9, instability_prob: 0.05, cooldown: 1.0 };
    const { complexity, symmetry, spirality, angularity, intersections, enclosure } = features;

    const ambiguity = resolverOutput?.ambiguity ?? 0.1;
    const base = resolverOutput?.base_power ?? 10;

    const mana_cost = Math.round(
      base
      + complexity * 30
      + ambiguity * 20
      + intersections * 2
      + spirality * 15
      - symmetry * 8
      - enclosure * 5
    );

    const stability = Math.max(0.05, Math.min(1,
      0.9 * symmetry
      - spirality * 0.3
      - complexity * 0.2
      - ambiguity * 0.15
      + enclosure * 0.1
    ));

    const instability_prob = Math.max(0, Math.min(0.8,
      (1 - symmetry) * 0.3
      + spirality * 0.25
      + angularity * (1 - enclosure) * 0.2
      + ambiguity * 0.15
    ));

    const cooldown = Math.max(0.5, mana_cost / 40);

    return {
      mana_cost: Math.max(5, mana_cost),
      stability: +stability.toFixed(2),
      instability_prob: +instability_prob.toFixed(2),
      cooldown: +cooldown.toFixed(1)
    };
  }
};

// ─── 4. EVOLUTION ENGINE ────────────────────────────────────
export const EvolutionEngine = {
  async reinforce(spell, db) {
    spell.usage_count++;
    spell.community_consensus = Math.min(1, Math.log10(spell.usage_count + 1) / 4);
    spell.stability = +(spell.stability * 0.9 + spell.community_consensus * 0.1).toFixed(3);
    if (spell.usage_count > 100) spell.mana_cost = Math.max(5, spell.mana_cost - 1);
    await db.upsert(spell);
  },

  checkVariant(existing, incoming, score) {
    if (score >= 0.82 && score < 0.92) {
      if (!existing.variants) existing.variants = [];
      if (!existing.variants.includes(incoming.spell_id)) {
        existing.variants.push(incoming.spell_id);
        incoming.variant_of = existing.spell_id;
      }
    }
  }
};

// ─── 5. SPELL DATABASE (Abstract) ──────────────────────────
export class SpellDatabase {
  async search(embedding, topK = 3) {
    throw new Error("SpellDatabase.search() must be implemented");
  }
  async upsert(spell) {
    throw new Error("SpellDatabase.upsert() must be implemented");
  }
  async get(id) {
    throw new Error("SpellDatabase.get() must be implemented");
  }
  async getAll() {
    throw new Error("SpellDatabase.getAll() must be implemented");
  }
  async size() {
    throw new Error("SpellDatabase.size() must be implemented");
  }
}

// ─── 6. SPELL CASTING ORCHESTRATOR ──────────────────────────
export const SIMILARITY_THRESHOLDS = { REUSE: 0.92, VARIANT: 0.82, RELATED: 0.65 };

export async function castSpell({ strokes, incantation, db, resolver }) {
  const features = FeatureExtractor.extract(strokes || []);
  const embedding = EmbeddingGenerator.generate(features, incantation);

  const results = await db.search(embedding, 3);
  const best = results[0];

  let spell, matchType, matchScore;

  if (best && best.score >= SIMILARITY_THRESHOLDS.REUSE) {
    spell = await db.get(best.id);
    matchType = "REUSE";
    matchScore = best.score;
    await EvolutionEngine.reinforce(spell, db);
  } else {
    const resolved = await resolver(features, incantation);
    if (!resolved) return null;

    const balancing = BalancingSystem.compute(features, resolved);

    const id = `spell_${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 7)}`;
    spell = {
      spell_id: id,
      symbol_embedding: Array.from(embedding),
      semantic_tags: resolved.semantic_tags || [],
      spell_name: resolved.spell_name,
      element: resolved.element,
      force: resolved.force,
      scale: resolved.scale,
      delivery: resolved.delivery,
      intent: resolved.intent,
      effect_description: resolved.effect_description,
      lore_hint: resolved.lore_hint,
      grammar_analysis: resolved.grammar_analysis,
      misfire_effect: resolved.misfire_effect,
      mana_cost: balancing.mana_cost,
      stability: balancing.stability,
      instability_prob: balancing.instability_prob,
      cooldown: balancing.cooldown,
      usage_count: 1,
      community_consensus: 0,
      variants: [],
      variant_of: null,
      created_at: Date.now(),
      incantation: incantation || null,
      features: {
        symmetry: features?.symmetry ?? 0,
        enclosure: features?.enclosure ?? 0,
        spirality: features?.spirality ?? 0,
        complexity: features?.complexity ?? 0,
        angularity: features?.angularity ?? 0,
      }
    };

    if (best && best.score >= SIMILARITY_THRESHOLDS.VARIANT) {
      const parent = await db.get(best.id);
      if (parent) EvolutionEngine.checkVariant(parent, spell, best.score);
      matchType = "VARIANT";
    } else if (best && best.score >= SIMILARITY_THRESHOLDS.RELATED) {
      matchType = "RELATED";
    } else {
      matchType = "NEW";
    }
    matchScore = best?.score ?? 0;

    await db.upsert(spell);
  }

  return { spell, matchType, matchScore, features };
}
