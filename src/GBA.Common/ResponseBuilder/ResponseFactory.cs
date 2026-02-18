using GBA.Common.ResponseBuilder.Contracts;

namespace GBA.Common.ResponseBuilder;

public class ResponseFactory : IResponseFactory {
    public IWebResponse GetSuccessReponse() {
        return new SuccessResponse();
    }

    public IWebResponse GetErrorResponse() {
        return new ErrorResponse();
    }
}