# Как да представя MovieTracker

> Този материал описва началния етап на проекта. Виж [реализацията за Exercises 02–06](Exercises-02-06.md) за текущия backend и PostgreSQL.

## Кратко представяне

> MovieTracker е учебен проект на C# с Blazor WebAssembly и ASP.NET Core Minimal API. Страницата Movies показва каталог от сървъра. Movie Insights е отделна демо страница, която използва LINQ върху пет филма, вече налични в клиента.

## Какво да покажа

1. Отварям `/movies` и показвам заглавието и филмовите карти. Обяснявам, че `MovieApiClient` изпраща HTTP заявка, а Blazor показва получения JSON като карти. [Movies.razor](../MovieTracker.Web/Pages/Movies.razor)
2. Показвам веригата на сървъра. Endpoint-ът проверява параметрите. Handler-ът изчислява коя част от списъка ни трябва. Хранилището подрежда по заглавие и връща тази част с `Skip` и `Take`. [Диаграма и файлове](README.md#как-се-движат-данните)
3. Натискам **Movie Insights**. Показвам трите жанра и трите филма. Посочвам Interstellar и обяснявам защо излиза пред Whiplash при еднаква година. [MovieInsights.razor](../MovieTracker.Web/Pages/MovieInsights.razor)

## Ако ме попитат

- **Защо има интерфейс за хранилището?** Handler-ът използва операции за броене и извличане на страница. API избира конкретната реализация чрез dependency injection. [IMovieRepository.cs](../MovieTracker.Application/Common/Repositories/IMovieRepository.cs), [Program.cs](../MovieTracker.API/Program.cs)
- **Защо Web не реферира API?** Web работи в браузъра и получава данните през HTTP. Собственият му модел описва JSON отговора. [MovieListResponseModel.cs](../MovieTracker.Web/Models/Movies/MovieListResponseModel.cs)
- **Как проверявам проекта?** Build проверява компилацията. Регресионните тестове проверяват съществуващото поведение. За демо екрана сравнявам показаните жанрове и подредбата с демо данните. [Инструкции за проверка](README.md#проверка)
