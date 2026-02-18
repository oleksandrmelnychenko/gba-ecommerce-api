namespace GBA.Domain.Messages.SchedulerTasks;

public sealed class UpdatePolishStorageProductsAvailabilityMessage {
    public UpdatePolishStorageProductsAvailabilityMessage(string tempFolderPath) {
        TempFolderPath = tempFolderPath;
    }

    public string TempFolderPath { get; }
}