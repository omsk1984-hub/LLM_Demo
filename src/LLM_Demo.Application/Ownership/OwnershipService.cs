namespace LLM_Demo.Application.Ownership;

using LLM_Demo.Domain.Ownership;

public sealed class OwnershipService
{
    /// <summary>
    /// Verifies that the given user owns the ownable entity.
    /// Throws UnauthorizedAccessException if ownership check fails.
    /// </summary>
    public void VerifyOwnership(IOwnable ownable, string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (!string.Equals(ownable.OwnerId, userId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException(
                $"User '{userId}' does not own this resource.");
    }

    /// <summary>
    /// Checks ownership without throwing. Returns true if owner matches.
    /// </summary>
    public bool IsOwner(IOwnable ownable, string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        return string.Equals(ownable.OwnerId, userId, StringComparison.Ordinal);
    }
}
