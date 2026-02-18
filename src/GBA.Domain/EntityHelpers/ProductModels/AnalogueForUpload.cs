namespace GBA.Domain.EntityHelpers.ProductModels;

public sealed class AnalogueForUpload {
    public string VendorCode { get; set; }

    public string AnalogueVendorCode { get; set; }

    public int ProductColumn { get; set; }

    public int AnalogueColumn { get; set; }

    public int Row { get; set; }

    public override bool Equals(object? obj) {
        if (obj is AnalogueForUpload other)
            return VendorCode == other.VendorCode
                   && AnalogueVendorCode == other.AnalogueVendorCode;
        return false;
    }

    public override int GetHashCode() {
        return (VendorCode, AnalogueVendorCode).GetHashCode();
    }
}