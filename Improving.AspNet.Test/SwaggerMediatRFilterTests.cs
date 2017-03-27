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
    using Microsoft.Owin.Hosting;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Owin;

    [TestClass]
    public class SwaggerMediatRFilterTests
    {
        private IWindsorContainer _container;
        private IMediator _mediator;
        private HttpConfiguration _configuration;
        private readonly string DocsUri;
        private readonly string SwaggerUri;
        private const string BaseAddress = "http://localhost:9133/";

        public SwaggerMediatRFilterTests()
        {
            DocsUri       = $"{BaseAddress}swagger/docs/MediatR";
            SwaggerUri    = $"{BaseAddress}swagger";
        }

        [TestInitialize]
        public void TestInitialize()
        {
            MediatRInstaller.Reset();
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

        /// <summary>
        /// This test can be used to serve the website in
        /// the test environment.  It will servie it for
        /// 10 minutes and then end.  It is only meant for dev.
        /// </summary>
        /// <returns></returns>
        [TestMethod, Ignore]
        public async Task ServeWebsite()
        {
            using (WebApp.Start(BaseAddress, Configuration))
            {
                await Task.Delay(TimeSpan.FromMinutes(10));
            }
        }

        [TestMethod]
        public async Task SwaggerUIIsConfigured()
        {
            await SwaggerResultShouldContain(SwaggerUri, "Swagger UI");
        }

        [TestMethod]
        public async Task HasTheAuthorizeStockRoute()
        {
            await SwaggerResultShouldContain(DocsUri, "process/improving/aspNet/tests/domain/authorizeStock");
        }

        [TestMethod]
        public async Task LoadsRequestObjects()
        {
            await TypeDefinitionIsProvided(typeof(GetStocks));
        }

        [TestMethod]
        public async Task LoadsResponseObjects()
        {
            await TypeDefinitionIsProvided(typeof(StocksResult));
        }

        [TestMethod]
        public async Task LoadsClassesWithSameNamesFromDifferentNamespaces()
        {
            await TypeDefinitionIsProvided(typeof(GetStocks));
            await TypeDefinitionIsProvided(typeof(Domain.AnotherNamespace.GetStocks));
        }

        [TestMethod]
        public async Task CreatesTypePropertyForStocksResult()
        {
            await CreatesTypeProperty(typeof(StocksResult));
        }

        private static void LogIfJson(string data)
        {
            if (!IsJson(data)) return;

            var pretty = JToken.Parse(data).ToString(Formatting.Indented);
            Console.WriteLine(pretty);
        }

        private static bool IsJson(string input){
            input = input.Trim();
            return input.StartsWith("{") &&
                   input.EndsWith("}") ||
                   input.StartsWith("[") &&
                   input.EndsWith("]");
        }

        #region Assertions

        public async Task SwaggerResultShouldContain(string uri, string expectedContent)
        {
            Console.WriteLine($"Should contain: [{expectedContent}]{Environment.NewLine}");

            using (WebApp.Start(BaseAddress, Configuration))
            {
                var result = await _mediator.GetAsync<string>(uri);

                LogIfJson(result);

                Assert.IsTrue(result.Contains(expectedContent));
            }
        }

        private async Task TypeDefinitionIsProvided(Type type)
        {
            var requestName  = type.FullName;
            await SwaggerResultShouldContain(DocsUri, $"\"{requestName}\"");
        }

        private async Task CreatesTypeProperty(Type type)
        {
            await SwaggerResultShouldContain(DocsUri, $"\"{type.FullName}, {type.Assembly.GetName().Name}\"");
        }

        #endregion
    }
}
