class_name FeatureExtractor
extends RefCounted

static func extract(strokes: Array) -> Dictionary:
	if strokes.is_empty():
		return {}
	var all_input: Array = []
	for s in strokes:
		all_input.append_array(s)
	if all_input.size() < 2:
		return {}

	var simplified: Array = []
	for stroke in strokes:
		simplified.append(rdp(stroke, 2.0))
	var norm: Array = _normalize(simplified)
	var all: Array = []
	for s in norm:
		all.append_array(s)
	if all.size() < 2:
		return {}

	var xs: Array = all.map(func(p): return p["x"])
	var ys: Array = all.map(func(p): return p["y"])
	var min_x: float = xs.min()
	var max_x: float = xs.max()
	var min_y: float = ys.min()
	var max_y: float = ys.max()
	var width: float = max(max_x - min_x, 1.0)
	var height: float = max(max_y - min_y, 1.0)

	return {
		"symmetry": _symmetry(all),
		"enclosure": _enclosure(norm),
		"spirality": _spirality(all),
		"angularity": _angularity(norm),
		"complexity": _complexity(norm),
		"point_count": _detect_points(all),
		"dir_bias": _direction_bias(all),
		"intersections": _intersection_count(norm),
		"stroke_count": norm.size(),
		"aspect_ratio": width / height,
		"compactness": _compactness(all),
		"normalized_strokes": norm
	}

static func rdp(points: Array, epsilon: float = 2.0) -> Array:
	if points.size() < 3:
		return points.duplicate(true)
	var max_dist: float = 0.0
	var idx: int = 0
	var end: int = points.size() - 1
	for i in range(1, end):
		var d := _perpendicular_dist(points[i], points[0], points[end])
		if d > max_dist:
			max_dist = d
			idx = i
	if max_dist <= epsilon:
		return [points[0], points[end]]

	var left: Array = rdp(points.slice(0, idx + 1), epsilon)
	var right: Array = rdp(points.slice(idx), epsilon)
	var merged: Array = left.slice(0, left.size() - 1)
	merged.append_array(right)
	return merged

static func _perpendicular_dist(p: Dictionary, a: Dictionary, b: Dictionary) -> float:
	var dx: float = b["x"] - a["x"]
	var dy: float = b["y"] - a["y"]
	if absf(dx) < 0.000001 and absf(dy) < 0.000001:
		return _hypot(p["x"] - a["x"], p["y"] - a["y"])
	var t: float = ((p["x"] - a["x"]) * dx + (p["y"] - a["y"]) * dy) / (dx * dx + dy * dy)
	return _hypot(p["x"] - (a["x"] + t * dx), p["y"] - (a["y"] + t * dy))

static func _normalize(strokes: Array) -> Array:
	var all: Array = []
	for s in strokes:
		all.append_array(s)
	if all.is_empty():
		return strokes.duplicate(true)
	var xs: Array = all.map(func(p): return p["x"])
	var ys: Array = all.map(func(p): return p["y"])
	var min_x: float = xs.min()
	var max_x: float = xs.max()
	var min_y: float = ys.min()
	var max_y: float = ys.max()
	var cx: float = (min_x + max_x) / 2.0
	var cy: float = (min_y + max_y) / 2.0
	var scale: float = max(max_x - min_x, max_y - min_y)
	if scale < 0.000001:
		scale = 1.0

	var out: Array = []
	for s in strokes:
		var ns: Array = []
		for p in s:
			ns.append({
				"x": ((p["x"] - cx) / scale) * 64.0,
				"y": ((p["y"] - cy) / scale) * 64.0
			})
		out.append(ns)
	return out

static func _symmetry(points: Array) -> float:
	if points.is_empty():
		return 0.0
	var score: float = 0.0
	for p in points:
		var mirror := {"x": -p["x"], "y": p["y"]}
		var nearest: float = INF
		for q in points:
			nearest = min(nearest, _hypot(q["x"] - mirror["x"], q["y"] - mirror["y"]))
		score += max(0.0, 1.0 - nearest / 20.0)
	return score / float(points.size())

static func _enclosure(strokes: Array) -> float:
	for s in strokes:
		if s.size() < 4:
			continue
		var first: Dictionary = s[0]
		var last: Dictionary = s[s.size() - 1]
		if _hypot(first["x"] - last["x"], first["y"] - last["y"]) < 12.0:
			return 1.0
	if strokes.size() > 1:
		var all: Array = []
		for s in strokes:
			all.append_array(s)
		if all.size() > 1:
			var f: Dictionary = all[0]
			var l: Dictionary = all[all.size() - 1]
			if _hypot(f["x"] - l["x"], f["y"] - l["y"]) < 15.0:
				return 0.7
	return 0.0

