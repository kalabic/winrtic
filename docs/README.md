The examples in this folder contain sample of JSON formatted configuration files.

### Example API configuration file:
```json
{
	"AZURE_OPENAI_ENDPOINT":"",
	"AZURE_OPENAI_API_KEY":"",
	"AZURE_OPENAI_DEPLOYMENT":"",
	"AZURE_OPENAI_USE_ENTRA":false,
	"OPENAI_API_KEY":""
}
```
Please provide one of:
- **AZURE_OPENAI_ENDPOINT** with **AZURE_OPENAI_API_KEY** and **AZURE_OPENAI_DEPLOYMENT** (or **AZURE_OPENAI_USE_ENTRA**=true)
- **OPENAI_API_KEY**

### Example session configuration file:
```json
{
	"Temperature":0.7,
	"ServerVAD":
	{
		"Threshold":0.4,
		"PrefixPadding":200,
		"SilenceDuration":800
	}
}
```

Supported parameters:
- **Instructions** - text prompt for assistant,
- **Temperature** - decimal value between 0.6 and 1.2, (default: 0.7)
- **ServerVAD** - Server VAD means that the model will detect the start and end of speech based on audio volume and respond at the end of user speech.
  - **Threshold** - Activation threshold for VAD (0.0 to 1.0). A higher threshold will require louder audio to activate the model, and might perform better in noisy environments. (default: 0.4)
  - **PrefixPadding** - Amount of audio to include before the VAD detected speech. Value in miliseconds between 0 and 2000. (default: 200)
  - **SilenceDuration** - Duration of silence to detect speech stop. Value in miliseconds between 0 and 2000, (default: 800)
