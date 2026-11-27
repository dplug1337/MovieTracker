using Microsoft.AspNetCore.Http;
using MovieTracker.Domain.Common.Results;

namespace MovieTracker.Application;

public static class ResultHttp
{
    public static IResult ToHttp(this Result result) =>
        result.IsSuccess ? Results.NoContent() : Failure(result.Error);

    public static IResult ToHttp<T>(this Result<T> result, Func<T, IResult>? success = null) =>
        result.IsSuccess ? (success is null ? Results.Ok(result.Value) : success(result.Value!)) : Failure(result.Error);

    private static IResult Failure(Error error) => error.Code switch
    {
        "NotFound" => Results.NotFound(error),
        "Duplicated" => Results.Conflict(error),
        "Forbidden" => Results.Forbid(),
        "Unauthorized" => Results.Unauthorized(),
        _ => Results.BadRequest(error)
    };
}
