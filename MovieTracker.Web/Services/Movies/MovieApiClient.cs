using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MovieTracker.Web.Models.Movies;

namespace MovieTracker.Web.Services.Movies;

public sealed class MovieApiClient(HttpClient httpClient)
{
    private LoginSession? session;

    public event Action? SessionChanged;

    public bool IsSignedIn => session is not null && session.ExpiresAtUtc > DateTime.UtcNow;
    public string? DisplayName => IsSignedIn ? session!.DisplayName : null;

    public Task<MovieListResponseModel> GetMoviesAsync(int page = 1, int pageSize = 24, CancellationToken cancellationToken = default) =>
        ReadAsync<MovieListResponseModel>($"api/movies?page={page}&pageSize={pageSize}", cancellationToken);

    public Task<MovieDetailsModel> GetDetailsAsync(Guid movieId, CancellationToken ct) =>
        ReadAsync<MovieDetailsModel>($"api/movies/{movieId}", ct);

    public Task<ReviewModel[]> GetReviewsAsync(Guid movieId, CancellationToken ct) =>
        ReadAsync<ReviewModel[]>($"api/movies/{movieId}/reviews", ct);

    public Task<PersonalRatingModel[]> GetRatingsAsync() => ReadAsync<PersonalRatingModel[]>("api/users/me/ratings");

    public Task<StatisticsModel> GetStatisticsAsync() => ReadAsync<StatisticsModel>("api/users/me/statistics");

    public Task<TopRatedMovieModel[]> GetTopRatedAsync() => ReadAsync<TopRatedMovieModel[]>("api/movies/top-rated");

    public Task<WatchlistModel[]> GetWatchlistAsync(CancellationToken ct = default) =>
        ReadAsync<WatchlistModel[]>("api/users/me/watchlist", ct);

    public Task RateAsync(Guid movieId, int rating) =>
        WriteAsync(HttpMethod.Post, $"api/movies/{movieId}/ratings", new { rating });

    public Task AddReviewAsync(Guid movieId, string text) =>
        WriteAsync(HttpMethod.Post, $"api/movies/{movieId}/reviews", new { text });

    public Task EditReviewAsync(Guid movieId, Guid reviewId, string text) =>
        WriteAsync(HttpMethod.Put, $"api/movies/{movieId}/reviews/{reviewId}", new { text });

    public Task DeleteReviewAsync(Guid movieId, Guid reviewId) =>
        WriteAsync(HttpMethod.Delete, $"api/movies/{movieId}/reviews/{reviewId}");

    public Task SetWatchlistAsync(Guid movieId, int status) =>
        WriteAsync(HttpMethod.Put, $"api/users/me/watchlist/{movieId}", new { status });

    public Task RemoveWatchlistAsync(Guid movieId) =>
        WriteAsync(HttpMethod.Delete, $"api/users/me/watchlist/{movieId}");

    public Task LoginAsync(string email, string password) =>
        StartSessionAsync("api/auth/login", new { email, password });

    public Task RegisterAsync(string email, string displayName, string password) =>
        StartSessionAsync("api/auth/register", new { email, displayName, password });

    public void Logout()
    {
        session = null;
        SessionChanged?.Invoke();
    }

    private async Task StartSessionAsync(string url, object body)
    {
        using var response = await SendAsync(HttpMethod.Post, url, body, includeSession: false);
        session = await ReadBodyAsync<LoginSession>(response, CancellationToken.None);
        SessionChanged?.Invoke();
    }

    private async Task<T> ReadAsync<T>(string url, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, url, ct: ct);
        return await ReadBodyAsync<T>(response, ct);
    }

    private static async Task<T> ReadBodyAsync<T>(HttpResponseMessage response, CancellationToken ct) =>
        await response.Content.ReadFromJsonAsync<T>(ct)
            ?? throw new HttpRequestException("The movie API returned an empty response.");

    private async Task WriteAsync(HttpMethod method, string url, object? body = null)
    {
        using var response = await SendAsync(method, url, body);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null,
        CancellationToken ct = default, bool includeSession = true)
    {
        using var request = new HttpRequestMessage(method, url);
        if (includeSession && IsSignedIn) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        if (body is not null) request.Content = JsonContent.Create(body);
        var response = await httpClient.SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return response;

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized && includeSession && session is not null)
                Logout();
            throw new HttpRequestException(await DescribeErrorAsync(response, includeSession, ct), null, response.StatusCode);
        }
    }

    private static async Task<string> DescribeErrorAsync(HttpResponseMessage response, bool includeSession, CancellationToken ct)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                return includeSession ? "Log in to continue." : "The email or password is incorrect.";
            case HttpStatusCode.Forbidden:
                return "You do not have permission to make this change.";
            case HttpStatusCode.NotFound:
                return "The requested item could not be found.";
            case HttpStatusCode.Conflict:
                return includeSession ? "You already reviewed this movie." : "An account with that email already exists.";
            case HttpStatusCode.BadRequest:
                if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
                {
                    var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
                    if (problem.TryGetProperty("errors", out var errors))
                        return string.Join(" ", errors.EnumerateObject().SelectMany(field =>
                            field.Value.EnumerateArray().Select(message => message.GetString())));
                }
                return "Check the information and try again.";
            default:
                return "The movie service is temporarily unavailable.";
        }
    }
}
