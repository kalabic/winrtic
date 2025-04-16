# WinRTIC
Windows RealTime Interaction Console for OpenAI's realtime API.
## WinRTIC
A Windows command line application for interaction with OpenAI's realtime API.

It will read endpoint options from JSON formatted configuration file `winrtic_api.conf` in current directory or from environment variables (if conf file was not found). A sample API config file can be found in the repository root.

There are additional command line options, but that is WIP.

## MiniRTIC
A minimum viable Windows command line console for interaction with OpenAI's realtime API.
Please provide one of following in your environment variables:
- AZURE_OPENAI_ENDPOINT with AZURE_OPENAI_USE_ENTRA=true or AZURE_OPENAI_API_KEY
- OPENAI_API_KEY

Project includes a free audio sample from [pixabay.com](https://pixabay.com) [^1].
## OpenRTIC
Realtime interaction class library.

[^1]: "Hello there" by [kittenstrike1](https://pixabay.com/users/kittenstrike1-35556891/),  https://pixabay.com/sound-effects/quothello-therequot-158832/
