# JSON Journal POC — реализация

Статус: код реализован, офлайн-тесты пройдены, ожидает натурных
испытаний на сетевом ресурсе (этапы A.13–A.17 ROADMAP).

Соответствует: ADR-014, ROADMAP «Приоритет 1 — JSON Journal POC»,
этапы A.1–A.12 (код) + A.15/A.16 (частично, офлайн).

---

## 1. Что реализовано

### Модели данных

| Файл | Назначение |
| --- | --- |
| `Models/JournalEvent.cs` | Контракт события внешнего журнала (A.1): `SchemaVersion`, `EventId`, `ProjectId`, `ModelId`, `ModelName`, `Author`, `CreatedUtc`, `EventType`, `Message`, `RecordId`, `TargetModels[]`, `Elements[]` |
| `Models/ChangeElementReference.cs` | Элемент события (A.2): `UniqueId` (основной), `ElementId` (дополнительный), `SourceModel`, `Category`, `Name` |
| `Models/JournalLocation.cs` | Расположение журнала: `JournalRoot`, `ProjectId`, `ModelId`, `ModelName` |
| `Models/JournalProcessedState.cs` | Локальное состояние обработки (A.11) |

Дополнительно к контракту ADR-014 добавлено поле `RecordId` —
идентификатор исходной записи `ChangeRecord` из Extensible Storage.
Оно не даёт одной записи появиться дважды (два канала — ES и JSON)
в окне журнала и в уведомлениях.

### Сервисы

| Файл | Назначение | Этап |
| --- | --- | --- |
| `Services/IJournalWriter.cs` | Интерфейс записи backend (A.3) | A.3 |
| `Services/IJournalReader.cs` | Интерфейс чтения backend (A.3) | A.3 |
| `Services/JsonJournalWriter.cs` | Атомарная запись: `.tmp` → записать → закрыть → rename → `.json`; уникальное имя `yyyyMMddTHHmmssfff_<eventId>.json`; замер длительности в лог (A.6, A.7, A.17) | A.6, A.7 |
| `Services/JsonJournalReader.cs` | Чтение только актуальных датных каталогов; игнор `.tmp`; валидация JSON и `SchemaVersion`; пропуск битых файлов без падения Revit (A.9, A.15) | A.9 |
| `Services/JournalPathResolver.cs` | Определение корня `_BIMToolsJournal`: override → автопоиск вверх (до 3 уровней) → рядом с central-файлом; кэш результата; неудачные поиски кэшируются на 5 минут (A.4) | A.4, A.5 |
| `Services/JournalStateService.cs` | Локальное состояние `%AppData%\BIMTools\RevitCopyParams\JournalState.json`; атомарная запись; битый файл переименовывается в `.corrupt-*`; очистка старше 14 дней и старше 2000 записей (A.11) | A.11 |
| `Services/JournalEventMapper.cs` | `ChangeRecord` ↔ `JournalEvent` без Revit API (A.8) | A.8 |
| `Services/JournalPublishService.cs` | Dual Write: публикация записи в JSON-канал best-effort (A.8) | A.8 |
| `Services/JournalPollingService.cs` | Фоновый таймер 2–5 с; file I/O вне UI-потока; уведомление через WPF Dispatcher; дедупликация «при показе» (A.10, A.14) | A.10 |

### Конфигурация

| Файл | Назначение |
| --- | --- |
| `Config/JournalSettings.cs` | Настройки из `%AppData%\BIMTools\RevitCopyParams\JournalSettings.json` |
| `Config/BIMToolsPaths.cs` | Добавлены `JournalStateJsonPath`, `JournalSettingsJsonPath` |

---

## 2. Интеграционные точки

### App.cs

* `OnStartup` — `JournalPollingService.Start()`;
* `OnShutdown` — `JournalPollingService.Stop()`;
* `DocumentOpened`, `DocumentSynchronizedWithCentral`,
  `DocumentReloadedLatest`, `DocumentActivated` —
  `JournalPollingService.UpdateContext(document)`:
  контекст фиксируется на Revit API-потоке, background его
  только читает;
* `DocumentSynchronizedWithCentral` — автоперезагрузка связей
  (A.19) **отключена по умолчанию**. Включается настройкой
  `reloadLinksAfterSync: true`. `RevitLinkReloadService`
  сохранён как fallback-механизм.

