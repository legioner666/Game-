#!/usr/bin/env bash
# Бот на 3 рівнях + знімки з playtest/requests.txt у папку $1. Чесний звіт: expected vs produced.
set -u
OUT="$1"; mkdir -p "$OUT"
ROOT="$(pwd)"
cd godot
godot --headless --import >/dev/null 2>&1 || true
: > "$ROOT/$OUT/bot_report.txt"
for L in 0 1 2; do
  echo "=== level $((L+1)) ===" >> "$ROOT/$OUT/bot_report.txt"
  timeout 400 godot --headless --path . -- --autoplay --fast=4 --level=$L --botlog 2>&1 | grep "^\[bot\]" >> "$ROOT/$OUT/bot_report.txt"
done
EXPECTED=0; PRODUCED=0
: > "$ROOT/$OUT/shots_log.txt"
while read -r L TS; do
  case "${L:-#}" in \#*) continue;; esac
  for T in ${TS//,/ }; do
    EXPECTED=$((EXPECTED+1))
    # --fast=6: кадр потрібен лише на ігровий час T; ui-режим показує кнопки спелів
    timeout 240 xvfb-run -a -s "-screen 0 1080x2400x24" godot --rendering-driver opengl3 --path . --resolution 1080x2400 -- --autoplay --ui --fast=6 --direct --level="$L" --shots="L${L}@${T}" > "shot.log" 2>&1
    RC=$?
    if [ -f "L${L}_${T}.png" ]; then
      PRODUCED=$((PRODUCED+1)); mv "L${L}_${T}.png" "$ROOT/$OUT/"
      echo "OK   L${L} t=${T}" >> "$ROOT/$OUT/shots_log.txt"
    else
      echo "FAIL L${L} t=${T} exit=${RC} (немає PNG)" >> "$ROOT/$OUT/shots_log.txt"
      tail -5 shot.log >> "$ROOT/$OUT/shots_log.txt"
    fi
  done
done < <(grep -v '^#' "$ROOT/playtest/requests.txt")
echo "expected=${EXPECTED} produced=${PRODUCED}" | tee -a "$ROOT/$OUT/shots_log.txt"
ls "$ROOT/$OUT"
[ "$EXPECTED" = "$PRODUCED" ]
