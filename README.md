# LTESystemMonitorAgent

Windows Service агент для сбора системных метрик компьютера и отправки данных на HTTP API.

Агент собирает состояние машины по расписанию, сохраняет снимки в SQLite, создает outbox-сообщения и отдельно отправляет их во внешний HTTP API. Если API временно недоступен, приложение не завершается: ошибка пишется в лог, сообщение остается доступным для повторной отправки.

## Требования к запуску

- Windows.
- .NET 10 SDK для сборки из исходников.
- Права администратора для установки, запуска, остановки и удаления Windows Service.

## Как собрать проект

Выполнить из корня репозитория:

```powershell
dotnet restore .\LTESystemMonitorAgent.slnx
dotnet build .\LTESystemMonitorAgent.slnx
dotnet test .\LTESystemMonitorAgent.slnx --no-restore
```

Опубликовать исполняемые файлы для установки службы:

```powershell
dotnet publish .\src\LTESystemMonitorAgent\LTESystemMonitorAgent.csproj -c Release -r win-x64 --self-contained true -o .\publish\LTESystemMonitorAgent
```

Публикация выполняется в self-contained режиме: на машине, где будет запускаться опубликованная папка, не требуется установленный .NET Runtime или .NET SDK.

После публикации основная папка приложения будет находиться здесь:

```text
.\publish\LTESystemMonitorAgent
```

Скрипты управления службой будут лежать на уровень выше, в корне `publish`:

```text
.\publish\start.cmd
.\publish\stop.cmd
.\publish\delete.cmd
```

## Как установить службу

Открыть папку:

```text
publish
```

Запустить `start.cmd`. Если служба еще не установлена, скрипт установит ее из `publish\LTESystemMonitorAgent\LTESystemMonitorAgent.exe` и затем запустит.

При запуске без прав администратора скрипт запросит повышение прав через стандартное окно Windows.

## Как запустить службу

Запустить из папки `publish`:

```text
start.cmd
```

Если служба уже установлена, скрипт просто запустит ее. Если служба уже запущена, скрипт покажет текущее состояние.

Альтернативная ручная команда PowerShell:

```powershell
Start-Service LTESystemMonitorAgent
```

## Как остановить службу

Запустить из папки `publish`:

```text
stop.cmd
```

Альтернативная ручная команда PowerShell:

```powershell
Stop-Service LTESystemMonitorAgent
```

## Как удалить службу

Запустить из папки `publish`:

```text
delete.cmd
```

Скрипт остановит службу, если она запущена, а затем удалит ее из Windows Service Control Manager.

Альтернативные ручные команды PowerShell:

```powershell
Stop-Service LTESystemMonitorAgent
sc.exe delete LTESystemMonitorAgent
```

Если служба уже остановлена, команда `Stop-Service` может вернуть предупреждение. После `sc.exe delete` служба будет удалена из Windows Service Control Manager.

## Как изменить конфигурацию

Файл конфигурации в исходниках:

```text
src\LTESystemMonitorAgent\appsettings.json
```

Файл конфигурации после публикации:

```text
publish\LTESystemMonitorAgent\appsettings.json
```

Основные параметры:

- `HttpMetricDelivery:ApiUrl` - адрес HTTP API, куда отправляются метрики.
- `Quartz:MetricCollectionIntervalSeconds` - интервал сбора метрик в секундах.
- `Monitoring:MonitoredProcesses` - список процессов, наличие которых нужно проверять.
- `Logging:FilePath` - путь к основному файлу лога.
- `HttpMetricDelivery:HttpTimeoutSeconds` - timeout HTTP-запроса в секундах.
- `Outbox:BatchSize` - количество outbox-сообщений, которые обрабатываются за один запуск отправки.
- `Database:ConnectionString` - строка подключения к SQLite.

Если `Logging:FilePath` задан относительным путем, файл лога создается относительно папки с `LTESystemMonitorAgent.exe`.

После изменения конфигурации для установленной службы нужно перезапустить службу:

```powershell
Restart-Service LTESystemMonitorAgent
```

## Как проверить работу приложения

Проверить, что служба установлена и запущена:

```powershell
Get-Service LTESystemMonitorAgent | Format-List Name, Status, ServiceType, StartType
sc.exe qc LTESystemMonitorAgent
```

Через 1-2 минуты после запуска проверить последние строки основного лога:

