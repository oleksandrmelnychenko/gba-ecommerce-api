using System;
using GBA.Common.Helpers.Products;

namespace GBA.Domain.Messages.Products;

public sealed class UploadComponentsFromFileMessage {
    public UploadComponentsFromFileMessage(
        string filePath,
        ComponentsUploadParseConfiguration configuration,
        Guid userNetId) {
        FilePath = filePath;
        Configuration = configuration;
        UserNetId = userNetId;
    }

    public string FilePath { get; }
    public ComponentsUploadParseConfiguration Configuration { get; }
    public Guid UserNetId { get; }
}