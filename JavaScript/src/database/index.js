/**
 * SPELL ENGINE — Database Implementations
 * 
 * This file contains example implementations of the SpellDatabase interface.
 * Users can extend these or create their own for different backends.
 */

// ─── EXAMPLE 1: IN-MEMORY DATABASE (for development) ────────────────────
export class InMemorySpellDatabase {
  constructor() {
    this.spells = new Map();
    this.index = [];
  }

  async search(embedding, topK = 3) {
    if (!this.index.length) return [];
    return this.index
      .map(e => ({
        id: e.id,
        score: this._cosine(embedding, e.embedding)
      }))
      .sort((a, b) => b.score - a.score)
      .slice(0, topK);
  }

  async upsert(spell) {
    this.spells.set(spell.spell_id, spell);
    const existing = this.index.find(e => e.id === spell.spell_id);
    if (existing) {
      existing.embedding = spell.symbol_embedding;
    } else {
      this.index.push({ id: spell.spell_id, embedding: spell.symbol_embedding });
    }
  }

  async get(id) {
    return this.spells.get(id) || null;
  }

  async getAll() {
    return [...this.spells.values()];
  }

  async size() {
    return this.spells.size;
  }

  _cosine(a, b) {
    let dot = 0;
    for (let i = 0; i < Math.min(a.length, b.length); i++) dot += a[i] * b[i];
    return dot;
  }
}

// ─── EXAMPLE 2: FIREBASE DATABASE ──────────────────────────────────────────
/**
 * Firebase implementation requires:
 * - npm install firebase
 * 
 * Usage:
 * import { initializeApp } from 'firebase/app';
 * import { FirebaseSpellDatabase } from './spellDb';
 * 
 * const firebaseApp = initializeApp({ ... });
 * const db = new FirebaseSpellDatabase(firebaseApp);
 */
export class FirebaseSpellDatabase {
  constructor(firebaseApp) {
    this.app = firebaseApp;
    this.db = null;
    this.initDb();
  }

  initDb() {
    try {
      const { getFirestore } = require('firebase/firestore');
      this.db = getFirestore(this.app);
    } catch (e) {
      throw new Error('Firebase Firestore not initialized. Run: npm install firebase');
    }
  }

  async search(embedding, topK = 3) {
    if (!this.db) return [];
    try {
      const { collection, getDocs } = require('firebase/firestore');
      const querySnapshot = await getDocs(collection(this.db, 'spells'));
      const results = [];

      querySnapshot.forEach(doc => {
        const spell = doc.data();
        const score = this._cosine(embedding, new Float32Array(spell.symbol_embedding));
        results.push({ id: spell.spell_id, score, spell });
      });

      return results.sort((a, b) => b.score - a.score).slice(0, topK);
    } catch (e) {
      console.error('Search error:', e);
      return [];
    }
  }

  async upsert(spell) {
    if (!this.db) return;
    try {
      const { collection, doc, setDoc } = require('firebase/firestore');
      const spellRef = doc(collection(this.db, 'spells'), spell.spell_id);
      await setDoc(spellRef, spell, { merge: true });
    } catch (e) {
      console.error('Upsert error:', e);
    }
  }

  async get(id) {
    if (!this.db) return null;
    try {
      const { collection, doc, getDoc } = require('firebase/firestore');
      const docRef = doc(collection(this.db, 'spells'), id);
      const docSnap = await getDoc(docRef);
      return docSnap.exists() ? docSnap.data() : null;
    } catch (e) {
      console.error('Get error:', e);
      return null;
    }
  }

  async getAll() {
    if (!this.db) return [];
    try {
      const { collection, getDocs } = require('firebase/firestore');
      const querySnapshot = await getDocs(collection(this.db, 'spells'));
      const spells = [];
      querySnapshot.forEach(doc => spells.push(doc.data()));
      return spells;
    } catch (e) {
      console.error('GetAll error:', e);
      return [];
    }
  }

  async size() {
    return (await this.getAll()).length;
  }

  _cosine(a, b) {
    let dot = 0;
    const minLen = Math.min(a.length, b.length);
    for (let i = 0; i < minLen; i++) dot += a[i] * b[i];
    return dot;
  }
}

// ─── EXAMPLE 3: REST API DATABASE ──────────────────────────────────────────
/**
 * Generic REST API implementation
 * 
 * Usage:
 * const db = new RestApiSpellDatabase('https://api.myserver.com/spells');
 * 
 * Expects your API to implement:
 * - POST /spells/search { embedding, topK } → [ { id, score, spell } ]
 * - POST /spells { spell } → { ok: true }
 * - GET /spells/:id → { spell }
 * - GET /spells → [ spells ]
 * - GET /spells/count → { count }
 */
