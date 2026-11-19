# Збірка Android APK без власного ПК

Дата перевірки: 19.11.2026. Ціль: ARM64 debug APK, Unity **6000.3.11f1**, IL2CPP, без платних послуг і без обходу ліцензування.

> Цей документ — результат дослідження та підготовлена конфігурація. **APK ще не зібрано**: жоден із варіантів нижче не запускався, бо для кожного потрібен вхід у Unity-акаунт власника.

## Порівняння

| Варіант | Безкоштовно | Що потрібно від власника | Обмеження | Висновок |
| --- | --- | --- | --- | --- |
| **Unity Build Automation (UBA)** | Безкоштовний рівень: 200 хв Windows (Micro), 100 хв Linux (Micro), 100 хв Mac на місяць, 25 ГБ сховища | Unity ID, браузер. Ліцензія Editor на машинах Unity — їхня | Хвилини Micro повільні; перша збірка з імпортом може забрати 30–60 хв. Після вичерпання ліміту доступ блокується, доки не додати картку — **автоматичного списання без картки немає**. Кілька збірок на місяць | **Рекомендовано першим кроком**: усе робиться з телефона, не потрібен `.ulf` |
| **GitHub Actions + GameCI** | Стандартні раннери безкоштовні й без ліміту хвилин для публічних репозиторіїв | Секрети `UNITY_LICENSE` (вміст `.ulf`), `UNITY_EMAIL`, `UNITY_PASSWORD` | `.ulf` для Personal видає лише Unity Hub після входу (один раз, на будь-якому комп'ютері). Образ Unity Android ~8,7 ГБ стиснутий — потрібне очищення диска | **Найкращий довгостроковий**: необмежені збірки, workflow уже готовий |
| **GitHub Codespaces** | 120 core-год/міс. (≈60 год на 2 ядрах), 15 ГБ-міс. сховища | Акаунт GitHub, ручне керування квотою | Editor + Android-модуль займають більшу частину 15 ГБ; Personal-активація потребує Unity Hub з GUI | Не рекомендовано як основний шлях; можливий лише як місце, де одноразово отримати `.ulf` (не перевірено) |

Факти перевірено за офіційними джерелами (посилання в кінці). Образ `unityci/editor:ubuntu-6000.3.11f1-android-3` існує на Docker Hub (перевірено API реєстру 19.11.2026, розмір шару ≈8,7 ГБ).

## Варіант A — Unity Build Automation (з телефона)

1. Увійти на [cloud.unity.com](https://cloud.unity.com) своїм Unity ID → **DevOps** → **Build Automation**. На вибір плану обрати безкоштовний (Start free trial / Free), **не додавати картку**.
2. Створити проєкт і підключити репозиторій GitHub `legioner666/Game-` (OAuth GitHub у браузері). Гілка: `claude/parasite-sprint-01` (або `gpt/parasite-evolution-design` після злиття).
3. Нова конфігурація збірки:
   - Platform: **Android**; Builder OS: Windows або Linux (Micro — безкоштовні хвилини).
   - Unity version: **6000.3.11f1**.
   - Project subfolder: **`parasite-evolution`**.
   - Advanced → **Pre-export method**: `ParasiteEvolution.Editor.PrototypeBuild.PrepareCloudBuild` — створює Bootstrap-сцену, URP-асети та Android-налаштування (ARM64, IL2CPP, API 26+, обидві альбомні орієнтації).
   - Android signing: debug (без власного keystore). Формат: APK.
4. Запустити збірку, дочекатися артефакту `.apk`, завантажити й встановити на телефон.
5. Стежити за лічильником хвилин у **Administration → Cost and Usage**. Не вмикати pay-as-you-go.

Невідомо до першого запуску: чи вистачить хвилин Micro на першу IL2CPP-збірку; чи потребує UBA прив'язки Cloud Project ID у `ProjectSettings` для цього репозиторію.

## Варіант B — GitHub Actions + GameCI (workflow уже в репозиторії)

Файл: [`.github/workflows/parasite-android-apk.yml`](../.github/workflows/parasite-android-apk.yml). Запуск лише вручну (**Actions → Parasite Evolution — Android debug APK → Run workflow**). Без секретів робота пропускається з поясненням; нічого платного не запускається.

Одноразово отримати `.ulf` (Unity Personal більше не підтримує ручну активацію через сайт):
1. На будь-якому комп'ютері (друг, робота, комп'ютерний клуб) встановити Unity Hub і увійти в свій Unity ID.
2. **Preferences → Licenses → Add → Get a free personal license**.
3. Знайти файл ліцензії: Windows `C:\ProgramData\Unity\Unity_lic.ulf`, macOS `/Library/Application Support/Unity/Unity_lic.ulf`, Linux `~/.local/share/unity3d/Unity/Unity_lic.ulf`.
4. На GitHub з телефона: **Settings → Secrets and variables → Actions → New repository secret**:
   - `UNITY_LICENSE` — увесь вміст `.ulf`;
   - `UNITY_EMAIL` — e-mail Unity ID;
   - `UNITY_PASSWORD` — пароль (GameCI радить пароль без спецсимволів).
5. Запустити workflow. APK з'явиться в артефакті `ParasiteEvolution-debug-apk` (зберігається 14 днів).

Що робить workflow: звільняє місце на диску раннера, кешує `Library`, запускає `game-ci/unity-builder@v4` з методом `ParasiteEvolution.Editor.PrototypeBuild.BuildAndroidCI` (читає `-customBuildPath`), завантажує APK. Секрети не виводяться в лог і не потрапляють у PR із форків.

Ризики: Personal-ліцензія з `.ulf` може потребувати повторного отримання після закінчення; перша збірка без кешу може тривати понад годину; якщо `free-disk-space` не звільнить достатньо місця — збірка впаде з «No space left on device».

## Чого ми не робимо

- Не вводимо облікові дані Unity за власника і не зберігаємо їх у коді.
- Не використовуємо неофіційні способи активації, чужі ліцензії чи неофіційні образи Editor.
- Не вмикаємо оплату, larger runners або платні хвилини.

## Джерела

- Unity Build Automation: https://docs.unity.com/en-us/build-automation
- Тарифи DevOps 2026 (безкоштовні хвилини, блокування без картки): https://unity.com/products/pricing-updates
- Пояснення нарахувань DevOps: https://support.unity.com/hc/en-us/articles/34748492914964-Understanding-Unity-DevOps-charges
- GameCI активація (Personal через Unity Hub): https://game.ci/docs/github/activation/
- GameCI builder: https://game.ci/docs/github/builder/
- Безкоштовні стандартні раннери для публічних репозиторіїв: https://docs.github.com/en/actions/reference/runners/github-hosted-runners
- Квоти Codespaces: https://docs.github.com/en/billing/reference/product-usage-included
- Системні вимоги Unity 6.3: https://docs.unity.com/en-us/engine/6000.3/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements
