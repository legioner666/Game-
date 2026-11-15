#!/usr/bin/env bash
# Install the official CLI only. Editor downloads and account sign-in are separate.
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cli_root="${XDG_DATA_HOME:-$HOME/.local/share}/parasite-unity-cli"
installer_file="$(mktemp)"
trap 'rm -f -- "$installer_file"' EXIT
if [[ ! -x "$cli_root/bin/unity" ]]; then
  curl --fail --silent --show-error --location --proto '=https' --tlsv1.2 \
    --connect-timeout 20 --max-time 180 \
    https://unity.com/install.sh --output "$installer_file"
  UNITY_CLI_HOME="$cli_root" UNITY_CLI_SKIP_SETUP=1 bash "$installer_file"
fi
"$cli_root/bin/unity" --version
python3 "$project_dir/Tools/check_source.py"
printf '\nCLI: %s\nProject: %s\n' "$cli_root/bin/unity" "$project_dir"
printf '%s\n' 'Editor, license, Unity tests and APK are not installed or run by this script.'
