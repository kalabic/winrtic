# WinRTIC
Windows RealTime Interaction Console for OpenAI's realtime API.
## WinRTIC
A Windows command line application for interaction with OpenAI's realtime API.

It will read endpoint options from JSON formatted configuration file `winrtic_api.conf` in current directory or from environment variables (if conf file was not found). A sample API config file can be found in [documentation folder](docs).

Command line options are:
```
Usage:
  WinRTIC [<session file>] [options]

Arguments:
  <session file>  JSON formatted file with root object containing key/value pairs, all optional
                  - 'Instructions' - text prompt for assistant,
                  - 'Temperature' - decimal value between 0.6 and 1.2, (default: 0.7)
                  - 'ServerVAD' - object with optional key/value pairs. Server VAD means that the model will detect the
                  start and end of speech based on audio volume and respond at the end of user speech.
                      - 'Threshold' -  Activation threshold for VAD (0.0 to 1.0). A higher threshold will require
                  louder audio to activate the model, and might perform better in noisy environments., (default: 0.5)
                      - 'PrefixPadding' - Amount of audio to include before the VAD detected speech. Value in
                  miliseconds between 0 and 2000, (default: 300)
                      - 'SilenceDuration' - Duration of silence to detect speech stop. Value in miliseconds between 0
                  and 2000, (default: 500)

Options:
  --api <api>     File with API options. (OPENAI_API_KEY or AZURE options)
  --version       Show version information
  -?, -h, --help  Show help and usage information
```

## MiniRTIC
A minimum viable Windows command line console for interaction with OpenAI's realtime API.
Please provide one of following in your environment variables:
- AZURE_OPENAI_ENDPOINT with AZURE_OPENAI_USE_ENTRA=true or AZURE_OPENAI_API_KEY
- OPENAI_API_KEY

Project includes a free audio sample from [pixabay.com](https://pixabay.com) [^1].
## OpenRTIC
Realtime interaction class library.

[^1]: "Hello there" by [kittenstrike1](https://pixabay.com/users/kittenstrike1-35556891/),  https://pixabay.com/sound-effects/quothello-therequot-158832/
