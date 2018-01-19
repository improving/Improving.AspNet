namespace Improving.AspNet.Tests.Domain
{
    using MediatR;

    public class TradeStock : Request.WithNoResponse
    {
        public Stock Stock { get; set; }

        public int NumberOfShares { get; set; }
    }
}