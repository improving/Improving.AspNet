using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using FluentValidation;
using MediatR;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Owin;
using Improving.MediatR;
using Improving.MediatR.Rest;
using Improving.MediatR.ServiceBus;
using Improving.AspNet.Tests.Domain;
using Ploeh.AutoFixture;

namespace Improving.AspNet.Tests
{
    [TestClass]
    public class ServiceBusControllerTests
    {
        private IWindsorContainer _container;
        private IMediator _mediator;
        private HttpConfiguration _configuration;

        private const string BaseAddress = "http://localhost:9134/";

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
        public void TestName()
        {
            var f = new Fixture();
            var req = f.Create<GetStocks>();
            var res = f.Create<StocksResult>();
        }

        [TestMethod]
        public async Task Should_Send_Remotely()
        {
            using (WebApp.Start(BaseAddress, Configuration))
            {
                var stock = await _mediator.At(BaseAddress)
                    .GetAsync<Stock>($"{BaseAddress}stock/APL");

                Assert.AreEqual("APL", stock.Symbol);
                Assert.AreEqual(300M, stock.Quote);
            }
        }

        [TestMethod]
        public async Task Should_Publish_Remotely()
        {
            using (WebApp.Start(BaseAddress, Configuration))
            {
                await _mediator.At(BaseAddress)
                    .PublishAsync(new StockChanged
                    {
                        Synbol = "APL",
                        Quote = 300M
                    });
            }
        }

        [TestMethod]
        public async Task Should_Reject_Unhandled_Requests()
        {
            using (WebApp.Start(BaseAddress, Configuration))
            {
                try
                {
                    await _mediator.At(BaseAddress)
                        .SendAsync(new UpdateStock
                        {
                            Stock = new Stock
                            {
                                Symbol = "APL",
                                Quote = 330M
                            }
                        });
                }
                catch (HttpRequestException ex)
                {
                    Assert.AreEqual("Response status code does not indicate success: 404 (Not Found).", ex.Message);
                }
            }
        }

        [TestMethod]
        public async Task Should_Propogate_ValidationException()
        {
            using (WebApp.Start(BaseAddress, Configuration))
            {
                try
                {
                    await _mediator.At(BaseAddress)
                        .SendAsync(new TradeStock
                        {
                            Stock = new Stock
                            {
                                Quote = 0M
                            },
                            NumberOfShares = 10
                        });
                }
                catch (ValidationException vex)
                {
                    var errors = vex.Errors.Select(e => e.ErrorMessage).ToArray();
                    Assert.AreEqual(2, errors.Length);
                    CollectionAssert.Contains(errors, "'Stock. Symbol' should not be empty.");
                    CollectionAssert.Contains(errors, "'Stock. Quote' must be greater than '0'.");
                }
            }
        }

        [TestMethod]
        public async Task Should_Handle_Remote_Exceptions()
        {
            try
            {
                using (WebApp.Start(BaseAddress, Configuration))
                {
                    await _mediator.At(BaseAddress)
                        .GetAsync<Stock>("http://localhost:9001/stock/APL");
                }
            }
            catch (Exception ex)
            {
                Assert.AreEqual("An error occurred while sending the request.",
                    ex.Message);
            }
        }
    }

    public class StockController : ApiController
    {
        [Authorize,
         HttpGet, Route("stock/{symbol}")]
        public Stock GetQuote(string symbol)
        {
            return new Stock { Symbol = symbol, Quote = 300M };
        }
    }

    public class AuthorizeAttribute : ActionFilterAttribute
    {
        public override Task OnActionExecutingAsync(
            HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            var mediator =
                (IMediator) actionContext
                    .ControllerContext.Configuration.DependencyResolver
                    .GetService(typeof(IMediator));
            return mediator.SendAsync(new AuthorizeStock());
        }
    }
}
