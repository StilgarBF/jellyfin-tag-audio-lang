#!/usr/bin/env python3
"""Build, test and package the plugin, and update the repository manifest.

Usage: python3 scripts/package.py [--skip-tests]

Reads name, guid, version, targetAbi, ... from build.yaml and writes
artifacts/audio-language-tagger_<version>.zip and manifest.json. The zip holds the
plugin DLL, thumb.png (assets/logo.png) and a meta.json, so a manual install shows the
same name, description and image as a catalog install. Upload the zip as a GitHub
release asset named v<version>; manifest.json points at that URL.
"""
import argparse
import hashlib
import json
import re
import subprocess
import sys
import zipfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / "Jellyfin.Plugin.AudioLangTagger" / "Jellyfin.Plugin.AudioLangTagger.csproj"
SOLUTION = ROOT / "Jellyfin.Plugin.AudioLangTagger.slnx"
REPO_URL = "https://github.com/StilgarBF/jellyfin-tag-audio-lang"
LOGO = ROOT / "assets" / "logo.png"
IMAGE_URL = "https://raw.githubusercontent.com/StilgarBF/jellyfin-tag-audio-lang/main/assets/logo.png"


def read_build_yaml(path):
    """Parse the small, flat build.yaml (scalars, folded '>' blocks, one list)."""
    data, key, folded = {}, None, []
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("---") or not line.strip():
            continue
        match = re.match(r"^(\w+):\s*(.*)$", line)
        if match and not line.startswith(" "):
            if key and folded:
                data[key] = " ".join(folded)
            key, value, folded = match.group(1), match.group(2).strip(), []
            if value == ">":
                continue
            data[key] = value.strip('"')
            key = None if value else key
        elif key:
            folded.append(line.strip())
    if key and folded:
        data[key] = " ".join(folded)
    return data


def run(*cmd):
    print("+", " ".join(str(c) for c in cmd), flush=True)
    subprocess.run(cmd, cwd=ROOT, check=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--skip-tests", action="store_true")
    args = parser.parse_args()

    meta = read_build_yaml(ROOT / "build.yaml")
    version = meta["version"]
    dotnet = ["dotnet"]

    if not args.skip_tests:
        run(*dotnet, "test", str(SOLUTION))

    run(*dotnet, "build", str(PROJECT), "-c", "Release", "--no-incremental",
        f"-p:Version={version}", f"-p:AssemblyVersion={version}", f"-p:FileVersion={version}")
    dll = PROJECT.parent / "bin" / "Release" / "net10.0" / "Jellyfin.Plugin.AudioLangTagger.dll"

    manifest_path = ROOT / "manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8")) if manifest_path.exists() else []
    entry = next((m for m in manifest if m["guid"] == meta["guid"]), None)
    # Keep the timestamp of an already packaged version so rebuilding gives the same zip.
    previous = next((v for v in (entry or {}).get("versions", []) if v["version"] == version), None)
    timestamp = previous["timestamp"] if previous else datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    plugin_meta = {
        "category": meta["category"], "changelog": meta["changelog"], "description": meta["description"],
        "guid": meta["guid"], "name": meta["name"], "overview": meta["overview"], "owner": meta["owner"],
        "targetAbi": meta["targetAbi"], "timestamp": timestamp, "version": version,
        "status": "Active", "autoUpdate": False, "imagePath": "thumb.png", "assemblies": [],
    }

    out = ROOT / "artifacts"
    out.mkdir(exist_ok=True)
    zip_name = f"audio-language-tagger_{version}.zip"
    zip_path = out / zip_name
    files = [
        (dll.name, dll.read_bytes()),
        ("thumb.png", LOGO.read_bytes()),
        ("meta.json", (json.dumps(plugin_meta, indent=2) + "\n").encode("utf-8")),
    ]
    # Fixed file timestamps so rebuilding the same inputs gives the same checksum.
    with zipfile.ZipFile(zip_path, "w") as archive:
        for name, data in files:
            info = zipfile.ZipInfo(name, date_time=(2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, data)
    checksum = hashlib.md5(zip_path.read_bytes()).hexdigest()

    entry = next((m for m in manifest if m["guid"] == meta["guid"]), None)
    if entry is None:
        entry = {"guid": meta["guid"], "versions": []}
        manifest.append(entry)
    entry.update(name=meta["name"], description=meta["description"], overview=meta["overview"],
                 owner=meta["owner"], category=meta["category"], imageUrl=IMAGE_URL)
    entry["versions"] = [v for v in entry["versions"] if v["version"] != version]
    entry["versions"].insert(0, {
        "version": version,
        "changelog": meta["changelog"],
        "targetAbi": meta["targetAbi"],
        "sourceUrl": f"{REPO_URL}/releases/download/v{version}/{zip_name}",
        "checksum": checksum,
        "timestamp": timestamp,
    })
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")

    print(f"\nPackage:  {zip_path}\nMD5:      {checksum}\nManifest: {manifest_path}")


if __name__ == "__main__":
    sys.exit(main())
