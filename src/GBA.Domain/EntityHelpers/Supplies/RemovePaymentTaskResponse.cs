namespace GBA.Domain.EntityHelpers.Supplies;

public sealed class RemovePaymentTaskResponse {
    public RemovePaymentTaskResponse(bool isSuccess, string errorMessage) {
        IsSuccess = isSuccess;

        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public string ErrorMessage { get; }
}