namespace GBA.Common.Configuration;

public class SecuritySettings {
    public string JwtKey { get; set; } = "GBA_Concord_JWT_Secret_Key_2024_Production_X9k2mP";
    public string PriceEncryptionKey { get; set; } = "GBA_Pr1c3_K3y_32";
    public string PriceEncryptionIV { get; set; } = "GBA_Pr1c3_IV_16!";

    private static SecuritySettings _instance = new();

    public static SecuritySettings Instance => _instance;

    public static void Initialize(SecuritySettings settings) {
        _instance = settings ?? new SecuritySettings();
    }
}
