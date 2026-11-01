# Jellyfin Audio Language Tagger

A Jellyfin plugin that tags movies, episodes and series with the languages of their audio tracks, so you can filter your library by tag (for example "German" / "Deutsch").

- **Reliable detection:** reads each audio stream's language tag (`ger`, `deu`, `de`, ...) first, then the stream title, the file name and the folder name. Commentary and audio-description tracks are ignored, and words like `CODEC` or `DEad` no longer match.
- **Automatic:** new and changed movies and episodes are tagged shortly after Jellyfin adds them.
- **Series too:** series are tagged from their episodes (any episode, every episode, or off).
- **Configurable in the dashboard:** languages, detection layers, series mode, dry run.
- **Safe:** only tags that belong to a configured language are ever added or removed; your own tags stay untouched.

## Requirements

- Jellyfin Server **12.4** or newer
- To keep the tags in `.nfo` files, enable the **Nfo** metadata saver in the library settings. The tags are stored in Jellyfin either way.

## Install

### Quick manual install

1. Download `audio-language-tagger_<version>.zip` from the [releases](https://github.com/StilgarBF/jellyfin-tag-audio-lang/releases).
2. Unpack it into a new folder named `AudioLangTagger_<version>` inside the Jellyfin `plugins` directory and restart Jellyfin. For a Debian/Ubuntu package install, including a Proxmox LXC:

   ```bash
   sudo mkdir -p /var/lib/jellyfin/plugins/AudioLangTagger_0.1.0.0
   sudo unzip audio-language-tagger_0.1.0.0.zip -d /var/lib/jellyfin/plugins/AudioLangTagger_0.1.0.0
   sudo chown -R jellyfin:jellyfin /var/lib/jellyfin/plugins/AudioLangTagger_0.1.0.0
   sudo systemctl restart jellyfin
   ```

   Other installs use a different data directory (Docker: the `/config/plugins` volume).

### Via plugin repository

In Jellyfin: **Dashboard → Plugins → Repositories → +**, add

```
https://raw.githubusercontent.com/StilgarBF/jellyfin-tag-audio-lang/main/manifest.json
```

then install **Audio Language Tagger** from the **Catalog** and restart Jellyfin.

## Use

1. Open **Dashboard → Plugins → Audio Language Tagger**.
2. Check the languages. German is preconfigured; add more with **Add language**.
3. Tag the existing library: tick **Dry run**, **Save**, click **Scan library now**, and read what would change in **Dashboard → Logs**. When it looks right, untick **Dry run**, **Save** and scan again.
4. From then on, new and changed items are tagged automatically.

New and changed items are tagged automatically, but items that are already in the library are only tagged by the scan. Jellyfin's regular library scan does not touch unchanged files. The scan is also available as the scheduled task **Tag audio languages** (Dashboard → Scheduled Tasks) if you want to run it on a schedule.

In Jellyfin, filter by the tags in a library under **Filters → Tags**.

### How a language is detected

Layers are checked from most to least reliable:

1. the audio stream **language tag**, matched against the ISO 639 codes of the language (alpha-2 and both alpha-3 forms; `de-AT` counts as `de`)
2. the audio stream **title**, as a whole word
3. the **file name**, as a whole word
4. the **folder name**, as a whole word

File and folder names are by default only used when no audio stream has a known language, so an English file in a folder called "German Edition" is not tagged. Words of two letters or fewer (such as `DE`) match only in capitals. Tracks whose title contains "commentary", "kommentar" or "audio description" are skipped.

## Build

Requires the .NET 10 SDK.

```bash
dotnet build Jellyfin.Plugin.AudioLangTagger.slnx
dotnet test Jellyfin.Plugin.AudioLangTagger.slnx
```

### Release

1. Set the new `version` in `build.yaml` and describe it in `changelog`.
2. Run `python3 scripts/package.py`. It runs the tests, builds the plugin, writes `artifacts/audio-language-tagger_<version>.zip` and updates `manifest.json`.
3. Commit `manifest.json` and `build.yaml`, tag `v<version>`, push, and create a GitHub release for that tag with the zip as its asset. The name must match the `sourceUrl` in `manifest.json`.

## License

This project is licensed under the [GNU General Public License v3 (GPLv3)](https://www.gnu.org/licenses/gpl-3.0.en.html).
