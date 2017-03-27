namespace Improving.AspNet.Tests.Domain
{
    using System.Threading.Tasks;
    using global::MediatR;

    public class StockChangedHandler :
        IAsyncNotificationHandler<StockChanged>
    {
        public Task Handle(StockChanged notification)
        {
            return Task.CompletedTask;
        }
    }
}