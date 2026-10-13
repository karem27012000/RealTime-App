namespace ChatApp.Domain.Exceptions;

/// <summary>
/// Thrown when a business rule is broken (for example: sending an empty message).
/// The API turns it into a 400 Bad Request.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
