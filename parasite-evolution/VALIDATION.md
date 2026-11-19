# Перевірка

## Sprint 01 (Claude, 19.11.2026, гілка `claude/parasite-sprint-01`)

| Перевірка | Де | Результат |
| --- | --- | --- |
| Структура, UTF-8/LF, JSON/XML, `.meta` GUID (`Tools/check_source.py`) | локально + GitHub Actions | **Пройдено**: 55 файлів, 29 GUID, 0 помилок |
| NUnit EditMode-тести без UnityEngine на .NET 8 (`Tools/GameplayCoreTests`) | локально + GitHub Actions | **46/46 пройдено**: кінцівки/хода, ривок, Наглядач, мутації та наявні `EconomyTests.cs` |
| Контроль якості тестів: навмисні поломки логіки (ліміт кінцівок у повітрі, перельот ривка, атака в сейв-зоні, підйом кроку, рівень мутації) | локально | **5/5 поломок виявлено** тестами |
| Компіляція всіх runtime-скриптів проти власних заглушок Unity API (`Tools/UnityApiStubCheck`, warnings-as-errors) | локально + GitHub Actions | **Пройдено**. Контроль: навмисна помилка типу й неоднозначність `Random` виявлені. Початкові скрипти GPT теж компілюються без помилок |
| Unity-імпорт, справжня компіляція з UnityEngine 6000.3.11f1 | — | **Не виконано** |
| Unity Test Runner (EditMode/PlayMode), `ProfileStoreTests` | — | **Не виконано** (потрібен Unity `JsonUtility`) |
| Рендер, FPS, алокації в профайлері, мультитач на телефоні | — | **Не виконано** |
| Android IL2CPP APK | — | **Не виконано**: workflow підготовлено, але потрібні ліцензійні секрети власника ([BUILD_WITHOUT_PC.md](BUILD_WITHOUT_PC.md)) |

Заглушки (`Tools/UnityApiStubCheck/Stubs`) написані вручну за публічними сигнатурами й нічого не виконують. Успішна збірка доводить коректність C#-коду щодо цих сигнатур, а не сумісність зі справжніми збірками Unity.

`docs/parasite-evolution/slime_preview_sprint01.png` — відтворення формул `SlimeVisual` у numpy/matplotlib (`Tools/Preview/slime_preview.py`), **не рендер Unity**.

Команди з кореня репозиторію:

```bash
python3 parasite-evolution/Tools/check_source.py
dotnet test parasite-evolution/Tools/GameplayCoreTests/GameplayCoreTests.csproj -c Release
dotnet build parasite-evolution/Tools/UnityApiStubCheck/UnityApiStubCheck.csproj -c Release
```

## Перша реалізація (GPT, 15.11.2026)

Дата: 15.11.2026. Перевірка стосується вихідних файлів, не готової гри.

| Перевірка | Результат |
| --- | --- |
| Структура Unity-проєкту, JSON/XML, UTF-8/LF | Пройдено локально |
| `.meta`: наявність, унікальні GUID та відсутність осиротілих записів | Пройдено: 21 GUID |
| Синтаксичний розбір C# через tree-sitter-c-sharp | 12 файлів, 1345 рядків, синтаксичних помилок не знайдено |
| EditMode-тести економіки та файлового відновлення | Написано 21 тесткейс, **не виконано** |
| Package Manager / справжній lock-файл | Не виконано |
| Компіляція Unity / перевірка API та збірок | Не виконано |
| Запуск Editor-сцени й перевірка рендера | Не виконано |
| Android IL2CPP build / підпис APK | Не виконано |
| Встановлення на Android / FPS / нагрів / мультитач | Не виконано |

Команда відтворення перевірки структури з кореня репозиторію:

```bash
python parasite-evolution/Tools/check_source.py
```

Локальний результат: 40 перевірених файлів, 21 GUID, помилок не знайдено. Парсер C# перевіряє граматику; він не замінює компілятор, перевірку Unity API або виконання тестів.

У середовищі виконання не знайдено Unity Editor, `dotnet`, `csc` або `mcs`. Тому відсутні докази успішного імпорту, запуску або APK. Наступна обов'язкова перевірка — відкрити проєкт у зафіксованому підтримуваному Editor, виконати EditMode-тести й Android-збірку за [README](README.md).
