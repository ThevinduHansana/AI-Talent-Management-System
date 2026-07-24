namespace RecruitmentPlatform.Application.Common.Exceptions;

/// <summary>
/// Base type for expected application errors. The global exception middleware maps derived
/// types to the appropriate HTTP status codes.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

/// <summary>Requested resource does not exist (maps to 404).</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string name, object key)
        : base($"{name} with identifier '{key}' was not found.") { }
}

/// <summary>Request conflicts with current state, e.g. duplicate (maps to 409).</summary>
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Authenticated user lacks permission for the action (maps to 403).</summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You are not allowed to perform this action.")
        : base(message) { }
}

/// <summary>Authentication failed or is required (maps to 401).</summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Authentication failed.") : base(message) { }
}

/// <summary>Invalid business input (maps to 400) with per-field errors.</summary>
public class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = new[] { error } }) { }
}
