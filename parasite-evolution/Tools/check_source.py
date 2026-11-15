#!/usr/bin/env python3
"""Check project text, JSON and Unity GUIDs. This does not compile or run Unity."""
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

root = Path(__file__).resolve().parents[1]
errors = []
checked = 0
for folder in (root / "Assets", root / "Packages", root / "ProjectSettings"):
    for path in sorted(folder.rglob("*")):
        if not path.is_file() or path.suffix not in {".cs", ".meta", ".asmdef", ".json", ".xml", ".txt", ".asset"}:
            continue
        checked += 1
        try:
            data = path.read_bytes()
            text = data.decode("utf-8")
            if b"\r" in data or not data.endswith(b"\n"):
                errors.append(f"{path.relative_to(root)}: expected UTF-8/LF with final newline")
            if any(line.rstrip() != line for line in text.splitlines()):
                errors.append(f"{path.relative_to(root)}: trailing whitespace")
            if path.suffix in {".json", ".asmdef"}:
                json.loads(text)
            if path.suffix == ".xml":
                ET.fromstring(text)
        except (UnicodeError, ValueError, ET.ParseError) as exc:
            errors.append(f"{path.relative_to(root)}: {exc}")

guids = {}
for path in sorted((root / "Assets").rglob("*")):
    if path.name.endswith(".meta"):
        continue
    meta = Path(str(path) + ".meta")
    if not meta.is_file():
        errors.append(f"{path.relative_to(root)}: missing .meta")
        continue
    match = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(), re.M)
    if not match:
        errors.append(f"{meta.relative_to(root)}: invalid GUID")
        continue
    guid = match.group(1)
    if guid in guids:
        errors.append(f"duplicate GUID: {path} and {guids[guid]}")
    guids[guid] = str(path.relative_to(root))

for meta in (root / "Assets").rglob("*.meta"):
    if not Path(str(meta)[:-5]).exists():
        errors.append(f"{meta.relative_to(root)}: orphan metadata")

print(json.dumps({"static_files_checked": checked, "unity_guids": len(guids),
                  "errors": errors, "unity_import": "not run", "unity_tests": "not run",
                  "android_build": "not run"}, ensure_ascii=False))
sys.exit(bool(errors))
