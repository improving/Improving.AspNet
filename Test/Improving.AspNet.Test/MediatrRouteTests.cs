namespace Improving.AspNet.Tests
{
    using System;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Castle.MicroKernel.Lifestyle;
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;
    using Castle.Windsor.Installer;
    using Domain;
    using global::MediatR;
    using MediatR;
    using MediatR.Rest;
    using MediatR.ServiceBus;
    using Microsoft.Owin.Hosting;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Owin;

    [TestClass]
    public class MediatrRouteTests
    {
        private IWindsorContainer _container;
        private IMediator _mediator;
        private HttpConfiguration _configuration;
        private const string BaseAddress = "http://localhost:9135/";

        [TestInitialize]
        public void TestInitialize()
        {
            _configuration = new HttpConfiguration();
            _container = new WindsorContainer()
                .Install(FromAssembly.This(),
                    new MediatRInstaller(Classes.FromThisAssembly()),
                    new WebApiInstaller(_configuration, Classes.FromThisAssembly())
                        .UseScopeAccessor<LifetimeScopeAccessor>()
                        .UseDefaultRoutesAndAttributeRoutes()
                        .TypeNameHandling()
                        .IgnoreNulls()
                );
            _mediator = _container.Resolve<IMediator>();
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            appBuilder.UseWebApi(_configuration);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _container.Dispose();
        }

        [TestMethod]
        public async Task ProcessIsConfigured()
        {
            await RouteShouldPass(new GetStocks(), "process");
        }

        [TestMethod]
        public async Task ProcessDoesNotAcceptWildCards()
        {
            await RouteShouldFail(new GetStocks(), "process/foo");
        }

        [TestMethod]
        public async Task TagRequiresClientAndDescription()
        {
            await RouteShouldPass(new GetStocks(), "tag/client1/firstthing");
        }

        [TestMethod]
        public async Task TagAloneFails()
        {
            await RouteShouldFail(new GetStocks(), "tag");
        }

        [TestMethod]
        public async Task TagAndClientFails()
        {
            await RouteShouldFail(new GetStocks(), "tag/client1");
        }

        [TestMethod]
        public async Task ProcessFullnameRouteIsConfigured()
        {
            await RouteShouldPass(new GetStocks(), "process/improving/aspNet/tests/domain/getStocks");
        }

        [TestMethod]
        public async Task ProcessFullnameRouteWithDecoratorsIsConfigured()
        {
            await RouteShouldPass(new GetStocks(), "process/improving/aspNet/tests/domain/getStocks/cached");
        }

        [TestMethod]
        public async Task PublishWithFullNameIsConfigured()
        {
            await RouteShouldPass(new StockChanged(), "publish/improving/aspNet/tests/domain/stockChanged");
        }

        [TestMethod]
        public async Task PublishIsSupported()
        {
            await RouteShouldPass(new StockChanged(), "publish");
        }

        [TestMethod]
        public async Task PublishIsNotWildcarded()
        {
            await RouteShouldFail(new StockChanged(), "publish/foo");
        }

        public async Task<Message> RouteShouldPass<TResp>(
            IAsyncRequest<TResp> request, string resourceUri)
        {
            using (WebApp.Start(BaseAddress, Configuration))
            {
                return await _mediator.PostAsync<Message, Message>(new Message(request), post =>
                {
                    post.BaseAddress = BaseAddress;
                    post.ResourceUri = resourceUri;
                    post.TypeNameHandling = true;
                });
            }
        }

        public async Task<Message> RouteShouldPass(
            IAsyncNotification notification, string resourceUri)
        {
            using (WebApp.Start(BaseAddress, Configuration))
            {
                return await _mediator.PostAsync<Message, Message>(new Message(notification), post =>
                {
                    post.BaseAddress = BaseAddress;
                    post.ResourceUri = resourceUri;
                    post.TypeNameHandling = true;
                });
            }
        }

        private async Task RouteShouldFail<TResp>(
            IAsyncRequest<TResp> request, string resourceUri)
        {
            using (WebApp.Start(BaseAddress, Configuration))
            {
                try
                {
                    await _mediator.PostAsync<Message, Message>(new Message(request), post =>
                    {
                        post.BaseAddress = BaseAddress;
                        post.ResourceUri = resourceUri;
                        post.TypeNameHandling = true;
                    });
                    throw new InternalTestFailureException("Should not reach here");
                }
                catch(Exception ex)
                {
                    Assert.IsNotNull(ex);
                }
            }
        }

        private async Task RouteShouldFail(IAsyncNotification notification, string resourceUri)
        {
            using (WebApp.Start(BaseAddress, Configuration))
            {
                try
                {
                    await _mediator.PostAsync<Message, Message>(new Message(notification), post =>
                    {
                        post.BaseAddress = BaseAddress;
                        post.ResourceUri = resourceUri;
                        post.TypeNameHandling = true;
                    });
                    throw new InternalTestFailureException("Should not reach here");
                }
                catch (Exception ex)
                {
                    Assert.IsNotNull(ex);
                }
            }
        }
    }
}
