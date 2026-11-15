# Підпис релізу (Google Play App Signing)

Приватні ключі в репозиторії **не зберігаються**. Debug-ключ `.github/debug.keystore` публічний і не придатний для Play.

## 1. Створити upload-ключ (один раз, локально)
`bash tools/make_upload_key.sh` — створить `crownrush-upload.keystore` і виведе значення для секретів. Збережіть файл і паролі в менеджері паролів (втрата ключа = складне відновлення через Play Support).

## 2. Додати GitHub Secrets (Settings → Secrets → Actions)
- `ANDROID_RELEASE_KEYSTORE_B64` — base64 keystore
- `ANDROID_RELEASE_KEY_ALIAS` — alias (за замовчуванням `crownrush`)
- `ANDROID_RELEASE_KEYSTORE_PASSWORD` — пароль (однаковий для store і key)

## 3. Запустити workflow `Release AAB (Play candidate)`
Без секретів workflow збирає лише `…VALIDATION-debug-signed-NOT-FOR-PLAY.aab`. З секретами — `crown-rush-release-signed.aab` (артефакт запуску, не публічний реліз), перевірка: package, versionCode, targetSdk=36, не debuggable.

## 4. Завантаження
Play Console → Release → Internal testing → Create release → увімкнути Play App Signing → завантажити AAB. Production — лише після виконання вимог тестування акаунта.
