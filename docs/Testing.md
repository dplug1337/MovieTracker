# Регресионни тестове

> Този материал описва началния етап на проекта. Виж [реализацията за Exercises 02–06](Exercises-02-06.md) за текущия backend и PostgreSQL.

Регресионният тест проверява, че познато поведение остава вярно след промяна. Unit тестът упражнява малък блок код директно и дава точен сигнал за причината при отказ. Integration тестът проверява как работят компонентите заедно. Само unit тестове биха пропуснали проблем в HTTP routing, DI или JSON. Само integration тестове биха затруднили намирането на точния дефект и биха изисквали повече инфраструктура.

## Как работи наборът

NUnit открива методите с `[Test]` и изпълнява всеки `[TestCase]` като отделен случай. `[SetUp]` подготвя validator преди всеки случай. `Assert.EnterMultipleScope()` събира повече от един неуспешен assert в един тест. `BaseUnitTest` създава Bogus Faker с фиксиран seed `1337`, за да се получават възпроизводими поредици от примерни данни. Ръчно зададени стойности са добра алтернатива, когато конкретната стойност е същността на проверката. Без фиксиран seed диагностиката след случаен отказ може да стане по-трудна. [BaseUnitTest.cs](../tests/MovieTracker.UnitTests/BaseUnitTest.cs), [Версии на пакетите](../Directory.Packages.props)

`MockMovieRepository` е ръчно написан test double. Той връща зададени данни и записва `RequestedSkip` и `RequestedTake`. Ако използваме истинския repository в unit теста, очакванията ще зависят от seed каталога и трудно ще изолираме сметката на handler-а. [MockMovieRepository.cs](../tests/MovieTracker.UnitTests/Mocks/MockMovieRepository.cs)

`IntegrationTestWebAppFactory : WebApplicationFactory<Program>` стартира API в среда `Testing`. `BaseIntegrationTest` създава клиент с базов адрес `https://localhost` и освобождава клиента и factory след изпълнение. Заявките минават през тестовия host без отделно стартиран API процес или реална TLS връзка. Тест срещу работещ API би покрил още мрежова конфигурация, но изисква външен процес. [Factory](../tests/MovieTracker.IntegrationTests/IntegrationTestWebAppFactory.cs), [BaseIntegrationTest.cs](../tests/MovieTracker.IntegrationTests/BaseIntegrationTest.cs)

## Всички 15 случая

### MovieTests — 6 случая

| Случай | Какво пази |
| --- | --- |
| `Create_WithValidValues_ReturnsTheMovie` | Успех, `Error.None`, наличен филм и премахнати крайни интервали от заглавие, описание и URL на постера. |
| `Create_WithInvalidValue_ReturnsAnError` — `id` | `Guid.Empty` връща `Movies.IdRequired`. |
| Същият метод — `title` | Празно заглавие от интервали връща `Movies.TitleRequired`. |
| Същият метод — `description` | Празно описание от интервали връща `Movies.DescriptionRequired`. |
| Същият метод — `releaseYear` | Година 1887 връща `Movies.ReleaseYearTooEarly`. |
| Същият метод — `posterUrl` | Празен URL от интервали връща `Movies.PosterUrlRequired`. |

Всеки невалиден случай проверява и двата флага, `Value == null`, кода и точния текст на грешката. [MovieTests.cs](../tests/MovieTracker.UnitTests/Movies/MovieTests.cs)

### ResultTests — 2 случая

| Случай | Какво пази |
| --- | --- |
| `Success_WithValue_CreatesSuccessfulResult` | `Result<string>.Success("movie")` запазва стойността, задава двата флага и използва `Error.None`. |
| `Error_ImplicitlyConvertsToFailedResult` | Присвояване на `Error` към `Result<string>` създава отказ със същата грешка и `null` стойност. |

[ResultTests.cs](../tests/MovieTracker.UnitTests/Common/Results/ResultTests.cs)

### GetMovieListValidatorTests — 4 случая

| Случай | Какво пази |
| --- | --- |
| `ValidateAsync_DefaultRequest_IsValid` | Стойности по подразбиране 1 и 12 са валидни. |
| `ValidateAsync_InvalidPagination_HasExpectedError` — `(0, 12)` | Грешките съдържат свойството `Page`. |
| Същият метод — `(1, 0)` | Грешките съдържат свойството `PageSize`. |
| Същият метод — `(1, 51)` | Грешките съдържат свойството `PageSize` и над горната граница. |

Тези проверки търсят името на свойството сред грешките. [GetMovieListValidatorTests.cs](../tests/MovieTracker.UnitTests/Movies/GetMovieList/GetMovieListValidatorTests.cs)

### GetMovieListHandlerTests — 1 случай

`HandleAsync_ReturnsTheRequestedMappedPage` извиква handler-а със страница 2 и размер 3. Проверява `skip = 3`, `take = 3`, запазени параметри, `TotalCount = 8`, един върнат елемент и всички mapped полета, включително текстов жанр `ScienceFiction`. [GetMovieListHandlerTests.cs](../tests/MovieTracker.UnitTests/Movies/GetMovieList/GetMovieListHandlerTests.cs)

```csharp
var response = await handler.HandleAsync(
    new GetMovieListRequest(Page: 2, PageSize: 3),
    CancellationToken.None);
```

### GetMovieListEndpointTests — 2 случая

| Случай | Какво пази |
| --- | --- |
| `GetMovies_ReturnsARequestedPageFromTheInMemoryCatalogue` | `200 OK`, десериализиращ се JSON, страница 1, размер 3, общо 10 филма, първи заглавия Arrival, Blade Runner 2049 и Inception в този ред, относителни пътища за постерите. |
| `GetMovies_WithInvalidPagination_ReturnsValidationProblem` | За `page=0&pageSize=51` статусът е 400 и JSON съдържа `errors.Page` и `errors.PageSize`. |

[GetMovieListEndpointTests.cs](../tests/MovieTracker.IntegrationTests/Movies/GetMovieListEndpointTests.cs)

## Как да ги стартираш

```powershell
dotnet build MovieTracker.slnx
dotnet test MovieTracker.slnx --no-build
```

По дефиниция има 13 unit и 2 integration случая. Реалният отчет от `dotnet test` показва колко случая са изпълнени успешно. Този набор не изпълнява Blazor UI и не проверява Movie Insights. За учебна проверка отвори страницата и сравни [очаквания резултат](LINQ.md#очакван-резултат). Бъдещ UI тест с браузър би проверил и визуализацията, но изисква допълнителна инфраструктура.
