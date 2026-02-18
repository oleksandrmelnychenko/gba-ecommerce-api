namespace GBA.Common.Exceptions.CustomExceptions;

public enum SupplyDocumentParseExceptionType {
    IncorrectDataType,
    EmptyValue,
    InvalidFileFormat,
    NoWorksheets,
    NoProductByVendorCode
}