# API справка

Базов локален адрес е `https://localhost:7003`. За защитени маршрути изпрати `Authorization: Bearer <accessToken>`. JSON полетата са в camelCase. Датите са UTC.

| Метод и маршрут | Достъп | Тяло и успешен резултат |
| --- | --- | --- |
| `GET /api/movies?page=1&pageSize=12` | Публичен | `200` с `items`, `page`, `pageSize`, `totalCount` |
| `GET /api/movies/{id}` | Публичен | `200` с основните полета и `averageRating`, `ratingsCount`, `personalRating` |
| `POST /api/movies` | Администратор | `title`, `description`, `genre`, `releaseYear`, `posterUrl`. Връща `201` и `Location` |
| `PUT /api/movies/{id}` | Администратор | Същите полета. Връща `200` с обновения филм |
| `DELETE /api/movies/{id}` | Администратор | `204` |
| `POST /api/auth/register` | Публичен | `email`, `displayName`, `password`. Връща `201` с `accessToken`, `expiresAtUtc`, `displayName` |
| `POST /api/auth/login` | Публичен | `email`, `password`. Връща `200` със същите полета като регистрацията |
| `GET /api/users/me/watchlist` | Вход | `200` със списък от `movie`, `status` и `watchedAtUtc` |
| `PUT /api/users/me/watchlist/{movieId}` | Вход | `{"status":3}`. Връща `204` |
| `DELETE /api/users/me/watchlist/{movieId}` | Вход | `204` |
| `POST /api/movies/{id}/ratings` | Вход | `{"rating":10}`. Връща `204` при запис или актуализация |
| `GET /api/users/me/ratings` | Вход | `200` със списък от `movie`, `rating` и `updatedAtUtc` |
| `GET /api/movies/{id}/reviews` | Публичен | `200` с `id`, `text`, `authorName`, `createdAtUtc`, `modifiedAtUtc`, `isMine` |
| `POST /api/movies/{id}/reviews` | Вход | `{"text":"Този филм заслужава второ гледане."}`. Връща `201` |
| `PUT /api/movies/{movieId}/reviews/{reviewId}` | Автор | `text`. Връща `204` |
| `DELETE /api/movies/{movieId}/reviews/{reviewId}` | Автор | `204` |
| `GET /api/users/me/statistics` | Вход | `200` с `watchedMoviesCount`, `averageRating`, `favouriteGenre`, `watchedThisMonthCount` |
| `GET /api/movies/top-rated` | Публичен | `200` с до 10 филма и полета `averageRating` и `ratingsCount` |

В ресурсните маршрути идентификаторите са GUID. `page` започва от 1, а `pageSize` е между 1 и 50. `genre` при запис е числова стойност от [MovieGenre](../MovieTracker.Domain/Movies/MovieGenre.cs). Статусите за запис във Watchlist са `1` за `WantToWatch`, `2` за `Watching` и `3` за `Watched`. Имената се връщат като низове.

Отговорът за филм съдържа `averageRating: null` при липса на оценки, както и `personalRating: null` за анонимен зрител или зрител без оценка. `isMine` е `false` за анонимен зрител. Преди запис на ревю API trim-ва текста и проверява дължина от 20 до 2000 символа.

| Грешка | Кога се връща |
| --- | --- |
| `400` | Невалидна заявка. FluentValidation връща поле `errors` във ValidationProblem |
| `401` | Липсва валиден токен или данните за вход са грешни |
| `403` | Потребителят е влязъл, но няма роля за каталога или опитва да промени чуждо ревю |
| `404` | Липсва филм или търсеният ресурс под този маршрут |
| `409` | Дублиран имейл или второ ревю за същия филм от същия потребител |
| `500` | Неочаквана грешка. Отговорът е ProblemDetails с `traceId` |

За локално управление на каталога първо създай акаунта, вземи неговия GUID от `sub` на токена и задай `Jwt:AdministratorIds:0` чрез User Secrets. Рестартирай API и влез отново, за да получиш токен с роля `administrator`.
