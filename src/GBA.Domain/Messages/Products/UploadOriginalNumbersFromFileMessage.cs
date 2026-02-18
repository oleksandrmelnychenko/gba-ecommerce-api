using System;
using GBA.Common.Helpers.Products;

namespace GBA.Domain.Messages.Products;

public sealed class UploadOriginalNumbersFromFileMessage {
    public UploadOriginalNumbersFromFileMessage(
        string filePath,
        OriginalNumbersUploadParseConfiguration configuration,
        Guid userNetId) {
        FilePath = filePath;
        Configuration = configuration;
        UserNetId = userNetId;
    }

    public string FilePath { get; }
    public OriginalNumbersUploadParseConfiguration Configuration { get; }
    public Guid UserNetId { get; }
}