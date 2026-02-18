using System;

namespace GBA.Domain.Messages.Products;

public sealed class AddNewSpecificationCodesFromFileMessage {
    public AddNewSpecificationCodesFromFileMessage(string pathToFile, Guid updatedByNetId) {
        PathToFile = pathToFile;

        UpdatedByNetId = updatedByNetId;
    }

    public string PathToFile { get; }

    public Guid UpdatedByNetId { get; }
}