# `.halalchain/`

Python services and shared local configuration for the HalalChain platform.

## Layout

```
.halalchain/
├── config.json           # Local shared configuration (placeholder; use env vars in prod)
├── ai-inference/         # Python FastAPI AI gateway
└── tawheed/              # Python FastAPI evidence + Policy Engine
```

The dot-prefix keeps these services visually grouped together as
"infrastructure-adjacent" Python, while the .NET solution remains the primary
workspace.

## Configuration

`config.json` mirrors the .NET `appsettings.json` schema. The non-development
rejection in `HalalChain.Platform.Api/Program.cs` still applies to anything
that loads this file into the API process, so never point a real environment
at this file. Use environment variables instead.
