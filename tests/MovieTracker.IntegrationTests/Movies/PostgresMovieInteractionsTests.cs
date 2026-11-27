using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieTracker.Application.Movies.Ratings;
using MovieTracker.Application.Movies.Reviews;
using MovieTracker.Application.Users;
using MovieTracker.Application.Users.Statistics;
using MovieTracker.Infrastructure.Persistence;

namespace MovieTracker.IntegrationTests.Movies;

[TestFixture]
public sealed class PostgresMovieInteractionsTests
{
    private const string First = "00000000-0000-0000-0000-000000000001";
    private const string Second = "00000000-0000-0000-0000-000000000002";
    private static string Movies(string id) => $"/api/movies/{id}";
    private const string MyRatings = "/api/users/me/ratings";
    private const string MyStatistics = "/api/users/me/statistics";
    private const string Watchlist = "/api/users/me/watchlist";

    private sealed class PostgresFactory(string connection, Guid adminId) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:MovieTracker", connection);
            builder.UseSetting("Jwt:SigningKey", Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
            builder.UseSetting("Jwt:AdministratorIds:0", adminId.ToString());
        }
    }

    [Test]
    public async Task MovieInteractions_UsePostgresAndEnforceHttpContracts()
    {
        var connection = Environment.GetEnvironmentVariable("MOVIETRACKER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) Assert.Ignore("Set MOVIETRACKER_TEST_CONNECTION to an isolated PostgreSQL database.");
        if (!new Npgsql.NpgsqlConnectionStringBuilder(connection).Database!.EndsWith("_test", StringComparison.Ordinal))
            Assert.Fail("Use a dedicated PostgreSQL database whose name ends in _test.");

        var adminId = Guid.NewGuid();
        await using var factory = new PostgresFactory(connection!, adminId);
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<MovieTrackerDbContext>();
            await database.Database.MigrateAsync();
            Assert.That(await database.Database.GetPendingMigrationsAsync(), Is.Empty);
        }

        using var guest = factory.CreateClient();
        using var alice = factory.CreateClient();
        using var bob = factory.CreateClient();
        using var carol = factory.CreateClient();
        await Register(alice, "Alice");
        await Register(bob, "Bob");
        await Register(carol, "Carol");

        Assert.That((await guest.PostAsJsonAsync(Movies(First) + "/ratings", new { rating = 0 })).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await guest.GetAsync(MyRatings)).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await guest.GetAsync(MyStatistics)).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await alice.PostAsJsonAsync("/api/movies", new { })).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var empty = await guest.GetFromJsonAsync<MovieDetailsResponse>(Movies(First));
        Assert.That(empty!.PersonalRating, Is.Null);
        Assert.That((await guest.GetAsync(Movies(Guid.Empty.ToString()) + "/reviews")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var newMovie = await CreateTestMovie(factory, adminId);
        var noRatings = await guest.GetFromJsonAsync<MovieDetailsResponse>(Movies(newMovie));
        Assert.That(noRatings!.AverageRating, Is.Null);
        Assert.That(noRatings.RatingsCount, Is.Zero);
        Assert.That(noRatings.PersonalRating, Is.Null);

        var ratingsRoute = Movies(newMovie) + "/ratings";
        foreach (var invalid in new[] { 0, 11 })
            Assert.That((await alice.PostAsJsonAsync(ratingsRoute, new { rating = invalid })).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        Assert.That((await alice.PostAsJsonAsync(ratingsRoute, new { rating = 1 })).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await alice.PostAsJsonAsync(ratingsRoute, new { rating = 10 })).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var concurrentRatings = await Task.WhenAll(Enumerable.Range(1, 8).Select(rating => alice.PostAsJsonAsync(ratingsRoute, new { rating })));
        Assert.That(concurrentRatings.All(x => x.StatusCode == HttpStatusCode.NoContent), Is.True);
        var own = await alice.GetFromJsonAsync<PersonalRatingResponse[]>(MyRatings);
        Assert.That(own, Has.Length.EqualTo(1));
        Assert.That((await bob.GetFromJsonAsync<PersonalRatingResponse[]>(MyRatings)), Is.Empty);

        var secondMovie = await CreateTestMovie(factory, adminId);
        var secondRatings = Movies(secondMovie) + "/ratings";
        foreach (var route in new[] { ratingsRoute, secondRatings })
        {
            foreach (var client in new[] { alice, bob })
                Assert.That((await client.PostAsJsonAsync(route, new { rating = 10 })).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            var excluded = await guest.GetFromJsonAsync<TopRatedMovieResponse[]>("/api/movies/top-rated");
            Assert.That(excluded!.Any(x => Movies(x.Id.ToString()) + "/ratings" == route), Is.False);
            Assert.That((await carol.PostAsJsonAsync(route, new { rating = 10 })).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }
        var top = await guest.GetFromJsonAsync<TopRatedMovieResponse[]>("/api/movies/top-rated");
        Assert.That(top!.Where(x => x.Id == Guid.Parse(newMovie) || x.Id == Guid.Parse(secondMovie)).Select(x => x.Id.ToString()), Is.EqualTo(new[] { newMovie, secondMovie }.Order(StringComparer.Ordinal)));
        var details = await alice.GetFromJsonAsync<MovieDetailsResponse>(Movies(newMovie));
        Assert.That(details!.RatingsCount, Is.EqualTo(3));
        Assert.That(details.AverageRating, Is.EqualTo(10));
        Assert.That(details.PersonalRating, Is.EqualTo(10));

        var reviewRoute = Movies(newMovie) + "/reviews";
        foreach (var length in new[] { 19, 2001 })
            Assert.That((await alice.PostAsJsonAsync(reviewRoute, new { text = $"  {new string('x', length)}  " })).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var minimum = new string('a', 20);
        var reviews = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => alice.PostAsJsonAsync(reviewRoute, new { text = $"  {minimum}  " })));
        Assert.That(reviews.Count(x => x.StatusCode == HttpStatusCode.Created), Is.EqualTo(1));
        Assert.That(reviews.Count(x => x.StatusCode == HttpStatusCode.Conflict), Is.EqualTo(3));
        var maximum = new string('b', 2000);
        Assert.That((await bob.PostAsJsonAsync(reviewRoute, new { text = $"  {maximum}  " })).StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var publicReviews = await guest.GetFromJsonAsync<ReviewResponse[]>(reviewRoute);
        Assert.That(publicReviews!.Select(x => x.Text), Is.EqualTo(new[] { maximum, minimum }));
        var ownReview = publicReviews![1];
        Assert.That(ownReview.AuthorName, Is.EqualTo("Alice"));
        Assert.That(ownReview.IsMine, Is.False);
        var editRoute = reviewRoute + "/" + ownReview.Id;
        Assert.That((await bob.PutAsJsonAsync(editRoute, new { text = minimum })).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await bob.DeleteAsync(editRoute)).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await alice.DeleteAsync(Movies(secondMovie) + "/reviews/" + ownReview.Id)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await alice.PutAsJsonAsync(editRoute, new { text = $"  {maximum}  " })).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var edited = (await alice.GetFromJsonAsync<ReviewResponse[]>(reviewRoute))!.Single(x => x.Id == ownReview.Id);
        Assert.That(edited.Text, Is.EqualTo(maximum));
        Assert.That(edited.ModifiedAtUtc, Is.Not.Null);
        Assert.That(edited.IsMine, Is.True);
        Assert.That((await alice.DeleteAsync(editRoute)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        await SetWatchStatus(alice, newMovie, 3);
        var started = (await alice.GetFromJsonAsync<WatchlistResponse[]>(Watchlist))!.Single().WatchedAtUtc;
        Assert.That(started, Is.Not.Null);
        await SetWatchStatus(alice, newMovie, 3);
        Assert.That((await alice.GetFromJsonAsync<WatchlistResponse[]>(Watchlist))!.Single().WatchedAtUtc, Is.EqualTo(started));
        var stats = await alice.GetFromJsonAsync<StatisticsResponse>(MyStatistics);
        Assert.That(stats, Is.EqualTo(new StatisticsResponse(1, 10, "Drama", 1)));

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<MovieTrackerDbContext>();
            var now = DateTime.UtcNow;
            var previousMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(-1);
            await database.Watchlist.Where(x => x.MovieId == Guid.Parse(newMovie))
                .ExecuteUpdateAsync(x => x.SetProperty(r => r.WatchedAtUtc, previousMonth));
            Assert.That(await database.Ratings.CountAsync(x => x.MovieId == Guid.Parse(newMovie)), Is.EqualTo(3));
        }
        stats = await alice.GetFromJsonAsync<StatisticsResponse>(MyStatistics);
        Assert.That(stats!.WatchedThisMonthCount, Is.Zero);
        Assert.That(stats.WatchedMoviesCount, Is.EqualTo(1));
        Assert.That((await bob.GetFromJsonAsync<WatchlistResponse[]>(Watchlist)), Is.Empty);
        await SetWatchStatus(alice, newMovie, 2);
        Assert.That((await alice.GetFromJsonAsync<WatchlistResponse[]>(Watchlist))!.Single().WatchedAtUtc, Is.Null);
        await SetWatchStatus(alice, newMovie, 3);
        Assert.That((await alice.GetFromJsonAsync<StatisticsResponse>(MyStatistics))!.WatchedThisMonthCount, Is.EqualTo(1));

        var snapshot = Environment.GetEnvironmentVariable("MOVIETRACKER_HTTP_SNAPSHOT");
        if (!string.IsNullOrWhiteSpace(snapshot))
        {
            var json = JsonSerializer.Serialize(new { details.AverageRating, details.RatingsCount,
                details.PersonalRating, edited.IsMine, edited.AuthorName, ValidationStatus = 400, ConflictStatus = 409 });
            await File.WriteAllTextAsync(snapshot, json);
        }

        using var admin = CreateAdminClient(factory, adminId);
        Assert.That((await admin.DeleteAsync(Movies(newMovie))).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await admin.DeleteAsync(Movies(secondMovie))).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    private static async Task SetWatchStatus(HttpClient client, string movieId, int status)
    {
        var response = await client.PutAsJsonAsync(Watchlist + "/" + movieId, new { status });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), await response.Content.ReadAsStringAsync());
    }

    private static async Task<string> CreateTestMovie(PostgresFactory factory, Guid adminId)
    {
        using var admin = CreateAdminClient(factory, adminId);
        var response = await admin.PostAsJsonAsync("/api/movies", new
        {
            title = "Test " + Guid.NewGuid(), description = "PostgreSQL integration test", genre = 1,
            releaseYear = 2000, posterUrl = "/images/posters/interstellar.svg"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
        var movie = await response.Content.ReadFromJsonAsync<MovieDetailsResponse>();
        return movie!.Id.ToString();
    }

    private static HttpClient CreateAdminClient(PostgresFactory factory, Guid adminId)
    {
        var client = factory.CreateClient();
        var issuer = factory.Services.GetRequiredService<IAccessTokenIssuer>();
        var user = new MovieTracker.Domain.Users.UserAccount("admin@example.test", "Admin");
        typeof(MovieTracker.Domain.Users.UserAccount).GetProperty(nameof(user.Id))!.SetValue(user, adminId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issuer.Issue(user).AccessToken);
        return client;
    }

    private static async Task Register(HttpClient client, string name)
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        var password = Guid.NewGuid().ToString("N");
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, displayName = name, password });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var token = await login.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }
}
