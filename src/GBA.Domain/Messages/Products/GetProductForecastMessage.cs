using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetProductForecastMessage {
    public GetProductForecastMessage(Guid productNetId, DateTime asOfDate, int forecastWeeks, bool useCache = true) {
        ProductNetId = productNetId;
        AsOfDate = asOfDate;
        ForecastWeeks = forecastWeeks;
        UseCache = useCache;
    }

    public Guid ProductNetId { get; }
    public DateTime AsOfDate { get; }
    public int ForecastWeeks { get; }
    public bool UseCache { get; }
}