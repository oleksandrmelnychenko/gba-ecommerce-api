using System.Net;
using GBA.Common.ResponseBuilder;
using GBA.Services.Infrastructure.Checkout;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GBA.Ecommerce.Controllers.Clients;

public sealed class CartConflictFilterAttribute : ExceptionFilterAttribute {
    public override void OnException(ExceptionContext context) {
        if (context.Exception is CheckoutKeyReuseException reuse) {
            context.Result = new UnprocessableEntityObjectResult(new ErrorResponse {
                Body = null,
                Message = reuse.Message,
                StatusCode = HttpStatusCode.UnprocessableEntity
            });
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is not CheckoutConflictException conflict) return;

        context.Result = new ConflictObjectResult(new ErrorResponse {
            Body = null,
            Message = conflict.Message,
            StatusCode = HttpStatusCode.Conflict
        });
        context.ExceptionHandled = true;
    }
}