### Окно журнала (ChangeJournalWindow)

* создание записи: ES-запись (как раньше) + JSON-событие
  `RecordCreated` (Dual Write, A.8);
* редактирование записи: ES + JSON-событие `RecordEdited`,
  получатели видят признак «изменено»;
* список окна = объединение: собственные ES-записи + ES-записи
  загруженных link + JSON-события, адресованные текущей модели
  (решение №2 «Архитектурных решений по журналу»).

### NotificationService

* добавлена проверка `JournalStateService.IsRecordProcessed`:
  запись, уже показанная через JSON-канал, не будет показана
  повторно через ES-канал (и наоборот) — дедупликация между
  каналами.

---

## 3. Правила дедупликации

1. Каждое JSON-событие имеет уникальный `EventId` (GUID).
2. Событие Dual Write несёт `RecordId` — идентификатор
   legacy-записи ES.
3. JSON-канал: событие пропускается, если его `EventId` или
   `RecordId` уже в локальном состоянии (`JournalState.json`).
4. ES-канал: запись пропускается, если её `Id` уже в
   `JournalState.json` (как `RecordId`).
5. Событие помечается обработанным **в момент показа уведомления**
   (решение POC): повторов нет даже после перезапуска Revit (A.14).
6. Собственные события (`Author` = имя пользователя Revit)
   не показываются как чужие.
7. В окне журнала JSON-события с `RecordId`, совпадающим с уже
   загруженной ES-записью, скрываются — дубли не появляются.

---

## 4. Настройки

Файл `%AppData%\BIMTools\RevitCopyParams\JournalSettings.json`.
Отсутствие файла = значения по умолчанию. Изменения применяются
после перезапуска Revit.

```json
{
  "jsonJournalEnabled": true,
  "pollingIntervalSeconds": 5,
  "lookbackDays": 3,
  "journalRootOverride": "",
  "reloadLinksAfterSync": false
}
```

| Поле | По умолчанию | Описание |
| --- | --- | --- |
| `jsonJournalEnabled` | `true` | Внешний JSON-канал включён |
| `pollingIntervalSeconds` | `5` | Интервал опроса (минимум 2, A.10) |
| `lookbackDays` | `3` | Глубина чтения датных каталогов (1–30) |
| `journalRootOverride` | пусто | Явный путь `\\server\share\...\_BIMToolsJournal`; пусто — автодетект |
| `reloadLinksAfterSync` | `false` | Автоперезагрузка связей после Sync (fallback, A.19) |

---

## 5. Определение корня журнала

Порядок (JournalPathResolver):

1. `journalRootOverride` из настроек;
2. автопоиск вверх от папки central-файла до 3 уровней:
   первая найденная `_BIMToolsJournal`;
3. иначе — `_BIMToolsJournal` рядом с central-файлом.

Примеры:

```text
\\server\share\05091-ЭФ25\AR\05091-ЭФ25_АР.rvt
→ корень \\server\share\05091-ЭФ25\_BIMToolsJournal (если есть)
→ ProjectId = 05091-ЭФ25, ModelId = AR, ModelName = 05091-ЭФ25_АР

\\server\share\05091-ЭФ25\05091-ЭФ25_АР.rvt
→ корень \\server\share\05091-ЭФ25\_BIMToolsJournal
→ ModelId = 05091-ЭФ25_АР (модель лежит в корне проекта)
```

Результат кэшируется на сессию; при недоступности сети поиск
повторяется не чаще, чем раз в 5 минут, чтобы не замедлять
открытие документа и Sync (A.15).

Структура каталогов создаётся автоматически при первой записи:

```text
_BIMToolsJournal\
└── Events\
    └── YYYY\
        └── MM\
            └── DD\
                └── Model\
                    └── 20260908T140501123_7f3a2b42....json
```

---

## 6. Формат файла события

```json
{
  "schemaVersion": 1,
  "eventId": "7f3a2b42-2d52-4b72-9af2-123456789abc",
  "projectId": "05091-ЭФ25",
  "modelId": "AR",
  "modelName": "05091-ЭФ25_АР",
  "author": "User1",
  "createdUtc": "2026-09-08T14:05:01.123Z",
  "eventType": "RecordCreated",
  "message": "Изменена схема вентиляции",
  "recordId": "5d2c...",
  "targetModels": [ "KR", "OV" ],
  "elements": []
}
```

