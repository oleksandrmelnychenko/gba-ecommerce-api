using System;

namespace GBA.Domain.Messages.Products.ProductSpecifications;

public sealed class UpdateSadProductSpecificationAssignmentsMessage {
    public UpdateSadProductSpecificationAssignmentsMessage(Guid sadNetId) {
        SadNetId = sadNetId;
    }

    public Guid SadNetId { get; }
}