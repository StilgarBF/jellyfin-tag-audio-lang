# Jellyfin Audio Language Tagger

The **Jellyfin Audio Language Tagger** is a Python script designed to parse your video library and automatically add language-specific tags to your media files. This makes it easier to filter and organize content based on the language of the audio tracks, helping users quickly find movies or shows in their preferred language.

## Features

- **Recursive Scanning:** Traverses your media directory and processes video files.
- **Audio Track Analysis:** Uses `ffprobe` to inspect audio tracks and identify language based on configurable search patterns.
- **NFO Tagging:** Automatically updates (or creates) a `movie.nfo` file in each media folder with language tags.
- **Dry-Run Mode:** Preview the changes without modifying any files.
- **Debug Mode:** Provides detailed output during processing.
- **Configurable:** Easily extendable language settings for adding support for additional languages.

## Requirements

- **Python 3**  
- **ffmpeg** (includes `ffprobe`)
- **Jellyfin Server:**  
  - Must have proper access rights to your media directory.
  - Library must be configured to use and store metadata in NFO files.

## Usage

1. **Prepare Your Environment:**
   - Ensure your Jellyfin server has proper access rights to the media directory so it can read the generated `movie.nfo` files.
   - Configure your Jellyfin library to use and store metadata in NFO files.

2. **Run the Script:**

   Open a terminal and run:

   ```bash
   python3 tag_media.py /path/to/media --language de --debug
   ```

   - `/path/to/media`: The root directory of your media library.
   - `--language de`: (Optional) Specifies the language configuration to use. The default is `de`.
   - `--dry-run`: (Optional) Use this flag to simulate the changes without writing any files.
   - `--debug`: (Optional) Enables detailed output to help you see what the script is doing.

3. **Update Jellyfin Metadata:**
   - After running the script, open your Jellyfin web interface.
   - Navigate to your library’s menu (using the three dots).
   - Click **"Refresh metadata"** and then **"Scan for new and updated files"** to load the new tags.

## Extending for Other Languages

The language-specific settings are stored at the top of the script in the `LANGUAGE_CONFIG` dictionary. For example, the default configuration for German (`de`) is:

   ```python
   LANGUAGE_CONFIG = {
       'de': {
            'readable_name': "German",
            'search_patterns': ["German", "Deutsch", "DE", "De "],
            'tags_to_add': ["German", "Deutsch"]
       }
       # Add more languages here as needed.
   }
   ```

To add another language:
- Add a new key with the language code.
- Define its `readable_name`, `search_patterns`, and `tags_to_add`.

Remember to pass the new language code via the `--language` parameter when running the script.

## License

This project is licensed under the [GNU General Public License v3 (GPLv3)](https://www.gnu.org/licenses/gpl-3.0.en.html).

---

Happy tagging and enjoy a more organized Jellyfin media library!
