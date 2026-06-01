using System.Collections.Generic;
using System.Threading.Channels;

namespace GBA.Common.Search;

/// <summary>
/// In-process signal for near-real-time targeted search re-indexing. Stock-mutating code
/// requests a product's re-index; a background consumer debounces and re-indexes just those
/// products, instead of waiting for the periodic incremental sync. Polling stays the safety net.
/// </summary>
public interface ISearchReindexSignal {
    void Request(long productId);
    void Request(IEnumerable<long> productIds);
    ChannelReader<long> Reader { get; }
}

public sealed class SearchReindexSignal : ISearchReindexSignal {
    private readonly Channel<long> _channel =
        Channel.CreateUnbounded<long>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<long> Reader => _channel.Reader;

    public void Request(long productId) {
        if (productId > 0) _channel.Writer.TryWrite(productId);
    }

    public void Request(IEnumerable<long> productIds) {
        if (productIds == null) return;
        foreach (long id in productIds) Request(id);
    }
}
