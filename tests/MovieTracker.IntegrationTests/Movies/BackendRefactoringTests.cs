using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MovieTracker.Application;
using MovieTracker.Application.Common.Repositories;
using MovieTracker.Application.Movies.GetMovieList;
using MovieTracker.Application.Movies.Ratings;
using MovieTracker.Domain.Movies;
using MovieTracker.Infrastructure;
using MovieTracker.Infrastructure.Persistence;
using MovieTracker.Infrastructure.Repositories;

namespace MovieTracker.IntegrationTests.Movies;

[TestFixture]
public sealed class BackendRefactoringTests
{
    [Test]
    public void AssemblyScanningRegistersScopedServicesOnce()
    {
        var services = new ServiceCollection();
        for (var i = 0; i < 2; i++)
        {
            services.AddMovieTrackerApplication();
            services.AddMovieTrackerInfrastructure(new ConfigurationBuilder().Build());
        }

        AssertScoped<GetMovieListHandler, GetMovieListHandler>();
        AssertScoped<IValidator<GetMovieListRequest>, GetMovieListValidator>();
        AssertScoped<IRatingRepository, RatingRepository>();
        AssertScoped<IMovieRepository, PostgresMovieRepository>();
        AssertScoped<MovieTrackerDbContext, MovieTrackerDbContext>();

        void AssertScoped<TContract, TImplementation>()
        {
            var descriptor = services.Single(s => s.ServiceType == typeof(TContract));
            Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(TImplementation)));
            Assert.That(descriptor.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
        }
    }

    [Test]
    public async Task ValidationShortCircuitsAndUnexpectedErrorsAreSafe()
    {
        var repository = new ThrowingMovieRepository();
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:SigningKey", Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
            builder.ConfigureServices(services =>
                services.Replace(ServiceDescriptor.Singleton<IMovieRepository>(repository)));
        });

        using var client = factory.CreateClient();
        using var query = await client.GetAsync("/api/movies?page=-1");
        Assert.That(query.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(repository.Calls, Is.Zero);

        using var body = await client.PostAsJsonAsync("/api/auth/register", new { email = "invalid", displayName = "", password = "short" });
        Assert.That(body.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        using var validation = JsonDocument.Parse(await body.Content.ReadAsStringAsync());
        Assert.That(validation.RootElement.GetProperty("errors").TryGetProperty("Email", out _), Is.True);

        using var unauthorized = await client.PostAsJsonAsync("/api/movies/00000000-0000-0000-0000-000000000001/ratings", new { rating = 0 });
        Assert.That(unauthorized.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        using var failure = await client.GetAsync("/api/movies");
        Assert.That(failure.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(repository.Calls, Is.EqualTo(1));
        Assert.That(failure.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/problem+json"));
        var text = await failure.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(text);
        Assert.That(problem.RootElement.GetProperty("traceId").GetString(), Is.Not.Empty);
        Assert.That(problem.RootElement.GetProperty("status").GetInt32(), Is.EqualTo(500));
        Assert.That(text, Does.Not.Contain("private diagnostic"));
    }

    private sealed class ThrowingMovieRepository : IMovieRepository
    {
        public int Calls { get; private set; }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            throw new InvalidOperationException("private diagnostic");
        }

        public Task<IReadOnlyList<Movie>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("private diagnostic");
    }
}
