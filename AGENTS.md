# Project Guidelines

## Project Structure

- `Jellyfin.Plugin.AudioLangTagger/` is a Jellyfin server plugin (C#, .NET 10) that tags movies, episodes and series with the languages of their audio tracks. See [README.md](README.md).
- `Jellyfin.Plugin.AudioLangTagger.Tests/` holds the xUnit tests.
- `build.yaml` holds the plugin metadata (`targetAbi` must match the Jellyfin server, currently 12.4.0.0).

## Toolchain

- The .NET 10 SDK is installed in `~/.dotnet`; run `export PATH=$HOME/.dotnet:$PATH` first.
- Build and test with `dotnet build` and `dotnet test` on `Jellyfin.Plugin.AudioLangTagger.slnx`.
- `python3 scripts/package.py` runs the tests, builds the release zip into `artifacts/` and updates `manifest.json` (see the Release section of the README). Bump `version` in `build.yaml` first.
- The config page runs inside Jellyfin's web client: do not use `document.createElement(tag, { is })` (it throws); declare `is="emby-..."` elements in markup.
- Jellyfin packages are referenced with `ExcludeAssets=runtime`, so tests must only touch code that does not load Jellyfin assemblies (the detection core).

## Safety

- Do not deploy to or modify the real Jellyfin server (192.168.178.121) without explicit confirmation; installing a plugin requires a restart.
- Test against a throwaway local Jellyfin with a few copied sample files, never against the real library.
- The media share is mounted at `/run/user/1000/gvfs/smb-share:server=ds415.local,share=medien` (`Videos/Movies`, `Videos/Serien`). Treat it as read-only.
