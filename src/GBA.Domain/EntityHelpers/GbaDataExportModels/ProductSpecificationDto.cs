namespace GBA.Domain.EntityHelpers.GbaDataExportModels;

public class ProductSpecificationDto {
    public string Name { get; set; }

    public string SpecificationCode { get; set; }

    public string Locale { get; set; }

    public decimal DutyPercent { get; set; }

    public bool IsActive { get; set; }

    public long AddedById { get; set; }

    public long ProductId { get; set; }

    public decimal CustomsValue { get; set; }

    public decimal Duty { get; set; }

    public decimal VATValue { get; set; }

    public decimal VATPercent { get; set; }
}