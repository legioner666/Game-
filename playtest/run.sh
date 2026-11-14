#!/usr/bin/env bash
# Запускає бота на 3 рівнях + знімки з playtest/requests.txt у папку $1
set -u
OUT="$1"; mkdir -p "$OUT"
cd godot
godot --headless --import >/dev/null 2>&1 || true
: > "../$OUT/bot_report.txt"
for L in 0 1 2; do
  echo "=== level $((L+1)) ===" >> "../$OUT/bot_report.txt"
  timeout 400 godot --headless --path . -- --autoplay --fast=4 --level=$L --botlog 2>&1 | grep "^\[bot\]" >> "../$OUT/bot_report.txt"
done
grep -v '^#' ../playtest/requests.txt | while read -r L TS; do
  [ -z "${L:-}" ] && continue
  xvfb-run -a -s "-screen 0 1080x2400x24" timeout 300 godot --rendering-driver opengl3 --path . --resolution 1080x2400 -- --autoplay --fast=3 --direct --level="$L" --shots="L${L}@${TS}" >/dev/null 2>&1
done
mv L*_*.png "../$OUT/" 2>/dev/null || true
ls "../$OUT"
