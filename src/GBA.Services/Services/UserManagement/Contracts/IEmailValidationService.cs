namespace GBA.Services.Services.UserManagement.Contracts;

public interface IEmailValidationService {
    bool IsEmailValid(string email);
}