namespace Improving.AspNet
{
    using System.Threading.Tasks;
    using System.Web.Http;
    using global::MediatR;
    using MediatR;
    using MediatR.Rest;
    using MediatR.ServiceBus;

    public class ServiceBusController : ApiController
    {
        private readonly IMediator _mediator;

        public ServiceBusController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Processes the request through a local pipeline
        /// </summary>
        /// <param name="message">request to process</param>
        /// <returns>The response</returns>
        [HttpPost, ServiceBusExceptionFilter]
        public async Task<Message> Process(Message message)
        {
            var request  = message?.Payload;
            var response = await DynamicDispatch.Dispatch(_mediator, request, _ =>
            {
                throw new HttpResponseException(HttpStatusCodeExtensions.UnprocessableEntity);
            });
            return response is Unit ? new Message() : new Message(response);
        }

        /// <summary>
        /// Publishes the notification through a local pipeline
        /// </summary>
        /// <param name="message">notification to publish</param>
        /// <returns></returns>
        [HttpPost, ServiceBusExceptionFilter]
        public async Task<Message> Publish(Message message)
        {
            var notification = message?.Payload as IAsyncNotification;
            if (notification == null)
                throw new HttpResponseException(HttpStatusCodeExtensions.UnprocessableEntity);
            await _mediator.PublishAsync(notification);
            return new Message();
        }
    }
}
