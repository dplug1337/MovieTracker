# Локално стартиране

Нужни са .NET 8 SDK, Docker Desktop с Linux Engine и PowerShell. Изпълни командите от корена на хранилището.

## Първоначална настройка

За нов PostgreSQL volume генерирай парола и JWT ключ. При вече настроена база използвай съществуващите стойности от User Secrets. Промяната на променливата за Compose след инициализацията на volume не сменя паролата в PostgreSQL.

~~~powershell
dotnet dev-certs https --trust
$env:MOVIETRACKER_DB_PASSWORD = [Guid]::NewGuid().ToString('N')
$connection = "Host=127.0.0.1;Port=55432;Database=movietracker;Username=movietracker;Password=$env:MOVIETRACKER_DB_PASSWORD"
dotnet user-secrets set 'ConnectionStrings:MovieTracker' $connection --project MovieTracker.API
dotnet user-secrets set 'Jwt:SigningKey' ([Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')) --project MovieTracker.API
docker compose up -d --wait
~~~

При настроен PostgreSQL извлечи паролата от локалната конфигурация, без да я изписваш в терминала. Използвай същия блок преди команди с `docker compose` в нов терминал.

~~~powershell
$config = Get-Content "$env:APPDATA/Microsoft/UserSecrets/MovieTracker.LocalDevelopment/secrets.json" -Raw | ConvertFrom-Json
$connection = [System.Data.Common.DbConnectionStringBuilder]::new()
$connection.ConnectionString = $config.'ConnectionStrings:MovieTracker'
$env:MOVIETRACKER_DB_PASSWORD = $connection['Password']
docker compose up -d --wait
~~~

## Стартиране

В първи терминал изпълни командите за API. Включването на миграции при старт е изрична настройка за локална разработка.

~~~powershell
$env:Database__MigrateOnStartup = 'true'
dotnet run --project MovieTracker.API --launch-profile https
~~~

Във втори терминал стартирай Blazor.

~~~powershell
dotnet run --project MovieTracker.Web --launch-profile https
~~~

Отвори [MovieTracker](https://localhost:7002) и провери [каталога през API](https://localhost:7003/api/movies?page=1&pageSize=3). Регистрирай се през страницата Join free и избери филм. Базата остава в именуван Docker volume. При следващо отваряне на приложението ползвай същия акаунт.

Ако искаш да приложиш миграциите отделно от стартирането, инсталирай `dotnet-ef` версия `8.0.26` и изпълни следното.

~~~powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project MovieTracker.Infrastructure --startup-project MovieTracker.API
~~~

## Тестове

~~~powershell
dotnet build MovieTracker.sln
dotnet test MovieTracker.sln --no-build --results-directory "$env:TEMP/MovieTracker-TestResults"
~~~

За PostgreSQL теста създай отделна база. Ако `movietracker_test` вече съществува, пропусни `createdb` и използвай същата база. Интеграционният тест прилага миграциите в тестовата база.

~~~powershell
docker compose exec postgres createdb -U movietracker movietracker_test
$config = Get-Content "$env:APPDATA/Microsoft/UserSecrets/MovieTracker.LocalDevelopment/secrets.json" -Raw | ConvertFrom-Json
$env:MOVIETRACKER_TEST_CONNECTION = $config.'ConnectionStrings:MovieTracker'.Replace('Database=movietracker;', 'Database=movietracker_test;')
dotnet test MovieTracker.sln --no-build --results-directory "$env:TEMP/MovieTracker-TestResults"
~~~

Извън Development задай `ConnectionStrings__MovieTracker`, `Jwt__SigningKey` и `Cors__AllowedOrigins__0` чрез конфигурацията на средата. Изпълнявай миграциите еднократно преди пускане на новата версия.
