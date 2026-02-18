using System;

namespace GBA.Common.Exceptions.CustomExceptions;

public class LocalizedException : Exception {
    public LocalizedException(string message) : base(message) { }

    public LocalizedException(string localizedMessageKey, string unlocalizedMessage) : base(localizedMessageKey + " " + unlocalizedMessage) {
        LocalizedMessageKey = localizedMessageKey;

        UnlocalizedMessage = unlocalizedMessage;
    }

    public LocalizedException(string localizedMessageKey, object[] values) : base(string.Format(localizedMessageKey, values)) {
        LocalizedMessageKeyWithParams = localizedMessageKey;

        ValuesForMessage = values;
    }

    public LocalizedException(string localizedMessageKey, object unlocalizedElementMessage) {
        LocalizedMessageKey = localizedMessageKey;

        UnlocalizeElementMessage = unlocalizedElementMessage;
    }

    public string LocalizedMessageKey { get; }

    public string UnlocalizedMessage { get; }

    public string LocalizedMessageKeyWithParams { get; }

    public object[] ValuesForMessage { get; }

    public object UnlocalizeElementMessage { get; }
}