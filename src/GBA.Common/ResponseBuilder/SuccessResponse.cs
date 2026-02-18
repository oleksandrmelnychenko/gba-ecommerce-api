using System.Net;
using GBA.Common.ResponseBuilder.Contracts;

namespace GBA.Common.ResponseBuilder;

public class SuccessResponse : IWebResponse {
    public object Body { get; set; }

    public string Message { get; set; }

    public HttpStatusCode StatusCode { get; set; }
}