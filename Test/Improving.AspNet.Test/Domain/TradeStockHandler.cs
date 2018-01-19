namespace Improving.AspNet.Tests.Domain
{
    using System.Threading.Tasks;
    using global::MediatR;

    public class TradeStockHandler
        : IAsyncRequestHandler<TradeStock, Unit>
    {
        public Task<Unit> Handle(TradeStock request)
        {
            return Task.FromResult(Unit.Value);
        }
    }
}