namespace Improving.AspNet.Tests.Domain.AnotherNamespace
{
    using System.Collections.Generic;
    using MediatR;

    public class GetStocks : Request.WithResponse<StocksResult>
    {
    }

    public class StocksResult
    {
        public ICollection<StockData> Stocks { get; set; }
    }

    public class StockData
    {
        public Stock Stock { get; set; }
    }
}
