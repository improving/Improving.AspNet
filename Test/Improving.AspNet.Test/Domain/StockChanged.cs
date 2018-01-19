namespace Improving.AspNet.Tests.Domain
{
    using global::MediatR;

    public class StockChanged : IAsyncNotification
    {
        public string  Synbol { get; set; }
        public decimal Quote  { get; set; }
    }
}