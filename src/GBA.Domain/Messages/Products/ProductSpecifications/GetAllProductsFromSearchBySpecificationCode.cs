namespace GBA.Domain.Messages.Products;

public sealed class GetAllProductsFromSearchBySpecificationCode {
    public GetAllProductsFromSearchBySpecificationCode(string code) {
        Code = code;
    }

    public string Code { get; }
}