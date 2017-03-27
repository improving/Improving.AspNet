namespace Improving.AspNet
{
    using System;
    using System.Data.Entity.Core;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;
    using System.Web.Http.Filters;
    using FluentValidation;
    using MediatR.Rest;
    using MediatR.ServiceBus;

    public class ServiceBusExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            var validationException = context.Exception as ValidationException;

            if (validationException != null)
            {
                context.Response = new HttpResponseMessage(
                    HttpStatusCodeExtensions.UnprocessableEntity)
                {
                    RequestMessage = context.Request,
                    ReasonPhrase   = "Validation",
                    Content        = AsContent(validationException.Errors
                        .Select(e => new ValidationFailureShim(e))
                        .ToArray())
                };
                return;
            }

            var concurrencyException = context.Exception as OptimisticConcurrencyException;

            if (concurrencyException != null)
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent(concurrencyException.Message)
                };
                return;
            }

            var httpError = new HttpError(context.Exception, true);

            context.Response = context.Exception is InvalidOperationException
                ? context.Request.CreateErrorResponse(HttpStatusCode.NotImplemented, httpError)
                : context.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, httpError);

            context.Response.ReasonPhrase = "HttpError";
        }

        private static ObjectContent AsContent<T>(T content)
        {
            return new ObjectContent(typeof(T), content, RestFormatters.Json);
        }
    }
}
