using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers;

public sealed class ProductSpecificationFileUploadResult {
    public ProductSpecificationFileUploadResult() {
        InvalidVendorCodes = new List<string>();
    }

    public long ParsedCount { get; set; }

    public long SuccessfullyUpdatedCount { get; set; }

    public long UpdateWasNotRequiredCount { get; set; }

    public List<string> InvalidVendorCodes { get; set; }
}