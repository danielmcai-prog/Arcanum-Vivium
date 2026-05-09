class_name BalancingSystem
extends RefCounted

static func compute(features: Dictionary, resolver_output: Dictionary) -> Dictionary:
	if features.is_empty():
		return {
			"mana_cost": 10,
			"stability": 0.9,
			"instability_prob": 0.05,
			"cooldown": 1.0
		}

	var ambiguity: float = resolver_output.get("ambiguity", 0.1)
	var base_power: float = resolver_output.get("base_power", 10)

	var mana_cost := int(round(
		base_power
		+ features.get("complexity", 0.0) * 30.0
		+ ambiguity * 20.0
		+ float(features.get("intersections", 0)) * 2.0
		+ features.get("spirality", 0.0) * 15.0
		- features.get("symmetry", 0.0) * 8.0
		- features.get("enclosure", 0.0) * 5.0
	))

	var stability: float = clampf(
		0.9 * features.get("symmetry", 0.0)
		- features.get("spirality", 0.0) * 0.3
		- features.get("complexity", 0.0) * 0.2
		- ambiguity * 0.15
		+ features.get("enclosure", 0.0) * 0.1,
		0.05,
		1.0
	)

	var instability: float = clampf(
		(1.0 - features.get("symmetry", 0.0)) * 0.3
		+ features.get("spirality", 0.0) * 0.25
		+ features.get("angularity", 0.0) * (1.0 - features.get("enclosure", 0.0)) * 0.2
		+ ambiguity * 0.15,
		0.0,
		0.8
	)

	var cooldown: float = max(0.5, float(mana_cost) / 40.0)
	return {
		"mana_cost": max(5, mana_cost),
		"stability": snappedf(stability, 0.01),
		"instability_prob": snappedf(instability, 0.01),
		"cooldown": snappedf(cooldown, 0.1)
	}
