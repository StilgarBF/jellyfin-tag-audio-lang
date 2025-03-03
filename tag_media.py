#!/usr/bin/env python3
import os
import sys
import json
import argparse
import subprocess
import xml.etree.ElementTree as ET

# Language configuration: extendable for other languages
LANGUAGE_CONFIG = {
    'de': {
         'readable_name': "German",
         'search_patterns': ["German", "Deutsch", "DE", "De "],
         'tags_to_add': ["German", "Deutsch"]
    }
    # Add more languages here as needed.
}

# ANSI color codes
COLOR_GREEN = "\033[32m"
COLOR_RED = "\033[31m"
COLOR_ORANGE = "\033[38;5;208m"
COLOR_RESET = "\033[0m"

# Define video file extensions to consider
VIDEO_EXTENSIONS = {'.mkv', '.mp4', '.avi', '.mov', '.wmv', '.flv', '.mpeg', '.mpg'}

def ffprobe_audio_tracks(filepath):
    """
    Run ffprobe on the file and return audio track data (list of dicts).
    Each dict represents a stream; we check for a title tag.
    """
    cmd = [
        'ffprobe', '-v', 'error',
        '-select_streams', 'a',
        '-show_entries', 'stream=index:stream_tags=title',
        '-of', 'json',
        filepath
    ]
    try:
        proc = subprocess.run(cmd, capture_output=True, text=True, check=True)
        data = json.loads(proc.stdout)
        return data.get("streams", [])
    except FileNotFoundError:
        print("Error: 'ffprobe' not found. Please install ffmpeg (which includes ffprobe) and ensure it is in your PATH.", file=sys.stderr)
        return []
    except subprocess.CalledProcessError as e:
        print(f"ffprobe error on {filepath}: {e}", file=sys.stderr)
        return []
    except json.JSONDecodeError:
        print(f"Could not parse ffprobe output for {filepath}", file=sys.stderr)
        return []

def update_nfo(nfo_path, tag_to_add, dry_run=False, debug=False):
    """
    Create or update an NFO file at nfo_path.
    If the file exists, try to find or add a <tags> element, then add a new <tag> if not already present.
    If not, create a new XML structure.
    """
    if os.path.exists(nfo_path):
        try:
            tree = ET.parse(nfo_path)
            root = tree.getroot()
        except ET.ParseError:
            # If parsing fails, start a new structure
            root = ET.Element("movie")
            tree = ET.ElementTree(root)
    else:
        # Create new NFO structure if file does not exist
        root = ET.Element("movie")
        tree = ET.ElementTree(root)
    
    # Check if the tag already exists directly under the root element
    tag_exists = any(child.tag == "tag" and child.text == tag_to_add for child in root)
    if tag_exists:
        if debug:
            print(f"{COLOR_GREEN}Tag '{tag_to_add}' already exists in {nfo_path}{COLOR_RESET}")
        return False  # No update needed

    # Add the new tag element directly to the root element (<movie>)
    new_tag_elem = ET.SubElement(root, "tag")
    new_tag_elem.text = tag_to_add

    if dry_run:
        print(f"{COLOR_ORANGE}[DRY-RUN]{COLOR_RESET} {COLOR_GREEN}Would update {nfo_path} with tag: {tag_to_add}{COLOR_RESET}")
    else:
        tree.write(nfo_path, encoding="utf-8", xml_declaration=True)
        print(f"{COLOR_GREEN}Updated {nfo_path} with tag: {tag_to_add}{COLOR_RESET}")
    return True

def process_video_file(filepath, dry_run=False, debug=False, language='de'):
    """
    Process a single video file: inspect audio tracks, filename, and folder name for language tags.
    If any audio track title, the filename, or the folder name contains one of the search patterns,
    update the corresponding NFO file with language tags.
    """
    config = LANGUAGE_CONFIG.get(language, LANGUAGE_CONFIG['de'])
    search_patterns = config['search_patterns']
    tags_to_add = config['tags_to_add']
    streams = ffprobe_audio_tracks(filepath)
    found_lang = False
    readable_name = config['readable_name']

    # Check each audio track's title.
    for stream in streams:
        title = ""
        if "tags" in stream and "title" in stream["tags"]:
            title = stream["tags"]["title"]
        if debug:
            print(f"File: {filepath} - Audio Track {stream.get('index', '?')}, Title: '{title}'")
        if any(pattern in title for pattern in search_patterns):
            found_lang = True
            if debug:
                print(f"{COLOR_GREEN}Found pattern in audio track title: '{title}'{COLOR_RESET}")

    # Also check the filename.
    file_name = os.path.basename(filepath)
    if any(pattern in file_name for pattern in search_patterns):
        found_lang = True
        if debug:
            print(f"{COLOR_GREEN}Found pattern in file name: '{file_name}'{COLOR_RESET}")

    # Also check the folder name.
    folder_name = os.path.basename(os.path.dirname(filepath))
    if any(pattern in folder_name for pattern in search_patterns):
        found_lang = True
        if debug:
            print(f"{COLOR_GREEN}Found pattern in folder name: '{folder_name}'{COLOR_RESET}")

    if found_lang:
        nfo_path = os.path.join(os.path.dirname(filepath), "movie.nfo")
        for tag in tags_to_add:
            update_nfo(nfo_path, tag, dry_run, debug)
        print(f"{COLOR_GREEN}{readable_name} media found in {filepath}{COLOR_RESET}")
    else:
        print(f"{COLOR_RED}No {readable_name} media found in {filepath}{COLOR_RESET}")

def main(args):
    if args.dry_run:
        print(f"{COLOR_ORANGE}===== Dry-Run, this will not write anything. ====={COLOR_RESET}")
    else:
        print(f"{COLOR_ORANGE}===== Hot-Run, this will write tags to movie.nfo files. ====={COLOR_RESET}")
    for root_dir, dirs, files in os.walk(args.path):
        for filename in files:
            ext = os.path.splitext(filename)[1].lower()
            if ext in VIDEO_EXTENSIONS:
                filepath = os.path.join(root_dir, filename)
                process_video_file(filepath, dry_run=args.dry_run, debug=args.debug, language=args.language)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(
        description="Recursively tag video media based on audio track titles, filename, or folder name containing patterns: '{readable_name}', 'Deutsch', 'DE', or 'De '."
    )
    parser.add_argument("path", nargs="?", default=".", help="Root media folder path (default: current directory)")
    parser.add_argument("--dry-run", action="store_true", help="Do not write changes, only show what would be updated")
    parser.add_argument("--debug", action="store_true", help="Print debug information about audio tracks, filename, and folder name")
    parser.add_argument("--language", default="de", help="Language configuration to use (default: de)")
    args = parser.parse_args()
    
    main(args)
