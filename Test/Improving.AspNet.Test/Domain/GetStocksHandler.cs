namespace Improving.AspNet.Tests.Domain
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::MediatR;

    public class GetStocksHandler
        : IAsyncRequestHandler<GetStocks, StocksResult>
    {
        public Task<StocksResult> Handle(GetStocks request)
        {
            return Task.FromResult(new StocksResult
            {
                Stocks = new List<StockData>
                {
                    new StockData { Stock = new Stock { Quote = 33.33M, Symbol = "brwn" }},
                    new StockData { Stock = new Stock { Quote = 55.44M, Symbol = "pcar" }}
                }
            });
        }
    }
}