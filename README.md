# LTESystemMonitorAgent

Windows Service агент для сбора системных метрик компьютера, сохранения снимков в SQLite и последующей отправки данных на HTTP API через outbox.

> Статус: проект находится в разработке. Инфраструктура запуска, DI, SQLite, миграции, NLog и Quartz.NET уже подключены. Сбор системных метрик, запись в SQLite и HTTP-отправка через outbox реализованы.

## Требования

- Windows
- .NET 10 SDK для сборки и разработки
- Права администратора для установки и управления Windows Service

## Сборка

```powershell
dotnet restore .\LTESystemMonitorAgent.slnx
dotnet build .\LTESystemMonitorAgent.slnx
```

## Публикация

```powershell
dotnet publish .\src\LTESystemMonitorAgent\LTESystemMonitorAgent.csproj -c Release -r win-x64 --self-contained false -o .\publish\LTESystemMonitorAgent
```

После публикации исполняемый файл будет находиться здесь:

```text
.\publish\LTESystemMonitorAgent\LTESystemMonitorAgent.exe
```

## Запуск в консольном режиме

Из исходников:

```powershell
dotnet run --project .\src\LTESystemMonitorAgent\LTESystemMonitorAgent.csproj
```

Из опубликованной папки:

```powershell
.\publish\LTESystemMonitorAgent\LTESystemMonitorAgent.exe
```

## Установка Windows Service

Выполнять PowerShell от имени администратора:

```powershell
$serviceName = "LTESystemMonitorAgent"
$exePath = "F:\my programs C#\LTESystemMonitorAgent\publish\LTESystemMonitorAgent\LTESystemMonitorAgent.exe"

New-Service `
  -Name $serviceName `
  -BinaryPathName "`"$exePath`"" `
  -DisplayName "LTE System Monitor Agent" `
  -Description "Collects system metrics and dispatches them to HTTP API." `
  -StartupType Automatic
