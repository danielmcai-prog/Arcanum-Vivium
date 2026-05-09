class_name WhisperTranscriber
extends RefCounted

# Godot HTTPRequest is asynchronous; this helper builds request metadata so
# game code can execute the network call and pass back the transcript.
func build_whisper_request(api_key: String, wav_bytes: PackedByteArray) -> Dictionary:
	return {
		"url": "https://api.openai.com/v1/audio/transcriptions",
		"method": HTTPClient.METHOD_POST,
		"headers": [
			"Authorization: Bearer %s" % api_key,
			"Content-Type: multipart/form-data"
		],
		"file_name": "audio.wav",
		"file_bytes": wav_bytes,
		"model": "whisper-1",
		"language": "en"
	}
