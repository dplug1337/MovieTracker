# MovieTracker от каталог до пълно приложение

## Проекти и посока на зависимостите

Blazor WebAssembly изпълнява страниците в браузъра. Той използва JSON договорите на API чрез `MovieApiClient`. В backend-а endpoint-ът свързва HTTP входа с handler. Handler-ът изпълнява операцията чрез интерфейс на repository. Domain съдържа правилата за филм, оценка, ревю и Watchlist. Infrastructure знае как да преведе операцията към PostgreSQL.

~~~mermaid
flowchart LR
    Web["Blazor WebAssembly"] -->|"HTTPS · JSON"| API["ASP.NET Core API"]
    API --> Application["Application · endpoints и handlers"]
    Application --> Domain["Domain · правила и обекти"]
    API --> Infrastructure["Infrastructure · repositories"]
    Infrastructure -. "реализира интерфейсите" .-> Application
    Infrastructure --> Domain
    Infrastructure -->|"EF Core + Npgsql"| PG[("PostgreSQL")]
~~~

Следвай `GET /api/movies` през `GetMovieListEndpoint`, `GetMovieListHandler` и `PostgresMovieRepository`. `MovieCatalogHandler` използва същия домейн при създаване и редакция на филм. Пагинацията намалява количеството данни при всяка заявка. [GetMovieList](../MovieTracker.Application/Movies/GetMovieList), [MovieCatalog.cs](../MovieTracker.Application/Movies/MovieCatalog.cs), [PostgresMovieRepository.cs](../MovieTracker.Infrastructure/Repositories/PostgresMovieRepository.cs)

## PostgreSQL, EF Core и миграции

Релационната база пази данните като редове в таблици. Primary key идентифицира всеки ред. Foreign key сочи към ред в друга таблица и пази връзката валидна. Уникалният индекс гарантира, че една стойност или комбинация може да се среща само веднъж, включително при едновременни записи.

`MovieTrackerDbContext` управлява EF модела и заявките в рамките на един DI scope. Npgsql е PostgreSQL доставчикът на EF Core. `ApplyConfigurationsFromAssembly` зарежда класовете `MovieConfiguration`, `UserConfiguration`, `WatchlistConfiguration`, `RatingConfiguration` и `ReviewConfiguration`. Миграцията описва промяна на схемата, а snapshot-ът описва последния модел за генериране на следващата миграция. Таблицата `__EFMigrationsHistory` пази приложените версии.

~~~csharp
builder.HasKey(x => x.Id);
builder.HasIndex(x => new { x.UserId, x.MovieId }).IsUnique();
builder.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.UserId);
~~~

Тези редове от `ReviewConfiguration` описват първичен ключ, уникален индекс за двойката потребител и филм и външен ключ към автора. При Rating и Watchlist двойката е съставен първичен ключ. Това е и конфликтният ключ за техните upsert операции. [DbContext](../MovieTracker.Infrastructure/Persistence/MovieTrackerDbContext.cs), [Конфигурации](../MovieTracker.Infrastructure/Persistence/InteractionConfigurations.cs), [Миграции](../MovieTracker.Infrastructure/Persistence/Migrations)

