using System.Security.Cryptography;

namespace fiskaltrust.Middleware.SCU.ES.Helpers;

public record Result<T, E>
{
    public record Ok(T Value) : Result<T, E>();
    public static implicit operator Result<T, E>(T v) => new Result<T, E>.Ok(v);

    public record Err(E Error) : Result<T, E>();
    public static implicit operator Result<T, E>(E e) => new Result<T, E>.Err(e);

    private Result() { } // private constructor can prevent derived cases from being defined elsewhere

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
    public bool IsOk => this switch
    {
        Ok => true,
        Err => false,
    };

    public bool IsErr => this switch
    {
        Ok => false,
        Err => true,
    };

    public T? OkValue => this switch
    {
        Ok ok => ok.Value,
        Err => default,
    };

    public E? ErrValue => this switch
    {
        Ok => default,
        Err err => err.Error,
    };

    public void Match(
        Action<T> success,
        Action<E> failure
    )
    {
        Action action = this switch
        {
            Ok ok => () => success(ok.Value),
            Err err => () => failure(err.Error)
        };

        action();
    }

    public R Match<R>(
        Func<T, R> success,
        Func<E, R> failure
    ) => this switch
    {
        Ok ok => success(ok.Value),
        Err err => failure(err.Error)
    };

    public Result<R, E> Map<R>(Func<T, R> success) => this switch
    {
        Ok ok => success(ok.Value),
        Err err => err.Error
    };

    public async Task<Result<R, E>> MapAsync<R>(Func<T, Task<R>> success) => this switch
    {
        Ok ok => await success(ok.Value),
        Err err => err.Error
    };

    public Result<T, R> MapErr<R>(Func<E, R> failure) => this switch
    {
        Ok ok => ok.Value,
        Err err => failure(err.Error)
    };

    public Result<R, E> AndThen<R>(Func<T, Result<R, E>> success) => this switch
    {
        Ok ok => success(ok.Value),
        Err err => err.Error
    };
    public async Task<Result<R, E>> AndThenAsync<R>(Func<T, Task<Result<R, E>>> success) => this switch
    {
        Ok ok => await success(ok.Value),
        Err err => err.Error
    };
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
}