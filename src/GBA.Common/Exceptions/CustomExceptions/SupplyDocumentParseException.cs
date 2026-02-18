using System;

namespace GBA.Common.Exceptions.CustomExceptions;

public class SupplyDocumentParseException : Exception {
    public SupplyDocumentParseException() { }

    public SupplyDocumentParseException(string message) : base(message) { }

    public SupplyDocumentParseException(SupplyDocumentParseExceptionType type, int row, int column, string vendorCode) : base($"{type} Col: {column} Row: {row}") {
        Type = type;

        Column = column;

        Row = row;

        VendorCode = vendorCode;
    }

    public SupplyDocumentParseExceptionType Type { get; set; }

    public int Column { get; set; }

    public int Row { get; set; }

    public string VendorCode { get; set; }
}