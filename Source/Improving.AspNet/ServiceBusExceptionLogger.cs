namespace Improving.AspNet
{
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.ExceptionHandling;
    using Castle.Core.Logging;
    using MediatR;

    /// <summary>
    /// Logs any exception that happened outside of the MediatR pipeline
    /// </summary>
    public class ServiceBusExceptionLogger : IExceptionLogger
    {
        private readonly ILogger _logger;

        public ServiceBusExceptionLogger(ILogger logger)
        {
            _logger = logger;
        }

        public async Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            if (!Equals(context.Exception?.Data[Stage.Logging], true))
                _logger.Error("Exception unhandled by Media", context.Exception);
        }
    }
}