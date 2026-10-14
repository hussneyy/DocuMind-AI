namespace DocuMind.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource doesn't exist, OR exists but isn't owned by the caller.
/// Deliberately indistinguishable from "doesn't exist" so clients can't probe for other users'
/// resource IDs.
/// </summary>
public class NotFoundException(string message) : Exception(message)
{
}
