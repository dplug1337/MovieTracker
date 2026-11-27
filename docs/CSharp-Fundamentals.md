# Основи на C#

> Този материал описва началния етап на проекта. Виж [реализацията за Exercises 02–06](Exercises-02-06.md) за текущия backend и PostgreSQL.

C# проверява типовете при компилация. Типът описва какви стойности може да има една променлива и какво може да прави кодът с нея. Например `int` представя цяло число, `string` — текст, а `Guid` — 128-битов идентификатор. Дори когато пишем `var`, компилаторът извежда конкретен тип от израза. Тази проверка хваща например опит за подаване на текст като година преди изпълнение. [Movie.cs](../MovieTracker.Domain/Movies/Movie.cs)

## Class, record и enum

```csharp
public sealed record GetMovieListRequest(int Page = 1, int PageSize = 12);
```

| Концепция | Как работи и къде е използвана | Алтернатива и последици при премахване |
| --- | --- | --- |
| `class` | `Movie` събира състоянието и правилата за валиден филм. Частният constructor и setters ограничават промяната отвън. `Create` проверява стойностите и изчиства интервалите. `sealed` забранява наследяване. [Movie.cs](../MovieTracker.Domain/Movies/Movie.cs) | Публичен constructor би бил по-кратък, но трябва пак да гарантира правилата и да реши как съобщава грешките. Без обща фабрика правилата лесно се разпръскват. |
| `record` | `GetMovieListRequest` и `MovieListItemResponse` са типове за пренос на данни. Компилаторът генерира свойства и сравнение по стойност за позиционните членове. Handler тестът сравнява response records по съдържание. [GetMovieList.cs](../MovieTracker.Application/Movies/GetMovieList/GetMovieList.cs) | Обикновен class е подходящ при повече поведение. Сравнението по стойност при него изисква собствена реализация или отделни проверки на полетата. |
| `enum` | `MovieGenre` дава именувани стойности с базов целочислен тип. Mapping-ът превръща `ScienceFiction` в текста `"ScienceFiction"`. Преобразуване от произволно число може да произведе недефинирана стойност. [MovieGenre.cs](../MovieTracker.Domain/Movies/MovieGenre.cs), [Mapping](../MovieTracker.Application/Movies/GetMovieList/GetMovieList.Mapping.cs) | `string` позволява свободни жанрове, както при демо модела, но правописни разлики могат да образуват отделни групи. Замяна в Domain би променила и валидните входове. |

## Interface и generics

`IMovieRepository` описва `CountAsync` и `GetPagedAsync`. Handler-ът извиква методите през този тип. В изпълнение обектът е реализацията, регистрирана от DI, а тестът може да подаде `MockMovieRepository` директно. Директна зависимост от конкретен клас спестява интерфейса при еднократен, самостоятелен код, но тук би свързала Application с Infrastructure и би усложнила unit теста. [IMovieRepository.cs](../MovieTracker.Application/Common/Repositories/IMovieRepository.cs), [MockMovieRepository.cs](../tests/MovieTracker.UnitTests/Mocks/MockMovieRepository.cs)

`Result<T>`, `Task<T>` и `IReadOnlyList<T>` са generics. `T` е параметър за тип, например `Movie` в `Result<Movie>`. Компилаторът знае какъв тип връща `Value`. С `object` бихме загубили тази гаранция и бихме използвали преобразувания при четене. `IReadOnlyList<T>` дава четене по индекс и `Count`, но самият интерфейс не гарантира, че оригиналната колекция е неизменяема. [Result.cs](../MovieTracker.Domain/Common/Results/Result.cs), [IMovieRepository.cs](../MovieTracker.Application/Common/Repositories/IMovieRepository.cs)

## Очаквани грешки и null

`Result<T>` представя успех с `Value` и `Error.None` или отказ с конкретен `Error`. При `return MovieErrors.TitleRequired;` implicit operator превръща `Error` във failed result. Exceptions са полезни за неочаквани проблеми, например липсващ JSON от API. `null` сам по себе си не казва защо създаването е неуспешно. [Movie.cs](../MovieTracker.Domain/Movies/Movie.cs), [Result.cs](../MovieTracker.Domain/Common/Results/Result.cs), [MovieApiClient.cs](../MovieTracker.Web/Services/Movies/MovieApiClient.cs)

`MovieListResponseModel?` означава, че отговорът може да липсва. Компилаторът следи възможния `null`. Операторът `!` в `Movie.Create(...).Value!` потиска предупреждението и разчита на валидните seed стойности. Той няма runtime проверка. Премахването му може да върне предупреждение, а премахването на runtime проверките би оставило реални грешки при `null`. [Movies.razor](../MovieTracker.Web/Pages/Movies.razor), [TestMovieRepository.cs](../tests/MovieTracker.IntegrationTests/Movies/TestMovieRepository.cs)

## async/await и cancellation

При `await httpClient.GetAsync(...)` незавършената задача позволява на метода да върне управление. Компилаторът генерира state machine, която съхранява нужното състояние и продължава след завършване на задачата. `async` не означава нова нишка. In-memory хранилището приключва синхронно и връща `Task.FromResult`. Endpoint-ът предава токен за прекратяване до хранилището, което проверява с `ThrowIfCancellationRequested`. Страницата оставя клиентския токен по подразбиране. Блокиращо чакане би задържало изпълнението, а синхронен интерфейс е по-прост само когато операцията е гарантирано локална. [MovieApiClient.cs](../MovieTracker.Web/Services/Movies/MovieApiClient.cs), [Endpoint](../MovieTracker.Application/Movies/GetMovieList/GetMovieList.Endpoint.cs), [Repository](../tests/MovieTracker.IntegrationTests/Movies/TestMovieRepository.cs)

Lambda като `movie => movie.Title` описва функция, която взема филм и връща заглавието му. Може да я заменим с именуван метод, ако логиката стане по-дълга. Виж [LINQ](LINQ.md) за местата, които я извикват.
