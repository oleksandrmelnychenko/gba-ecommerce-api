namespace GBA.Domain.EntityHelpers.Supplies;

public sealed class ExportSadDocumentsResponse {
    public ExportSadDocumentsResponse(
        string facturaXlsx,
        string facturaPdf,
        string specXlsx,
        string specPdf,
        string oldFacturaXlsx,
        string oldFacturaPdf,
        string oldSpecXlsx,
        string oldSpecPdf) {
        FacturaXlsx = facturaXlsx;

        FacturaPdf = facturaPdf;

        SpecXlsx = specXlsx;

        SpecPdf = specPdf;

        OldFacturaXlsx = oldFacturaXlsx;

        OldFacturaPdf = oldFacturaPdf;

        OldSpecXlsx = oldSpecXlsx;

        OldSpecPdf = oldSpecPdf;
    }

    public string FacturaXlsx { get; }

    public string FacturaPdf { get; }

    public string SpecXlsx { get; }

    public string SpecPdf { get; }

    public string OldFacturaXlsx { get; }

    public string OldFacturaPdf { get; }

    public string OldSpecXlsx { get; }

    public string OldSpecPdf { get; }
}