export class RestApiSpellDatabase {
  constructor(apiBaseUrl, headers = {}) {
    this.baseUrl = apiBaseUrl.replace(/\/$/, '');
    this.headers = { 'Content-Type': 'application/json', ...headers };
  }

  async search(embedding, topK = 3) {
    try {
      const res = await fetch(`${this.baseUrl}/search`, {
        method: 'POST',
        headers: this.headers,
        body: JSON.stringify({ embedding: Array.from(embedding), topK })
      });
      if (!res.ok) throw new Error(`Search failed: ${res.status}`);
      return await res.json();
    } catch (e) {
      console.error('Search error:', e);
      return [];
    }
  }

  async upsert(spell) {
    try {
      const res = await fetch(`${this.baseUrl}`, {
        method: 'POST',
        headers: this.headers,
        body: JSON.stringify(spell)
      });
      if (!res.ok) throw new Error(`Upsert failed: ${res.status}`);
    } catch (e) {
      console.error('Upsert error:', e);
    }
  }

  async get(id) {
    try {
      const res = await fetch(`${this.baseUrl}/${id}`, {
        method: 'GET',
        headers: this.headers
      });
      if (!res.ok) throw new Error(`Get failed: ${res.status}`);
      const data = await res.json();
      return data.spell || null;
    } catch (e) {
      console.error('Get error:', e);
      return null;
    }
  }

  async getAll() {
    try {
      const res = await fetch(`${this.baseUrl}`, {
        method: 'GET',
        headers: this.headers
      });
      if (!res.ok) throw new Error(`GetAll failed: ${res.status}`);
      return await res.json();
    } catch (e) {
      console.error('GetAll error:', e);
      return [];
    }
  }

  async size() {
    try {
      const res = await fetch(`${this.baseUrl}/count`, {
        method: 'GET',
        headers: this.headers
      });
      if (!res.ok) throw new Error(`Size failed: ${res.status}`);
      const data = await res.json();
      return data.count || 0;
    } catch (e) {
      console.error('Size error:', e);
      return 0;
    }
  }
}

// ─── EXAMPLE 4: SUPABASE DATABASE ──────────────────────────────────────────
/**
 * Supabase PostgreSQL implementation
 * 
 * Usage:
 * import { createClient } from '@supabase/supabase-js';
 * import { SupabaseSpellDatabase } from './spellDb';
 * 
 * const supabase = createClient(URL, KEY);
 * const db = new SupabaseSpellDatabase(supabase);
 * 
 * Note: Requires pgvector extension in Postgres for similarity search
 */
export class SupabaseSpellDatabase {
  constructor(supabaseClient) {
    this.supabase = supabaseClient;
  }

  async search(embedding, topK = 3) {
    try {
      // Use vector similarity (requires pgvector in Postgres)
      const { data, error } = await this.supabase.rpc('search_spells', {
        embedding: Array.from(embedding),
        top_k: topK
      });

      if (error) throw error;
      return data || [];
    } catch (e) {
      console.error('Search error:', e);
      return [];
    }
  }

  async upsert(spell) {
    try {
      const { error } = await this.supabase
        .from('spells')
        .upsert([spell], { onConflict: 'spell_id' });

      if (error) throw error;
    } catch (e) {
      console.error('Upsert error:', e);
    }
  }

  async get(id) {
    try {
      const { data, error } = await this.supabase
        .from('spells')
        .select('*')
        .eq('spell_id', id)
        .single();

      if (error) throw error;
      return data;
    } catch (e) {
      console.error('Get error:', e);
      return null;
    }
  }

  async getAll() {
    try {
      const { data, error } = await this.supabase.from('spells').select('*');
      if (error) throw error;
      return data || [];
    } catch (e) {
      console.error('GetAll error:', e);
      return [];
    }
  }

  async size() {
    try {
      const { count, error } = await this.supabase
        .from('spells')
        .select('*', { count: 'exact', head: true });

      if (error) throw error;
      return count || 0;
    } catch (e) {
      console.error('Size error:', e);
      return 0;
    }
  }

  _cosine(a, b) {
    let dot = 0;
    for (let i = 0; i < Math.min(a.length, b.length); i++) dot += a[i] * b[i];
    return dot;
  }
}
