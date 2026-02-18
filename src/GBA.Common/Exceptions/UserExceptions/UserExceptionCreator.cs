using System;
using GBA.Common.Exceptions.UserExceptions.Contracts;

namespace GBA.Common.Exceptions.UserExceptions;

public class UserExceptionCreator<TException> where TException : IUserException, new() {
    private IUserException _userException;

    private UserExceptionCreator() { }

    public string GetUserMessage => _userException.GetUserMessageException;

    public static UserExceptionCreator<TException> Create(string userMessage) {
        UserExceptionCreator<TException> instance = new();

        TException userException = new();
        userException.SetUserMessage(userMessage);

        instance._userException = userException;
        return instance;
    }

    public void Throw() {
        throw (Exception)_userException;
    }
}