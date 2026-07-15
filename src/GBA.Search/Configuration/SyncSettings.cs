using Microsoft.Extensions.Options;

namespace GBA.Search.Configuration;

public sealed class SyncSettings {
    public const string SectionName = "SearchSync";

    public int IncrementalIntervalSeconds { get; set; } = 60;
    public int FullRebuildHour { get; set; } = 3;
    public int BatchSize { get; set; } = 1000;
    public bool Enabled { get; set; } = true;
    public bool UseAliasSwap { get; set; }
    public bool CleanupOldCollections { get; set; } = true;
    public int CollectionsToKeep { get; set; } = 2;

    /// <summary>Readiness fails once the durable sync watermark is older than this many seconds.</summary>
    public int LagWarningSeconds { get; set; } = 300;
}

/// <summary>Rejects search modes that cannot provide an atomic durable generation fence.</summary>
public sealed class SyncSettingsValidator : IValidateOptions<SyncSettings> {
    public const string AliasSwapUnsupportedMessage =
        "SearchSync:UseAliasSwap=true is unsupported because alias mutation cannot be "
        + "atomically fenced with generation promotion. Keep UseAliasSwap=false.";
    public const string InvalidLagLimitMessage =
        "SearchSync:LagWarningSeconds must be greater than zero.";

    public ValidateOptionsResult Validate(string? name, SyncSettings options) {
        if (options.UseAliasSwap) {
            return ValidateOptionsResult.Fail(AliasSwapUnsupportedMessage);
        }

        return options.LagWarningSeconds <= 0
            ? ValidateOptionsResult.Fail(InvalidLagLimitMessage)
            : ValidateOptionsResult.Success;
    }
}
