namespace GBA.Domain.Messages.Logging;

public sealed class AddNewErrorLogMessage {
    public AddNewErrorLogMessage(string url, string entity) {
        URL = url;

        Entity = entity;
    }

    public string URL { get; set; }

    public string Entity { get; set; }
}