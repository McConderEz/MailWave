using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.SharedKernel.Shared;

/// <summary>
/// Паттерн Result для возвращения результата обработчиков
/// </summary>
public class Result
{
    protected Result(bool isSuccess, IEnumerable<Error> errors)
    {
        if (isSuccess && errors.Any(x => x != Error.None))
            throw new InvalidOperationException();
        
        if (!isSuccess && errors.Any(x => x == Error.None))
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Errors = errors.ToList();
    }
    
    /// <summary>
    /// Список ошибок
    /// </summary>
    public ErrorList Errors { get; set; }
    
    /// <summary>
    /// Булево свойство, успешен ли результат 
    /// </summary>
    public bool IsSuccess { get; }
    
    /// <summary>
    /// Булево свойство, провален ли результат
    /// </summary>
    public bool IsFailure => !IsSuccess;
    
    public static Result Success() => new(true, [Error.None]);
    public static Result Failure(Error error) => new(false, [error]);
    public static implicit operator Result(Error error) => new( false, [error]);
    public static implicit operator Result(ErrorList errors) => new( false, errors);

    public override string ToString()
    {
        return string.Join("\n", Errors);
    }
}

public class Result<TValue> : Result
{
    private Result(TValue value,bool isSuccess, IEnumerable<Error> errors) 
        : base(isSuccess, errors)
    {
        _value = value;
    }
    
    private readonly TValue _value;

    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("The value of a failure result cannot be accessed");

    public static Result<TValue> Success(TValue value) => new(value, true, [Error.None]);
    public new static Result<TValue> Failure(Error error) => new(default!, false, [error]);

    public static implicit operator Result<TValue>(TValue value) => new(value, true, [Error.None]);
    public static implicit operator Result<TValue>(Error error) => new(default!, false, [error]);
    public static implicit operator Result<TValue>(ErrorList errors) => new(default!, false, errors);
}