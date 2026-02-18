namespace GBA.Domain.Messages.Supplies.GroupedPaymentTasks;

public sealed class GetAllGroupedPaymentTasksMessage {
    public GetAllGroupedPaymentTasksMessage(int limit) {
        Limit = limit;
    }

    public int Limit { get; set; }
}