```powershell
Get-Content .\publish\LTESystemMonitorAgent\logs\agent.log -Tail 100
```

В логах должны появиться события запуска, выполнения задач Quartz, сбора метрик, успешной отправки или ошибки отправки:

```json
{"level":"INFO","logger":"LTESystemMonitorAgent.Program","message":"Starting LTESystemMonitorAgent."}
{"level":"INFO","logger":"LTESystemMonitorAgent.Jobs.CollectMetricsJob","message":"Metric collection job started."}
{"level":"INFO","logger":"LTESystemMonitorAgent.Jobs.DispatchOutboxJob","message":"Outbox dispatch job started."}
```

Для проверки HTTP-отправки можно запустить вспомогательный mock API:

```powershell
dotnet run --project .\tools\LTESystemMockApi\LTESystemMockApi.csproj
```

После запуска mock API указать его адрес в `HttpMetricDelivery:ApiUrl`, например:

```json
"HttpMetricDelivery": {
  "ApiUrl": "http://localhost:5203/api/metrics",
  "HttpTimeoutSeconds": 10
}
```

Приложение также можно запустить в консольном режиме без установки службы:

```powershell
dotnet run --project .\src\LTESystemMonitorAgent\LTESystemMonitorAgent.csproj
```

Консольный запуск нужен только для ручной проверки и разработки. Основной сценарий запуска по ТЗ - Windows Service.

## Где находятся логи

Основной лог приложения пишется в файл из параметра `Logging:FilePath`.

Путь по умолчанию после публикации:

```text
publish\LTESystemMonitorAgent\logs\agent.log
```

Путь по умолчанию при запуске через `dotnet run`:

```text
src\LTESystemMonitorAgent\bin\Debug\net10.0\logs\agent.log
```

Служебный лог самого NLog:

```text
publish\LTESystemMonitorAgent\logs\nlog-internal.log
```

Лог пишется в формате JSON Lines: одна строка равна одному JSON-событию.

Файловое логирование настраивается через:

```text
src\LTESystemMonitorAgent\nlog.config
```

## Пример конфигурационного файла

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
  "HttpMetricDelivery": {
    "ApiUrl": "http://localhost:5203/api/metrics",
    "HttpTimeoutSeconds": 10
  },
  "Outbox": {
    "BatchSize": 10
  },
  "Logging": {
    "FilePath": "logs/agent.log",
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

## Пример JSON, который отправляется на API

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

## Краткое описание архитектуры приложения

Приложение разделено на независимые слои: чтение состояния машины, сохранение метрик, outbox-очередь, доставка метрик и исполняемый Windows Service host.

- `LTESystemMonitorAgent` - исполняемый Generic Host: DI, конфигурация, Windows Service integration, NLog, Quartz jobs, автоматическое применение миграций.
- `LTESystemMachineState.Abstractions` - контракт и модели снимка состояния компьютера.
- `LTESystemMachineState` - Windows-реализация чтения CPU, RAM, IP-адресов, дисков и процессов.
- `LTESystemMonitoring.Abstractions` - контракт сервиса сбора и сохранения метрик.
- `LTESystemMonitoring` - получает снимок состояния машины, сохраняет его в SQLite и создает outbox-сообщение.
- `LTESystemOutbox.Abstractions` - контракт диспетчера outbox.
- `LTESystemOutbox` - читает outbox-сообщения из SQLite, формирует payload, вызывает доставку и обновляет статус отправки.
- `LTESystemMetricDelivery.Abstractions` - контракт и модели доставки метрик во внешний канал.
- `LTESystemMetricDelivery.Http` - HTTP-реализация отправки метрик на API с timeout и обработкой ошибок.
- `LTESM.DAL.Abstractions` - EF Core сущности и контракт `ILTEDbContext`.
- `LTESM.DAL.SQLite` - SQLite EF Core контекст, маппинг таблиц и регистрация базы данных.
- `tools\LTESystemMockApi` - вспомогательный mock API для ручной проверки POST-запросов.
- `tests` - unit-тесты для мониторинга, outbox, HTTP-доставки и конфигурации.

Quartz запускает две периодические задачи:

- `CollectMetricsJob` - собирает системные метрики каждые `Quartz:MetricCollectionIntervalSeconds`.
- `DispatchOutboxJob` - отправляет накопленные outbox-сообщения каждые `Quartz:OutboxDispatchIntervalSeconds`.
