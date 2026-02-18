namespace GBA.Common.ResponseBuilder.Contracts;

public interface IResponseFactory {
    IWebResponse GetSuccessReponse();

    IWebResponse GetErrorResponse();
}