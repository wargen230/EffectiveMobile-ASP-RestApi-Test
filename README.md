# EffectiveMobile ASP REST API — Test

Тестовый ASP.NET Core Web API для поиска рекламных платформ по локациям.

> В этом файле описано как собрать и запустить проект локально и в Docker, как запускать тесты.

## Содержание

* [Использование API / Примеры](#использование-api--примеры)
* [Быстрый старт (локально)](#быстрый-старт-локально)
* [Docker](#docker)
* [Тесты](#тесты)
* [Структура репозитория](#структура-репозитория)
---

## Использование API / Примеры
> Для тестирования API в основном использовался POSTMAN, но команды будут написаны с использованием curl

API содержит два метода:
1. POST - запрос, команда: /api/upload, используется для загрузки текстового файла с платформами и локациями.
2. GET - запрос, команда: /api/search, поиск платформ по локации.

Для загрузки файла используется form-data, в связи с особенностями работы IFormFile, соответсвенно, синтаксис команды отправки файла будет следующий:
```bash
curl -X POST https://example.com/api/upload \
  -F "file=@/путь/к/файлу.txt"
```

Для поиска по файлу используется Query, соответствено синтаксис команды поиска файла будет следующий:
```bash
curl https://example.com/api/search?location=test_location
```

## Быстрый старт (локально)

### Требования

* .NET SDK (рекомендуется 8.0+, проверь `TargetFramework` в `TestAPI.API/*.csproj`).
* Git.
* (Опционально) Python 3.x — если хочешь запускать Python-тесты (папка `TestAPI.PythonTests`).

### Клонирование и запуск

```bash
git clone https://github.com/wargen230/EffectiveMobile-ASP-RestApi-Test.git
cd EffectiveMobile-ASP-RestApi-Test
```

В корне решения можно выполнить:

```bash
# Восстановить зависимости
dotnet restore

# Запустить API (из папки TestAPI.API)
cd TestAPI.API
dotnet run
```

После запуска API будет доступен по адресу, указанному в выводе (обычно `http://localhost:5001`). Если в проекте подключён Swagger — открой `/swagger`.

---

## Docker

Рекомендуется использовать Docker для локального тестирования и для деплоя.

### Сборка и запуск

```bash
# сборка образа
docker build -t testapi:latest . (в папке проекта, где лежит Dockerfile)

# запуск контейнера
# пробрасываем порт хоста:контейнера (опционально, использовалось для тестирования на vps сервере)
docker run --rm -p 5001:5001 testapi:latest
# тогда API будет доступен по http://localhost:5001
```

## Тесты

### .NET юнит-тесты

Проект тестов находится в `TestAPI.Tests`.

Запуск:
```bash
dotnet test ./TestAPI.Tests
```

### Python-тесты

Для динамического тестирования нагрузки использовалось опенсорс приложение locust, тесты лежат в папке `TestAPI.PythonTests`.

#### Установка locust

```bash
pip install -e git://github.com/locustio/locust.git@master#egg=locust
```

#### Запуск тестов
```bash
locust -f ./your_test_source/example_locust_test.py --no-web -c число пользователей на выбор -r скорость загрузки на выбор
```

## Структура репозитория

```
.github/workflows/           # (опционально) workflows
TestAPI.API/                 # ASP.NET Core Web API проект
TestAPI.Tests/               # .NET юнит-тесты
TestAPI.PythonTests/         # (опционально) Python тесты
EffectiveMobile-ASP-RestApi-Test.sln
README.md
.gitignore
```

