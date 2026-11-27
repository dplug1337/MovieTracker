using MovieTracker.Application.Movies.Ratings;
using MovieTracker.Application.Movies.Reviews;
using MovieTracker.Application.Users.Statistics;
using MovieTracker.API.Security;
using MovieTracker.Application.Users;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MovieTracker.Application.Common.Repositories;
using MovieTracker.Application.Movies;
using MovieTracker.Application.Movies.GetMovieList;
using MovieTracker.Infrastructure.Persistence;
using MovieTracker.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MovieTrackerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MovieTracker")));
builder.Services.AddScoped<IMovieRepository, PostgresMovieRepository>();
builder.Services.AddScoped<IMovieCatalogRepository, PostgresMovieRepository>();
builder.Services.AddScoped<GetMovieListHandler>();
builder.Services.AddScoped<IValidator<GetMovieListRequest>, GetMovieListValidator>();
builder.Services.AddScoped<MovieCatalogHandler>();
builder.Services.AddScoped<IValidator<CreateMovieRequest>, CreateMovieValidator>();
builder.Services.AddScoped<IValidator<UpdateMovieRequest>, UpdateMovieValidator>();

JwtAccessTokens.AddMovieTrackerSecurity(builder.Services, builder.Configuration);
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
builder.Services.AddScoped<AccountsHandler>();
builder.Services.AddScoped<WatchlistHandler>();
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginValidator>();
builder.Services.AddScoped<IValidator<SetWatchlistRequest>, SetWatchlistValidator>();

builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();
builder.Services.AddScoped<RatingsHandler>();
builder.Services.AddScoped<ReviewsHandler>();
builder.Services.AddScoped<StatisticsHandler>();
builder.Services.AddScoped<IValidator<RateMovieRequest>, RateMovieValidator>();
builder.Services.AddScoped<IValidator<WriteReviewRequest>, WriteReviewValidator>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("MovieTrackerWeb", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("MovieTrackerWeb");
app.UseAuthentication();
app.UseAuthorization();
app.MapAccounts();
app.MapWatchlist();
app.MapGetMovieListEndpoint();
app.MapMovieCatalog();
app.MapRatings();
app.MapReviews();
app.MapStatistics();

app.Run();

public partial class Program;
