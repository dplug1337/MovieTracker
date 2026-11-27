using MovieTracker.API.Startup;
using MovieTracker.Application;
using MovieTracker.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMovieTrackerApplication();
builder.Services.AddMovieTrackerInfrastructure(builder.Configuration);
builder.Services.AddMovieTrackerApi(builder.Configuration);

var app = builder.Build();

app.UseMovieTrackerMiddleware();
app.MapMovieTrackerEndpoints();
await app.InitializeMovieTrackerDatabaseAsync();

app.Run();

public partial class Program;
