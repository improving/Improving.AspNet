using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Improving.AspNet.Tests
{
    using Domain;
    using MediatR.Cache;
    using MediatR.Rest.Get;
    using MediatR.Route;
    using MediatR.ServiceBus;

    [TestClass]
    public class ServiceBusRouterTests
    {
        [TestMethod]
        public void Should_Get_Path_For_Request_Type()
        {
            var path = ServiceBusRouter.GetRequestPath(typeof(GetStocks));
            Assert.AreEqual("improving/aspNet/tests/domain/getStocks", path);
        }

        [TestMethod]
        public void Should_Get_Path_For_Notification_Type()
        {
            var path = ServiceBusRouter.GetRequestPath(typeof(StockChanged));
            Assert.AreEqual("improving/aspNet/tests/domain/stockChanged", path);
        }

        [TestMethod]
        public void Should_Get_Path_For_Request_Instance()
        {
            var path = ServiceBusRouter.GetRequestPath(new GetStocks());
            Assert.AreEqual("improving/aspNet/tests/domain/getStocks", path);
        }

        [TestMethod]
        public void Should_Get_Path_For_Notification_Instance()
        {
            var path = ServiceBusRouter.GetRequestPath(new StockChanged());
            Assert.AreEqual("improving/aspNet/tests/domain/stockChanged", path);
        }

        [TestMethod]
        public void Should_Get_Path_For_Request_Decorator()
        {
            var path = ServiceBusRouter.GetRequestPath(typeof(Cached<StocksResult>));
            Assert.AreEqual("cached", path);
        }

        [TestMethod]
        public void Should_Get_Path_For_Decorated_Request()
        {
            var path = ServiceBusRouter.GetRequestPath(new GetStocks().Cached().RouteTo(""));
            Assert.AreEqual("improving/aspNet/tests/domain/getStocks/routed/cached", path);
        }

        [TestMethod]
        public void Should_Return_Nothing_If_Not_Request_Type()
        {
            var path = ServiceBusRouter.GetRequestPath(typeof(ServiceBusRouterTests));
            Assert.IsNull(path);
        }

        [TestMethod]
        public void Should_Return_Nothing_If_Not_Request_Instance()
        {
            var path = ServiceBusRouter.GetRequestPath(this);
            Assert.IsNull(path);
        }

        [TestMethod]
        public void Should_Return_Nothing_If_Generic_Request_Definition()
        {
            var path = ServiceBusRouter.GetRequestPath(typeof(GetRequest<,>));
            Assert.IsNull(path);
        }

        [TestMethod]
        public void Should_Return_Nothing_If_Generic_Request_Type()
        {
            var path = ServiceBusRouter.GetRequestPath(typeof(GetRequest<Stock,Stock>));
            Assert.IsNull(path);
        }
    }
}
