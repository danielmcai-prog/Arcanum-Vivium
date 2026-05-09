class_name EmbeddingGenerator
extends RefCounted

static func generate(features: Dictionary, incantation_text: String = "") -> PackedFloat32Array:
	if features.is_empty():
		return PackedFloat32Array([0.0] * 32)

	var text_vec: PackedFloat32Array = _text_embedding(incantation_text)
	var sym_vec := PackedFloat32Array([
		features.get("symmetry", 0.0),
		features.get("enclosure", 0.0),
		features.get("spirality", 0.0),
		features.get("angularity", 0.0),
		features.get("complexity", 0.0),
		min(1.0, float(features.get("point_count", 0)) / 8.0),
		features.get("dir_bias", 0.0),
		min(1.0, float(features.get("intersections", 0)) / 10.0),
		min(1.0, float(features.get("stroke_count", 0)) / 5.0),
		min(1.0, absf(float(features.get("aspect_ratio", 1.0)) - 1.0)),
		features.get("compactness", 0.0),
		features.get("symmetry", 0.0) * features.get("enclosure", 0.0),
		features.get("spirality", 0.0) * features.get("complexity", 0.0),
		features.get("angularity", 0.0) * float(features.get("point_count", 0)) / 8.0,
		features.get("enclosure", 0.0) * (1.0 - features.get("spirality", 0.0)),
		(1.0 - features.get("symmetry", 0.0)) * features.get("complexity", 0.0)
	])

	var out := PackedFloat32Array()
	out.resize(32)
	for i in range(16):
		out[i] = sym_vec[i] * 0.6 + text_vec[i] * 0.4
	for i in range(16):
		out[i + 16] = text_vec[i + 16] * 0.8 + sym_vec[i % 16] * 0.2
	return _normalize(out)

static func cosine(a: PackedFloat32Array, b: PackedFloat32Array) -> float:
	var n := min(a.size(), b.size())
	var dot: float = 0.0
	for i in range(n):
		dot += a[i] * b[i]
	return dot

static func _text_embedding(text: String) -> PackedFloat32Array:
	var v := PackedFloat32Array([0.0] * 32)
	if text.is_empty():
		return v
	var words := text.to_lower().split(" ", false)
	var lexicon := {
		"star": _make_vec({6: 0.8, 16: 0.9}),
		"fire": _make_vec({17: 0.8}),
		"ice": _make_vec({18: 0.8}),
		"water": _make_vec({19: 0.8}),
		"wind": _make_vec({20: 0.8}),
		"earth": _make_vec({21: 0.8}),
		"void": _make_vec({22: 0.8}),
		"eye": _make_vec({23: 0.8}),
		"bomb": _make_vec({24: 0.9}),
		"cluster": _make_vec({25: 0.7}),
		"frozen": _make_vec({18: 0.7, 26: 0.8}),
		"heaven": _make_vec({16: 0.8, 27: 0.7}),
		"abyss": _make_vec({22: 0.9, 28: 0.8}),
		"gravity": _make_vec({29: 0.8}),
		"collapse": _make_vec({29: 0.9}),
		"spiral": _make_vec({1: 0.7, 20: 0.6, 30: 0.8}),
		"chaos": _make_vec({3: 0.7, 31: 0.9})
	}

	for word in words:
		if lexicon.has(word):
			var lv: PackedFloat32Array = lexicon[word]
			for i in range(32):
				v[i] = max(v[i], lv[i])
		else:
			var idx := abs(_stable_hash(word)) % 16 + 16
			v[idx] = max(v[idx], 0.3)
	return v

static func _make_vec(values: Dictionary) -> PackedFloat32Array:
	var out := PackedFloat32Array([0.0] * 32)
	for k in values.keys():
		var i: int = k
		if i >= 0 and i < out.size():
			out[i] = values[k]
	return out

static func _stable_hash(value: String) -> int:
	var h := 17
	for i in range(value.length()):
		h = int((h * 31 + value.unicode_at(i)) & 0x7fffffff)
	return h

static func _normalize(v: PackedFloat32Array) -> PackedFloat32Array:
	var mag: float = 0.0
	for x in v:
		mag += x * x
	mag = sqrt(mag)
	if mag < 0.000001:
		mag = 1.0
	var out := PackedFloat32Array()
	out.resize(v.size())
	for i in range(v.size()):
		out[i] = v[i] / mag
	return out
