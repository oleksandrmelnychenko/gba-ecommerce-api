using System.Text.RegularExpressions;
using GBA.Services.Services.UserManagement.Contracts;

namespace GBA.Services.Services.UserManagement;

public sealed class EmailValidationService : IEmailValidationService {
    public bool IsEmailValid(string email) {
        return Regex.IsMatch(
            email,
            @"^(([^<>()\[\]\\.,;:\s@""]+(\.[^<>()\[\]\\.,;:\s@""]+)*)|("".+""))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$"
        );
    }
}