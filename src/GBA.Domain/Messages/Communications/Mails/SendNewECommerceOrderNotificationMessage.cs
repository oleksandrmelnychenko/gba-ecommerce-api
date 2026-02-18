namespace GBA.Domain.Messages.Communications.Mails;

public sealed class SendNewECommerceOrderNotificationMessage {
    public SendNewECommerceOrderNotificationMessage(
        string recipientName,
        string recipientMail,
        string subject,
        string message
    ) {
        RecipientName = recipientName;

        RecipientMail = recipientMail;

        Subject = subject;

        Message = message;
    }

    public string RecipientName { get; }

    public string RecipientMail { get; }

    public string Subject { get; }

    public string Message { get; }
}