using System;
using GBA.Common.Helpers.Products;

namespace GBA.Domain.Messages.Products;

public sealed class UploadAnaloguesFromFileMessage {
    public UploadAnaloguesFromFileMessage(
        string filePath,
        AnaloguesUploadParseConfiguration analoguesUploadParseConfiguration,
        Guid userNetId) {
        FilePath = filePath;
        AnaloguesUploadParseConfiguration = analoguesUploadParseConfiguration;
        UserNetId = userNetId;
    }

    public string FilePath { get; }
    public AnaloguesUploadParseConfiguration AnaloguesUploadParseConfiguration { get; }
    public Guid UserNetId { get; }
}