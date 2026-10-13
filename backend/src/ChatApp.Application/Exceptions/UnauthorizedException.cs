namespace ChatApp.Application.Exceptions;

/// <summary>Mapped to HTTP 401: we don't know who the caller is.</summary>
public sealed class UnauthorizedException(string message = "Please log in first.") : Exception(message);
