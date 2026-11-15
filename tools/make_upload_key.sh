#!/usr/bin/env bash
# Створює upload keystore для Google Play. Запускати ЛОКАЛЬНО; файл і паролі не коміттити.
set -euo pipefail
OUT="${1:-crownrush-upload.keystore}"
ALIAS="${ALIAS:-crownrush}"
read -r -s -p "Password (min 8 chars): " PASS; echo
keytool -genkeypair -v -keystore "$OUT" -alias "$ALIAS" -keyalg RSA -keysize 4096 -validity 10000 \
  -storepass "$PASS" -keypass "$PASS" -dname "CN=Crown Rush, O=<DEVELOPER_NAME>, C=UA"
echo "ANDROID_RELEASE_KEY_ALIAS=$ALIAS"
echo "ANDROID_RELEASE_KEYSTORE_B64 (copy the next line into the secret):"
base64 -w0 "$OUT"; echo
echo "ANDROID_RELEASE_KEYSTORE_PASSWORD = the password you typed"
echo "KEEP $OUT SAFE and NEVER commit it."
