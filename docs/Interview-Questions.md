# Въпроси за интервю и защита

> Този материал описва началния етап на проекта. Виж [реализацията за Exercises 02–06](Exercises-02-06.md) за текущия backend и PostgreSQL.

1. **Как стига филм от API до екрана?** `Movies.razor` извиква `MovieApiClient.GetMoviesAsync`. Endpoint-ът валидира заявката, handler-ът взема страница чрез `IMovieRepository`, а клиентът чете JSON и рендерира карти. [Жизнен цикъл](Request-Lifecycle.md)
2. **Какво става при `page=0&pageSize=51`?** FluentValidation открива две нарушения. Endpoint-ът връща 400 и имена на двете свойства в `errors`. Handler-ът се извиква само при успех. [Endpoint](../MovieTracker.Application/Movies/GetMovieList/GetMovieList.Endpoint.cs)
3. **Защо при страница 2 с размер 3 се пропускат точно три филма?** Страниците започват от 1. Формулата `(2 - 1) * 3` прескача първата страница. [Handler тест](../tests/MovieTracker.UnitTests/Movies/GetMovieList/GetMovieListHandlerTests.cs)
4. **Защо интерфейс между handler и repository?** Той описва нужните операции и позволява API да избере реализация. Unit тестът подава контролиран заместител. [Архитектура](Architecture.md)
5. **Защо Web има свой модел?** Браузърът чете HTTP JSON. Съвместимите имена и типове са част от транспортния договор. Web няма нужда от сървърните assemblies. [Web модели](../MovieTracker.Web/Models/Movies)
6. **Кога record е удобен?** При обекти, чието значение е в стойностите. Handler тестът сравнява получения record с нов record със същите полета. [C# основи](CSharp-Fundamentals.md)
7. **Създава ли `await` нова нишка?** Самият `await` продължава изпълнението, когато задачата приключи. Хранилището връща вече завършени задачи, а HTTP клиентът изчаква реална асинхронна операция. [Хранилище](../tests/MovieTracker.IntegrationTests/Movies/TestMovieRepository.cs), [Клиент](../MovieTracker.Web/Services/Movies/MovieApiClient.cs)
8. **Какво става, ако заменим `ThenBy` с `OrderBy`?** Заглавието става нов основен ключ. Искаме годината да остане първа, а заглавието да разрешава равенството. [LINQ](LINQ.md)
9. **Защо има два тестови проекта?** Unit тестовете изолират правила и handler. Integration тестовете проверяват HTTP статуса, JSON и реалното свързване на API слоя с хранилището. [Тестове](Testing.md)
10. **CORS предпазва ли чужд потребителски ресурс?** CORS управлява достъпа от браузърни origins. Потребителският достъп изисква валидирана идентичност и сървърно разрешение. [Сигурност](Security-Fundamentals.md)
11. **Какво би се променило при бъдеща база данни?** Нова repository реализация би извличала данните от база и би се регистрирала в API. Схемата и поведението при едновременен достъп ще изискват отделно проектиране. [Архитектура](Architecture.md)
12. **Защо commit и push са различни?** Commit създава локален запис, а push публикува локални commits към remote. [Git](Git-Workflow.md)

За устна демонстрация използвай [сценария за представяне](presentation.md).
