using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MovieTracker.Application.Common.Repositories;
using MovieTracker.IntegrationTests.Movies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MovieTracker.IntegrationTests;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:SigningKey", Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<IMovieRepository, TestMovieRepository>()));
    }
}
