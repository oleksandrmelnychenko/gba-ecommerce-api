namespace GBA.Domain.Messages.UserManagement;

public sealed class RefreshTokenMessage {
    public RefreshTokenMessage(string refreshToken) {
        RefreshToken = refreshToken;
    }

    public string RefreshToken { get; set; }
}