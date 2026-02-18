using System;

namespace GBA.Common.Exceptions.CustomExceptions;

public class ProductUploadParseException : Exception {
    public ProductUploadParseException() { }

    public ProductUploadParseException(string message) : base(message) { }

    public ProductUploadParseException(ProductUploadParseExceptionType type, int column, int row, string vendorCode) : base($"{type} Col: {column} Row: {row}") {
        Type = type;

        Column = column;

        Row = row;

        VendorCode = vendorCode;
    }

    public ProductUploadParseExceptionType Type { get; set; }

    public int Column { get; set; }

    public int Row { get; set; }

    public string VendorCode { get; set; }
}