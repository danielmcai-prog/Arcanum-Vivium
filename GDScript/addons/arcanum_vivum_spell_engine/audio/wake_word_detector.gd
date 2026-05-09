class_name WakeWordDetector
extends RefCounted

var _wake_word: String
var _buffer: String = ""

func _init(wake_word: String = "arcanum") -> void:
	_wake_word = wake_word.to_lower()

func process_text(text: String) -> bool:
	_buffer = (_buffer + text).to_lower()
	if _buffer.length() > 100:
		_buffer = _buffer.substr(_buffer.length() - 100, 100)
	return _buffer.contains(_wake_word)

func reset() -> void:
	_buffer = ""
