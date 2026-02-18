using System;
using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Supplies.Ukraine.Sads;

public sealed class UploadProductSpecificationForSadMessage {
    public UploadProductSpecificationForSadMessage(
        string pathToFile,
        ProductSpecificationParseConfiguration parseConfiguration,
        Guid sadNetId,
        Guid userNetId) {
        PathToFile = pathToFile;
        ParseConfiguration = parseConfiguration;
        SadNetId = sadNetId;
        UserNetId = userNetId;
    }

    public string PathToFile { get; }
    public ProductSpecificationParseConfiguration ParseConfiguration { get; }
    public Guid SadNetId { get; }
    public Guid UserNetId { get; }
}