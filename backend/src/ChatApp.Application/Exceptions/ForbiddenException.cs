namespace ChatApp.Application.Exceptions;

/// <summary>Mapped to HTTP 403: the user exists but is not allowed to do this.</summary>
public sealed class ForbiddenException(string message) : Exception(message);
