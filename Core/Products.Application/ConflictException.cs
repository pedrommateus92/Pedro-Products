namespace Products.Application;

/// <summary>
/// The request is valid but conflicts with the current state of the resource (HTTP 409).
/// </summary>
public sealed class ConflictException(string message) : Exception(message);
