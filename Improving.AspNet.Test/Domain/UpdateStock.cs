namespace Improving.AspNet.Tests.Domain
{
    using MediatR;

    public class UpdateStock : Request.WithNoResponse
    {
        public Stock Stock { get; set; }
    }
}