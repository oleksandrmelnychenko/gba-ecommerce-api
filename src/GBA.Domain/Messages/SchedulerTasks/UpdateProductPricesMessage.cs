namespace GBA.Domain.Messages.SchedulerTasks;

public sealed class UpdateProductPricesMessage {
    public UpdateProductPricesMessage(string tempFolderPath) {
        TempFolderPath = tempFolderPath;
    }

    public string TempFolderPath { get; }
}