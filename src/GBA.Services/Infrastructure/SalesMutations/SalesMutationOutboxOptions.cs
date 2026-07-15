using System;

namespace GBA.Services.Infrastructure.SalesMutations;

public sealed class SalesMutationOutboxOptions {
    public const string SectionName = "SalesMutationOutbox";

    public string AllowedInternalBaseUri { get; set; } = string.Empty;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxAuthenticationDeliveryAttempts { get; set; } = 3;
    public TimeSpan PendingUnhealthyAfter { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan DispatchCompletedRetention { get; set; } = TimeSpan.FromDays(14);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    public void Validate() {
        GetValidatedAllowedInternalBaseUri();
        RequirePositive(PollInterval, nameof(PollInterval));
        RequirePositive(LeaseDuration, nameof(LeaseDuration));
        RequirePositive(RequestTimeout, nameof(RequestTimeout));
        RequirePositive(InitialRetryDelay, nameof(InitialRetryDelay));
        RequirePositive(MaxRetryDelay, nameof(MaxRetryDelay));
        RequirePositive(PendingUnhealthyAfter, nameof(PendingUnhealthyAfter));
        RequirePositive(DispatchCompletedRetention, nameof(DispatchCompletedRetention));
        RequirePositive(CleanupInterval, nameof(CleanupInterval));

        if (MaxAuthenticationDeliveryAttempts <= 0)
            throw new InvalidOperationException(
                $"{nameof(MaxAuthenticationDeliveryAttempts)} must be positive.");

        if (MaxRetryDelay < InitialRetryDelay)
            throw new InvalidOperationException(
                $"{nameof(MaxRetryDelay)} must be greater than or equal to {nameof(InitialRetryDelay)}.");

        if (LeaseDuration <= RequestTimeout)
            throw new InvalidOperationException(
                $"{nameof(LeaseDuration)} must be greater than {nameof(RequestTimeout)}.");
    }

    public Uri GetValidatedAllowedInternalBaseUri() {
        string value = AllowedInternalBaseUri?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri allowedBaseUri) ||
            (!string.Equals(allowedBaseUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(allowedBaseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) ||
            string.IsNullOrWhiteSpace(allowedBaseUri.Host) ||
            !string.IsNullOrEmpty(allowedBaseUri.UserInfo) ||
            !string.IsNullOrEmpty(allowedBaseUri.Query) ||
            !string.IsNullOrEmpty(allowedBaseUri.Fragment) ||
            allowedBaseUri.AbsolutePath != "/")
            throw new InvalidOperationException(
                $"{nameof(AllowedInternalBaseUri)} must be an absolute HTTP(S) origin without a path, query, fragment, or user information.");

        return new Uri(allowedBaseUri.GetLeftPart(UriPartial.Authority), UriKind.Absolute);
    }

    private static void RequirePositive(TimeSpan value, string propertyName) {
        if (value <= TimeSpan.Zero)
            throw new InvalidOperationException($"{propertyName} must be positive.");
    }
}
