namespace Improving.AspNet.Tests.Domain
{
    using MediatR;

    public class AuthorizeStock : Request.WithNoResponse
    {
        public Stock Stock { get; set; }
    }
}