static func _spirality(points: Array) -> float:
	if points.size() < 6:
		return 0.0
	var angles: Array = []
	for i in range(1, points.size()):
		angles.append(atan2(points[i]["y"] - points[i - 1]["y"], points[i]["x"] - points[i - 1]["x"]))
	var total_turn: float = 0.0
	for i in range(1, angles.size()):
		var d: float = angles[i] - angles[i - 1]
		if d > PI:
			d -= TAU
		if d < -PI:
			d += TAU
		total_turn += d
	return min(1.0, absf(total_turn) / (TAU * 2.0))

static func _angularity(strokes: Array) -> float:
	var sharp := 0
	var total := 0
	for s in strokes:
		for i in range(1, s.size() - 1):
			var a: float = atan2(s[i]["y"] - s[i - 1]["y"], s[i]["x"] - s[i - 1]["x"])
			var b: float = atan2(s[i + 1]["y"] - s[i]["y"], s[i + 1]["x"] - s[i]["x"])
			var diff: float = absf(a - b)
			if diff > PI:
				diff = TAU - diff
			if diff > 0.5:
				sharp += 1
			total += 1
	return float(sharp) / float(total) if total > 0 else 0.0

static func _complexity(strokes: Array) -> float:
	var total_len: float = 0.0
	for s in strokes:
		for i in range(1, s.size()):
			total_len += _hypot(s[i]["x"] - s[i - 1]["x"], s[i]["y"] - s[i - 1]["y"])
	return min(1.0, total_len / 400.0)

static func _detect_points(points: Array) -> int:
	if points.size() < 5:
		return 0
	var extrema := 0
	for i in range(1, points.size() - 1):
		var d1: float = _hypot(points[i]["x"], points[i]["y"])
		var d0: float = _hypot(points[i - 1]["x"], points[i - 1]["y"])
		var d2: float = _hypot(points[i + 1]["x"], points[i + 1]["y"])
		if d1 > d0 and d1 > d2 and d1 > 15.0:
			extrema += 1
	return min(extrema, 12)

static func _direction_bias(points: Array) -> float:
	if points.is_empty():
		return 0.0
	var cx: float = 0.0
	var cy: float = 0.0
	for p in points:
		cx += p["x"]
		cy += p["y"]
	cx /= float(points.size())
	cy /= float(points.size())
	return atan2(cy, cx) / PI

static func _intersection_count(strokes: Array) -> int:
	var segs: Array = []
	for s in strokes:
		for i in range(0, s.size() - 1):
			segs.append([s[i], s[i + 1]])
	var count := 0
	for i in range(segs.size()):
		for j in range(i + 2, segs.size()):
			if _seg_intersect(segs[i][0], segs[i][1], segs[j][0], segs[j][1]):
				count += 1
	return min(count, 20)

static func _seg_intersect(a: Dictionary, b: Dictionary, c: Dictionary, d: Dictionary) -> bool:
	var det: float = (b["x"] - a["x"]) * (d["y"] - c["y"]) - (b["y"] - a["y"]) * (d["x"] - c["x"])
	if absf(det) < 0.001:
		return false
	var t: float = ((c["x"] - a["x"]) * (d["y"] - c["y"]) - (c["y"] - a["y"]) * (d["x"] - c["x"])) / det
	var u: float = -((a["x"] - c["x"]) * (b["y"] - a["y"]) - (a["y"] - c["y"]) * (b["x"] - a["x"])) / det
	return t > 0.01 and t < 0.99 and u > 0.01 and u < 0.99

static func _compactness(points: Array) -> float:
	if points.size() < 3:
		return 0.0
	var xs: Array = points.map(func(p): return p["x"])
	var ys: Array = points.map(func(p): return p["y"])
	var area: float = (xs.max() - xs.min()) * (ys.max() - ys.min())
	var perim: float = 0.0
	for i in range(1, points.size()):
		perim += _hypot(points[i]["x"] - points[i - 1]["x"], points[i]["y"] - points[i - 1]["y"])
	if perim < 0.000001:
		return 0.0
	return min(1.0, (4.0 * PI * area) / (perim * perim))

static func _hypot(x: float, y: float) -> float:
	return sqrt(x * x + y * y)
