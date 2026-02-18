using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.EntityHelpers.Supplies;

public sealed class GenerateTaxFreePrintingDocumentResponse {
    public GenerateTaxFreePrintingDocumentResponse(string xlsxFilePath, string pdfFilePath, TaxFree taxFree) {
        XlsxFilePath = xlsxFilePath;

        PdfFilePath = pdfFilePath;

        TaxFree = taxFree;

        TaxFrees = new List<TaxFree>();
    }

    public GenerateTaxFreePrintingDocumentResponse(string xlsxFilePath, string pdfFilePath, List<TaxFree> taxFrees) {
        XlsxFilePath = xlsxFilePath;

        PdfFilePath = pdfFilePath;

        TaxFree = null;

        TaxFrees = taxFrees;
    }

    public string XlsxFilePath { get; }

    public string PdfFilePath { get; }

    public List<TaxFree> TaxFrees { get; }

    public TaxFree TaxFree { get; }
}