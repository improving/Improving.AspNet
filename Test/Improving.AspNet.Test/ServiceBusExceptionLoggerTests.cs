namespace Improving.AspNet.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Castle.Core.Logging;
    using Castle.MicroKernel.Lifestyle;
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;
    using Castle.Windsor.Installer;
    using global::MediatR;
    using MediatR;
    using MediatR.Rest;
    using MediatR.ServiceBus;
    using Microsoft.Owin.Hosting;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Owin;
    using Rhino.Mocks;

    [TestClass]
    public class ServiceBusExceptionLoggerTests
    {
        private const string BaseAddress = "http://localhost:9134/";

        [TestMethod]
        public async Task Should_Log_Exceptions()
        {
            var _configuration = new HttpConfiguration();
            var messages = new List<string>();
            var logger = MockRepository.GenerateMock<ILogger>();
            logger
                .Expect(x => x.Error(Arg<string>.Is.TypeOf, Arg<Exception>.Is.TypeOf))
                .WhenCalled(m =>
                                {
                                    messages.Add((string) m.Arguments[0]);
                                });

            var _container = new WindsorContainer();
            _container.Register(
                Component.For<ILogger>().Instance(logger)
            );
            _container.Install(FromAssembly.This(),
                new MediatRInstaller(Classes.FromThisAssembly()),
                new WebApiInstaller(_configuration, Classes.FromThisAssembly())
                    .UseScopeAccessor<LifetimeScopeAccessor>()
                    .UseDefaultRoutesAndAttributeRoutes()
                    .TypeNameHandling()
                    .IgnoreNulls()
                    .UseGlobalExceptionLogging()
            );

            var mediator = _container.Resolve<IMediator>();

            using (WebApp.Start(BaseAddress, builder => builder.UseWebApi(_configuration)))
            {
                try
                {
                    await mediator.At(BaseAddress)
                        .GetAsync<string>($"{BaseAddress}throwException");
                }
                catch (Exception)
                {
                    Assert.AreEqual(1, messages.Count);
                }
            }

            _container.Dispose();
        }
    }

    public class ExceptionController : ApiController
    {
        [HttpGet, Route("throwException")]
        public IHttpActionResult ThrowException()
        {
            throw new Exception("intentional exception");
        }
    }
}