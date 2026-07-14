# Проблема: Отсутствует TCP Lobby Server в репозитории

## Суть проблемы

Проект реализует двухуровневую клиент-серверную архитектуру:

1. **TCP Lobby Server** (порт 7700) — внешний matchmaker-сервис, который обрабатывает команды `CREATE` и `JOIN:CODE`, генерирует коды лобби и назначает UDP-порты для игровых сессий.
2. **Unity Dedicated Server** (Unity Netcode for GameObjects) — игровой сервер, запускаемый с флагами `-server -port XXXX`, который принимает подключения клиентов по UDP и управляет всей игровой логикой.

Проблема в том, что код TCP Lobby Server **отсутствует в репозитории**. В проекте есть только **клиент** для этого сервера — `LobbyManager.cs` (`Assets/Scripts/Network/LobbyManager.cs`), который подключается к `10.93.27.48:7700` по TCP и отправляет команды `CREATE\n` / `JOIN:CODE\n`. Сам сервер, который эти команды обрабатывает, нигде в проекте не реализован.

## Как я к этому пришёл

1. **Изучил структуру проекта** — нашёл папку `Assets/Scripts/Network/` с сетевыми скриптами.
2. **Прочитал `LobbyManager.cs`** — увидел, что он является TCP-клиентом: создаёт `TcpClient`, подключается к `serverAddress:managerPort` (по умолчанию `10.93.27.48:7700`), отправляет `CREATE\n` или `JOIN:CODE\n`, парсит ответ.
3. **Искал серверную сторону** — искал файлы с паттернами `TcpListener`, `Socket`, `7700`, `listener`, а также любые `.py`, `.sh`, `Dockerfile`, `*LobbyServer*`, `*deploy*`, `Server/**`. Ничего не найдено.
4. **Проверил `NetworkGameManager.cs`** — он содержит `StartDedicatedServer()`, который запускает Unity Netcode сервер (UDP). Это игровой сервер, а не TCP-лобби.
5. **Проверил `BuildScript.cs`** — есть билд-скрипты для Windows Client и Linux Server. Linux Server — это Unity headless build (игровой сервер), а не TCP лобби.
6. **Проверил `EditorBuildSettings.asset`** — сцены: Main Menu, Join, Custom Game, Quick Game, MultiplayerGameTest. Нет сцены для запуска лобби-сервера.

**Вывод:** TCP Lobby Server — это внешний сервис, который запускается отдельно (вероятно, на `10.93.27.48`), но его код не хранится в этом репозитории. Без него клиент не сможет создать или найти лобби.

## Предпринятые действия

Написал простой TCP-сервер на Python (`server/lobby_server.py`), который:

- Слушает `127.0.0.1:7700`
- Обрабатывает `CREATE\n` → генерирует 5-символьный код, назначает порт начиная с 7777, отвечает `CODE:XXXX\nPORT:YYYY\n`
- Обрабатывает `JOIN:CODE\n` → возвращает `PORT:YYYY\n` или `NOT_FOUND\n`
- Хранит лобби в словаре в памяти (нет персистентности — нормально для тестов)

Также изменил адреса в C#-коде:

- `LobbyManager.cs:13` — `serverAddress` с `10.93.27.48` на `127.0.0.1`
- `NetworkGameManager.cs:28` — `serverAddress` с `10.93.27.48` на `127.0.0.1`
- `NetworkGameManager.cs:449` — `transport.ConnectionData.Address` с `10.93.27.48` на `127.0.0.1`
- `CustomGameUINetwork.cs:44` — fallback-адрес с `10.93.27.48` на `127.0.0.1`

## Почему такое решение

1. **Python — минимальный порог входа.** Не нужно собирать额外 проект, не нужен .NET SDK. Достаточно `python lobby_server.py`.
2. **Протокол тривиальный.** Два текстовых сообщения, ответ — два поля. Любой язык подошёл бы, Python — самый доступный.
3. **Адреса переключены на `127.0.0.1`.** Это позволяет тестировать всё на одной машине без сетевой инфраструктуры. Для продакшена адрес можно вернуть.
4. **Unity Dedicated Server не затронут.** Игровой сервер по-прежнему запускается из Unity (Play Mode или билд с `-server`). TCP-лобби только координирует подключения.

## Альтернативы, которые были отброшены

- **Написать лобби-сервер на C#** — избыточно, проект и так использует C# для Unity. Отдельный C#-проект потребовал бы `dotnet build`.
- **Запускать Unity как лобби-сервер** — Unity Netcode не предназначен для TCP-лобби, это был бы костыль.
- **Использовать существующий сервер `10.93.27.48`** — не работает локально, зависит от внешней инфраструктуры.

## Статус изменений

| Файл | Действие |
|---|---|
| `server/lobby_server.py` | Создан (новый) |
| `Assets/Scripts/Network/LobbyManager.cs:13` | Адрес → `127.0.0.1` |
| `Assets/Scripts/Network/NetworkGameManager.cs:28` | Адрес → `127.0.0.1` |
| `Assets/Scripts/Network/NetworkGameManager.cs:449` | Адрес → `127.0.0.1` |
| `Assets/Scripts/Network/CustomGameUINetwork.cs:44` | Fallback → `127.0.0.1` |

## Как использовать

```bash
# 1. Запустить лобби-сервер
cd server
python lobby_server.py

# 2. Запустить Unity Dedicated Server (в Unity Editor или билд)
# В Unity Editor: открой проект, нажми Play
# Или: ClientBuild.exe -server -port 7777

# 3. Запустить Unity Client
# Второй экземпляр Unity или билд → Main Menu → Create Game / Join
```
