namespace Improving.AspNet.Tests.Domain
{
    using System.Threading.Tasks;
    using global::MediatR;

    public class AuthorizeStockHandler
        : IAsyncRequestHandler<AuthorizeStock, Unit>
    {
        public Task<Unit> Handle(AuthorizeStock request)
        {
            return Task.FromResult(Unit.Value);
        }
    }
}