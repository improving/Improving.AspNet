using System.Collections.Generic;

namespace Improving.AspNet.Tests.Domain
{
    using MediatR;

    /// <summary>
    /// Retrieves all stocks
    /// </summary>
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