Поля сериализуются в camelCase; чтение регистронезависимо.
`Elements[]` в POC пуст — контракт готов к Sprint 5 (A.18).

---

## 7. Офлайн-тестирование (этап A.12)

Ядро (Writer, Reader, State, Mapper) не зависит от Revit API
и протестировано вне Revit на .NET 8 (C# 7.3 — язык целевой
сборки net48). Результат: **50 проверок, 0 ошибок**.

Проверено:

* round-trip события с кириллицей;
* атомарная публикация: ровно один `.json`, `.tmp` не остаётся;
* структура `Events\YYYY\MM\DD\Model` создаётся автоматически;
* фильтрация по модели-получателю (чужие события не читаются);
* битый JSON → log → skip, валидный JSON обрабатывается;
* чужая `SchemaVersion` пропускается;
* неполное событие без `TargetModels` не ломает reader;
* `.tmp` игнорируется reader'ом;
* уникальность имён файлов, сортировка по времени;
* недоступный корень журнала → пусто, без исключений (A.15);
* маппинг `ChangeRecord` ↔ `JournalEvent` (дедуп-идентификатор,
  признак редактирования);
* повреждённый `JournalState.json` → `.corrupt-*`, работа
  продолжается (A.11);
* отметки обработки и дедуп `EventId`/`RecordId` (A.14);
* очистка состояния по сроку 14 дней (A.11);
* конкурентная запись 8 потоков: все файлы валидны, перезаписи
  нет, все `EventId` уникальны (A.16).

Стенд: `scripts/journal-test` (не входит в сборку плагина).

---

## 8. Что проверить в Revit (натурные испытания)

Этапы A.13–A.17 выполняются на тестовом проекте `05091-ЭФ25`
(сначала только АР и КР):

1. Открыть АР, создать запись в журнале → на сетевом ресурсе
   появился `...\Events\YYYY\MM\DD\AR\<...>.json` (A.13).
2. Открыть КР → в течение интервала polling появилось
   уведомление; в логе `JournalDiagnostics.log` —
   `JOURNAL POLL | Fresh=1` (A.14).
3. Убедиться, что Reload АР не выполняется
   (`LINK RELOAD SKIPPED | Reason=DisabledBySettings`) (A.14).
4. Собственная запись в КР не показывается как чужая; повторный
   опрос не создаёт дубликат уведомления (A.14).
5. Недоступность сетевого ресурса: Sync не ломается, ошибки —
   в логе; после восстановления сети записи появляются (A.15).
6. Перезапуск Revit: показанные события не показываются повторно;
   накопленные за время выключения — показываются (A.15).
7. Повреждённый JSON на сетевом ресурсе: Revit не падает,
   остальные события обрабатываются (A.15).
8. 2–4 одновременных клиента: записи из всех моделей доставлены (A.16).
9. Замерить: длительность записи JSON, длительность опроса,
   latency «CreatedUtc → показ уведомления» (A.17).
10. Окно «Журнал»: объединённый список ES + JSON без дублей;
    статусы Mine/New/Read корректны.

---

## 9. Известные ограничения POC

* Проверка `Directory.Exists` на недоступном сетевом ресурсе
  может занимать секунды. Поиск корня кэшируется, но первый
  неудачный резолв после запуска может замедлить открытие
  документа. Если это станет заметно — добавить таймаут
  через асинхронный резолв.
* Polling читает сетевые каталоги в background-потоке;
  при большой глубине `lookbackDays` на медленной сети возможны
  длинные циклы — циклы защищены от наложения флагом `busy`.
* Имя пользователя Revit — единственный признак «своей» записи.
  Два пользователя с одинаковым `Username` не увидят уведомлений
  друг друга (унаследовано от legacy-механики ES).
* Запись JSON fire-and-forget: если сеть отвалится в момент
  записи, событие этого Sync может не дойти. Это ожидаемое
  поведение best-effort канала; ES-канал остаётся fallback.
* Архивирование (A.20) не реализовано — отложено до накопления
  реальных данных.
