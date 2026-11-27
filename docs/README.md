# MovieTracker — учебно ръководство за Exercises 01–06

MovieTracker е учебно приложение за филми. Отвори `/movies`, за да видиш каталога от ASP.NET Core API, и `/movie-insights`, за да видиш статистика върху пет филма, вградени в Blazor клиента. Проследи двата потока поотделно, за да разбереш кога браузърът прави HTTP заявка и кога обработва данни локално.

## Упражнения 02–06

- [Локално стартиране и PostgreSQL тестове](Local-Startup.md)
- [API справка](API.md)
- [Архитектура, примери от кода и диаграми](Exercises-02-06.md)
- [Статус на реализацията и проверки](Implementation-Status.md)

## Основи от Exercise 01

| Урок | Какво ще можеш да обясниш |
| --- | --- |
| [C# основи](CSharp-Fundamentals.md) | Типове, класове, records, интерфейси, generics, грешки и асинхронност |
| [.NET основи](DotNet-Fundamentals.md) | SDK, runtime, target framework, NuGet и Blazor WebAssembly |
| [Архитектура](Architecture.md) | Кой проект какво знае и кой избира реализацията на хранилището |
| [Жизнен цикъл на заявката](Request-Lifecycle.md) | Пътят от `/movies` до JSON отговора и обратно |
| [LINQ](LINQ.md) | Групиране, сортиране, пагинация и Movie Insights |
| [Тестове](Testing.md) | Какво проверява всеки от 15-те регресионни случая |
| [Git](Git-Workflow.md) | Работно копие, staging, commit, branch и push |
| [Сигурност](Security-Fundamentals.md) | Граници на доверие, валидация, HTTPS, CORS и бъдеща идентичност |
| [Речник](Glossary.md) | Кратки определения с препратки към уроците |
| [Въпроси за интервю](Interview-Questions.md) | Въпроси за самопроверка с примерни отговори |

## Какво е реализирано

Каталогът, детайлите, акаунтите, Watchlist, оценките и ревютата използват PostgreSQL чрез API. Movie Insights запазва локалните филми за домашното от Exercise 01. Основните уроци описват и началния вариант на приложението. Справката за текущия код е в [Exercises 02–06](Exercises-02-06.md).

## Практическа проверка

```powershell
dotnet restore MovieTracker.slnx
dotnet build MovieTracker.slnx --no-restore
dotnet test MovieTracker.slnx --no-build --no-restore
```

При стартирани API и Web отвори [каталога](https://localhost:7002/movies) и [справката](https://localhost:7002/movie-insights). Свери справката с [очакваните резултати](LINQ.md#очакван-резултат), а HTTP параметрите с [примерите за заявки](Request-Lifecycle.md#примери-за-заявки).

За защита на проекта използвай [сценария за устно представяне](presentation.md). Решението за произхода на данните е записано в [ADR 0001](adr/0001-movie-insights-demo-data.md).
