namespace ChatApp.Application.Exceptions;

/// <summary>Mapped to HTTP 404.</summary>
public sealed class NotFoundException(string entityName, object key)
    : Exception($"{entityName} with id '{key}' was not found.");
