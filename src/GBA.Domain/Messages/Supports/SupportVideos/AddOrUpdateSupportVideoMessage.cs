using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Supports.SupportVideos;

public sealed class AddOrUpdateSupportVideoMessage {
    public AddOrUpdateSupportVideoMessage(SupportVideo supportVideo) {
        SupportVideo = supportVideo;
    }

    public SupportVideo SupportVideo { get; }
}