```

## Запуск службы

```powershell
Start-Service LTESystemMonitorAgent
```

## Остановка службы

```powershell
Stop-Service LTESystemMonitorAgent
```

## Удаление службы

```powershell
Stop-Service LTESystemMonitorAgent
sc.exe delete LTESystemMonitorAgent
```

## Конфигурация

Основной файл конфигурации:

```text
src\LTESystemMonitorAgent\appsettings.json
```

После публикации конфигурация находится рядом с exe:

```text
publish\LTESystemMonitorAgent\appsettings.json
```

Текущий пример:

```json
{
  "Database": {
    "ConnectionString": "Data Source=ltesystem-monitor-agent.db",
    "LoggingEnabled": false
  },
  "Quartz": {
    "MetricCollectionIntervalSeconds": 30,
    "OutboxDispatchIntervalSeconds": 10
  },
  "Monitoring": {
    "CpuSampleMilliseconds": 500,
    "MonitoredProcesses": [
      "notepad"
    ]
  },
  "Outbox": {
    "ApiUrl": "https://localhost:7200/api/metrics",
    "HttpTimeoutSeconds": 10,
    "BatchSize": 10
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

Планируемые настройки по ТЗ:

- Путь к лог-файлу.

## Логи

NLog настраивается в:

```text
src\LTESystemMonitorAgent\nlog.config
```

По умолчанию логи пишутся в папку `logs` рядом с исполняемым файлом:

```text
publish\LTESystemMonitorAgent\logs\agent.log
publish\LTESystemMonitorAgent\logs\nlog-internal.log
```

Файл `agent.log` пишется в формате JSON Lines: одна строка равна одному JSON-событию.

При запуске из `dotnet run` логи находятся в:

```text
src\LTESystemMonitorAgent\bin\Debug\net10.0\logs
```

## Проверка работы

Проверить, что приложение запущено именно как Windows Service:

```powershell
Get-Service LTESystemMonitorAgent | Format-List Name, Status, ServiceType, StartType
sc.exe qc LTESystemMonitorAgent
```

Через 1-2 минуты после запуска проверить последние строки JSON-лога:

```powershell
Get-Content .\publish\LTESystemMonitorAgent\logs\agent.log -Tail 100
```

В рабочем запуске в логах должны появляться сообщения:

```json
{"level":"INFO","logger":"LTESystemMonitorAgent.Program","message":"Starting LTESystemMonitorAgent."}
{"level":"INFO","logger":"LTESystemMonitorAgent.Program","message":"LTESystemMonitorAgent host built successfully."}
{"level":"INFO","logger":"LTESystemMonitorAgent.Jobs.CollectMetricsJob","message":"Metric collection job started."}
{"level":"INFO","logger":"LTESystemMonitorAgent.Jobs.DispatchOutboxJob","message":"Outbox dispatch job started."}
```

После остановки службы проверить, что остановка тоже записана в лог:

```powershell
Stop-Service LTESystemMonitorAgent
Get-Content .\publish\LTESystemMonitorAgent\logs\agent.log -Tail 20
```

Ожидаемые сообщения:

```json
{"level":"INFO","logger":"LTESystemMonitorAgent.Program","message":"LTESystemMonitorAgent is stopping."}
{"level":"INFO","logger":"LTESystemMonitorAgent.Program","message":"LTESystemMonitorAgent stopped."}
```

## Mock API

Для ручной проверки отправки метрик есть вспомогательный Web API:

```powershell
dotnet run --project .\tools\LTESystemMockApi\LTESystemMockApi.csproj
```

Swagger UI доступен в Development-режиме:

```text
https://localhost:<port>/swagger
```

Mock endpoint:

```text
POST /api/metrics
```

Сервис принимает произвольный JSON payload метрик, пишет его в консоль через NLog и возвращает `200 OK`.

## Миграции БД

Миграции применяются автоматически при старте приложения до запуска Quartz job-ов.

Пример добавления миграции:

```powershell
dotnet ef migrations add InitialCreate `
  --project .\src\LTESM.DAL.SQLite\LTESM.DAL.SQLite.csproj `
  --startup-project .\src\LTESystemMonitorAgent\LTESystemMonitorAgent.csproj
```

## Пример JSON для HTTP API

Пример payload, который агент отправляет на HTTP API:

```json
{
  "collectedAtUtc": "2026-05-25T18:30:00Z",
  "hostname": "WORKSTATION-01",
  "ipAddresses": [
    {
      "address": "192.168.1.10",
      "addressFamily": "InterNetwork",
      "networkInterfaceName": "Ethernet"
    }
  ],
  "windowsVersion": "Microsoft Windows 11 Pro 10.0.26100",
  "uptimeSeconds": 86400,
  "cpuUsagePercent": 17.5,
  "ramUsagePercent": 63.2,
  "totalMemoryBytes": 34359738368,
  "availableMemoryBytes": 12616466432,
  "diskSpaces": [
    {
      "name": "C:\\",
      "volumeLabel": "System",
      "driveFormat": "NTFS",
      "totalSpaceBytes": 512000000000,
      "freeSpaceBytes": 128000000000
    }
  ],
  "runningProcesses": [
    {
      "processId": 1234,
      "name": "notepad",
      "startedAtUtc": "2026-05-25T17:45:00Z",
      "workingSetBytes": 52428800
    }
  ],
  "monitoredProcesses": [
    {
      "name": "notepad",
      "isRunning": true,
      "matchedProcessCount": 1
    }
  ]
}
```

## Архитектура

- `LTESystemMonitorAgent` - исполняемый Generic Host, конфигурация DI, Windows Service integration, NLog, Quartz jobs, авто-миграции.
- `LTESystemMonitoring.Abstractions` - контракты сбора метрик.
- `LTESystemMonitoring` - реализация сбора метрик и DI extension.
- `LTESystemOutbox.Abstractions` - контракты отправки outbox.
- `LTESystemOutbox` - реализация outbox-dispatcher, HTTP POST, retry/backoff и DI extension.
- `LTESM.DAL.Abstractions` - EF-сущности и контракт `ILTEDbContext`.
- `LTESM.DAL.SQLite` - SQLite EF Core контекст, маппинг таблиц и регистрация БД.
- `tests` - будущие unit/integration тесты.
- `tools` - будущие вспомогательные приложения, включая mock API.

Quartz запускает две периодические задачи:

- `CollectMetricsJob` - сбор и сохранение снимка метрик.
- `DispatchOutboxJob` - отправка накопленных сообщений outbox.