~~~mermaid
erDiagram
    USERS ||--o{ WATCHLIST : owns
    MOVIES ||--o{ WATCHLIST : includes
    USERS ||--o{ RATINGS : gives
    MOVIES ||--o{ RATINGS : receives
    USERS ||--o{ REVIEWS : writes
    MOVIES ||--o{ REVIEWS : receives
    USERS {
        guid Id PK
        string Email UK
        string DisplayName
        string PasswordHash
    }
    MOVIES {
        guid Id PK
        string Title
        int Genre
    }
    WATCHLIST {
        guid UserId PK,FK
        guid MovieId PK,FK
        int Status
        datetime WatchedAtUtc
    }
    RATINGS {
        guid UserId PK,FK
        guid MovieId PK,FK
        int Value
        datetime UpdatedAtUtc
    }
    REVIEWS {
        guid Id PK
        guid UserId FK
        guid MovieId FK
        string Text
        datetime CreatedAtUtc
        datetime ModifiedAtUtc
    }
~~~

## Идентичност и права

Автентикацията отговаря кой изпраща заявката. Авторизацията проверява дали идентифицираният потребител има право на операцията. `AccountsHandler` използва `PasswordHasher<UserAccount>` от ASP.NET Core Identity. При регистрация базата получава хеш с индивидуална сол и настройки на алгоритъма. При вход библиотеката проверява подадената парола спрямо този хеш.

`JwtAccessTokens` издава подписан HS256 токен с едночасова валидност и `sub` с GUID на потребителя. JWT middleware проверява алгоритъма, издателя, аудиторията, подписа, срока и формата на `sub`. `UserClaims.FindUserId` извлича самоличността от вече валидиран principal. При редакция на ревю `ReviewsHandler` сравнява този GUID с `review.UserId`.

~~~csharp
if (review.UserId != userId) return Result.Failure(Forbidden);
~~~

Промени по каталога изискват политика `CatalogueWrite`. Администраторските GUID се задават от локална конфигурация. [Accounts.cs](../MovieTracker.Application/Users/Accounts.cs), [JwtAccessTokens.cs](../MovieTracker.API/Security/JwtAccessTokens.cs), [Reviews.cs](../MovieTracker.Application/Movies/Reviews/Reviews.cs)

~~~mermaid
sequenceDiagram
    participant B as Blazor
    participant A as AccountsHandler
    participant P as PostgreSQL
    participant J as JWT middleware
    B->>A: HTTPS login с имейл и парола
    A->>P: Търси нормализирания имейл
    P-->>A: PasswordHash
    A->>A: Проверява паролата и подписва JWT
    A-->>B: Access token и срок
    B->>J: Authorization Bearer JWT
    J->>J: Валидира подпис, issuer, audience, срок и sub
    J-->>B: 401 при невалиден токен или резултат от защитения маршрут
~~~

## Оценки, ревюта и статистика

`RatingsHandler.SetAsync` използва `Rating.Create` за правилото 1–10. `RatingRepository.SetAsync` изпълнява параметризиран `INSERT ... ON CONFLICT (...) DO UPDATE`. Дори два паралелни POST-а да стигнат едновременно до PostgreSQL, съставният ключ позволява само един ред за потребител и филм. За ревюто повторението има друг смисъл. `ReviewRepository.AddAsync` разпознава точно нарушението на уникалния индекс и връща `Error.Exist`, което `ResultHttp` превръща в HTTP 409.

~~~mermaid
sequenceDiagram
    participant B as Blazor
    participant E as RatingsEndpoints
    participant H as RatingsHandler
    participant R as RatingRepository
    participant D as PostgreSQL
    B->>E: POST ratings с Bearer и rating
    Note over E: JWT и ValidationFilter са преди handler-а
    E->>H: GUID от sub, MovieId, request
    H->>H: Rating.Create и проверка на филма
    H->>R: SetAsync
    R->>D: INSERT ON CONFLICT DO UPDATE
    D-->>R: Един ред за потребител и филм
    E-->>B: 204 No Content
~~~

При `IQueryable` извикванията `Where`, `GroupBy` и `OrderBy` изграждат израз. EF Core го превежда към SQL при `ToListAsync`, `AverageAsync` или друга крайна операция. Например `RatingRepository.TopAsync` оставя групирането, филтъра, подредбата и лимита на PostgreSQL.

~~~csharp
var top = db.Ratings.GroupBy(x => x.MovieId)
    .Where(g => g.Count() >= 3)
    .Select(g => new { MovieId = g.Key, Average = g.Average(x => x.Value), Count = g.Count() })
    .OrderByDescending(x => x.Average).ThenByDescending(x => x.Count).ThenBy(x => x.MovieId).Take(10);
~~~

Любим жанр е жанрът с най-много филми в статус `Watched`. При равенство печели по-малката числова стойност в `MovieGenre`. Средната лична оценка включва всички оценки на потребителя. При липса на оценки стойността е `null`.

`WatchlistRepository.SetAsync` запазва вече записания `WatchedAtUtc` при повторно `Watched`, включително `null` за стари редове. Излизането от `Watched` изчиства датата. Ново преминаване към `Watched` записва нова UTC дата. `StatisticsHandler` подава полуотворен UTC интервал от началото на текущия месец до началото на следващия. [RatingRepository.cs](../MovieTracker.Infrastructure/Repositories/RatingRepository.cs), [WatchlistRepository.cs](../MovieTracker.Infrastructure/Repositories/WatchlistRepository.cs), [StatisticsRepository.cs](../MovieTracker.Infrastructure/Repositories/StatisticsRepository.cs)

## DI, extension methods и старт

Extension method се дефинира като статичен метод с `this` пред първия параметър. Това позволява `builder.Services.AddMovieTrackerApplication()` да се чете като операция на `IServiceCollection`.

Reflection чете метаданните на компилираната assembly. `ApplicationServices` открива конкретни публични типове със суфикс `Handler` и реализации на `IValidator<T>`. `InfrastructureServices` открива публични класове със суфикс `Repository` в namespace-а на repositories и свързва техните Application интерфейси. DI контейнерът създава обектите и попълва зависимостите им при разрешаване. `TryAddScoped` и `TryAddEnumerable` правят повторното сканиране без дубликати. Scoped lifetime означава една инстанция на регистрация в рамките на заявката.

~~~mermaid
flowchart TD
    P["Program.cs създава builder"] --> A["AddMovieTrackerApplication"]
    A --> I["AddMovieTrackerInfrastructure"]
    I --> S["AddMovieTrackerApi"]
    S --> B["Build"]
    B --> M["UseMovieTrackerMiddleware"]
    M --> E["MapMovieTrackerEndpoints"]
    E --> D{"Database:MigrateOnStartup?"}
    D -->|да| F["Scoped DbContext · MigrateAsync"]
    D -->|не| R["Run · приема заявки"]
    F --> R
~~~

[Program.cs](../MovieTracker.API/Program.cs), [ApiStartup.cs](../MovieTracker.API/Startup/ApiStartup.cs), [ApplicationServices.cs](../MovieTracker.Application/ApplicationServices.cs), [InfrastructureServices.cs](../MovieTracker.Infrastructure/InfrastructureServices.cs)

## Валидация, Result и обработка на грешки

Middleware обвива последващите етапи. `UseExceptionHandler` регистрира механизма, който улавя изключенията. Следват HTTPS, CORS, автентикация и авторизация. Валидирането в `ValidationFilter` се изпълнява за API групата след проверката за достъп. Филтърът обхожда вече свързаните аргументи, намира валидатори и извиква `ValidateAsync` с `RequestAborted`. `[AsParameters]` свързва query параметрите за `GetMovieListRequest`. Тялото на PUT/POST се десериализира от ASP.NET Core.

~~~mermaid
flowchart TD
    A["Защитен маршрут"] --> B{"Валиден достъп?"}
    B -->|не| C["401 или 403"]
    B -->|да| D["ASP.NET Core свързва body или query"]
    P["Публичен маршрут"] --> D
    D --> E["ValidationFilter намира IValidator"]
    E --> F["ValidateAsync с RequestAborted"]
    F --> G{"Грешки?"}
    G -->|да| H["400 ValidationProblem"]
    G -->|не| I["Endpoint извиква handler"]
~~~

`MovieWriteValidator<TRequest>` съдържа общите правила за Create и Update. Очакван бизнес отказ се връща чрез `Result` и `Error`. Например липсващ филм връща `Error.NotFound` и статус 404. `ApiExceptionHandler` обработва неочакваните изключения и връща 500 ProblemDetails с `traceId`. В .NET 8 middleware записва изключението в сървърните логове. Клиентът получава общо заглавие без вътрешна диагностика.

[ValidationFilter.cs](../MovieTracker.API/Startup/ValidationFilter.cs), [ResultHttp.cs](../MovieTracker.Application/ResultHttp.cs), [ApiExceptionHandler.cs](../MovieTracker.API/Startup/ApiExceptionHandler.cs)

## Mapster и решения за границите

`ResponseMapping` сканира Application assembly за `IRegister`. `GetMovieListMapping` преобразува `Movie` към DTO с име на жанра. `ReviewsMapping` използва двойка от `Review` и GUID на зрителя, за да изчисли `IsMine`. Фабриките на домейна продължават да създават обектите при запис. DTO за отговор съдържа само полета за клиента.

~~~csharp
config.NewConfig<Movie, MovieListItemResponse>().MapToConstructor(true)
    .Map(dest => dest.Genre, source => source.Genre.ToString());
~~~

Ръчно регистрираните зависимости са лесни за проследяване при малък проект. Сканирането премахва повтарящи се регистрации и въвежда изискване класовете да следват конвенцията. Маршрутите са изброени на едно място в `ApiStartup`. За обработката на грешки API използва изключения за неочаквани сривове и `Result` за предвидими бизнес случаи. [ResponseMapping.cs](../MovieTracker.Application/Common/ResponseMapping.cs), [GetMovieList.Mapping.cs](../MovieTracker.Application/Movies/GetMovieList/GetMovieList.Mapping.cs)

## Ограничения на учебната конфигурация

Токенът се пази в паметта на Blazor и презареждането прекратява клиентската сесия. След изход издаденият JWT остава валиден до изтичането си. Регистрацията няма потвърждение на имейл. Входът няма rate limiting или блокиране при поредица от грешни опити. Преди публично внедряване добави ограничение на честотата, управление на ключове, потвърждение на адреси и политика за отнемане на токени. Приемай заявки само през HTTPS, ограничи CORS до реалния Web origin и ползвай отделна PostgreSQL роля за API с права, нужни за работата му.

Сравни публичния и личния изглед към един филм с [API справката](API.md) и виж отделното учебно решение за [Movie Insights](adr/0001-movie-insights-demo-data.md).
