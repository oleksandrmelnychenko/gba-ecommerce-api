namespace GBA.Domain.EntityHelpers;

public sealed class GenerateSaleInvoiceDocumentResponse {
    public GenerateSaleInvoiceDocumentResponse(string documentURL, string errorMessage, bool isSuccess = false, string pdfDocumentURL = "") {
        IsSuccess = isSuccess;

        PdfDocumentURL = pdfDocumentURL;

        DocumentURL = documentURL;

        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; set; }

    public string PdfDocumentURL { get; }

    public string DocumentURL { get; set; }

    public string ErrorMessage { get; set; }
}