namespace GBA.Domain.Messages.SchedulerTasks;

public sealed class UpdateUkrainianStorageProductsAvailabilityMessage {
    public UpdateUkrainianStorageProductsAvailabilityMessage(string tempFolderPath) {
        TempFolderPath = tempFolderPath;
    }

    public string TempFolderPath { get; }
}