using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers;

public sealed class UploadProductSpecificationResult {
    public UploadProductSpecificationResult() {
        SuccessfullyUpdatedProducts = new List<string>();

        MissingProducts = new List<string>();

        UpdateNotRequiredProducts = new List<string>();
    }

    public List<string> SuccessfullyUpdatedProducts { get; }

    public List<string> MissingProducts { get; }

    public List<string> UpdateNotRequiredProducts { get; }
}