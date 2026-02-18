namespace GBA.Common.Exceptions.UserExceptions.Contracts;

public interface IUserException {
    string GetUserMessageException { get; }

    void SetUserMessage(string message);
}