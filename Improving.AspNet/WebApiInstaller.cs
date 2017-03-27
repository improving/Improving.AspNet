namespace Improving.AspNet
{
    using Castle.MicroKernel.Lifestyle;
    using Castle.MicroKernel.Lifestyle.Scoped;
    using Castle.MicroKernel.Registration;
    using Castle.MicroKernel.SubSystems.Configuration;
    using Castle.Windsor;
    using FluentValidation;
    using FluentValidation.WebApi;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.Linq;
    using System.Runtime.Serialization.Formatters;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Cors;
    using System.Web.Http.Filters;
    using MediatR;
    using MediatR.ServiceBus;
    using Swashbuckle.Application;
    using System.IO;
    using System.Web.Http.ExceptionHandling;
    using Castle.Core.Logging;

    public class WebApiInstaller : IWindsorInstaller
    {
        private readonly HttpConfiguration _configuration;
        private readonly FromAssemblyDescriptor[] _fromAssemblies;
        private bool _useFluentValidation;
        private Type _scopeAccessor;
        private bool _useGlobalExceptionLogging;

        public WebApiInstaller(params FromAssemblyDescriptor[] fromAssemblies)
            : this(GlobalConfiguration.Configuration, fromAssemblies)
        {
        }

        public WebApiInstaller(HttpConfiguration configuration,
                               params FromAssemblyDescriptor[] fromAssemblies)
        {
            _configuration  = configuration;
            _fromAssemblies = fromAssemblies;
            configuration.Formatters.Insert(0, new CamelCasingJsonFormatter());

            DiscoverMediatrRoutes();
        }

        public WebApiInstaller UseFluentValidation()
        {
            _useFluentValidation = true;
            return this;
        }

        public WebApiInstaller UseScopeAccessor<T>() where T : IScopeAccessor
        {
            _scopeAccessor = typeof(T);
            return this;
        }

        public WebApiInstaller UseDefaultRoutesAndAttributeRoutes()
        {
            _configuration.MapHttpAttributeRoutes();
            _configuration.Routes.MapHttpRoute(
                    name:          "DefaultApi",
                    routeTemplate: "api/{controller}/{id}",
                    defaults:      new { id = RouteParameter.Optional }
                );
            return this;
        }

        public WebApiInstaller UseJsonAsTheDefault()
        {
            _configuration.Formatters.Remove(_configuration.Formatters.XmlFormatter);
            return this;
        }

        public WebApiInstaller IgnoreCircularReferencesInJson()
        {
            _configuration.Formatters.JsonFormatter.SerializerSettings
                .ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            return this;
        }

        public WebApiInstaller IgnoreNulls()
        {
            _configuration.Formatters.JsonFormatter
                .SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            return this;
        }

        public WebApiInstaller UseCamelCaseJsonPropertyNames()
        {
            _configuration.Formatters.JsonFormatter
                .SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            return this;
        }

        public WebApiInstaller TypeNameHandling(TypeNameHandling handling = Newtonsoft.Json.TypeNameHandling.Auto)
        {
            var jsonFormatter = _configuration.Formatters.JsonFormatter;
            jsonFormatter.SerializerSettings.TypeNameHandling = handling;
            jsonFormatter.SerializerSettings.TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple;
            return this;
        }

        public WebApiInstaller UseFilters(Action<HttpFilterCollection> filters)
        {
            filters?.Invoke(_configuration.Filters);
            return this;
        }

        public WebApiInstaller EnableCors(ICorsPolicyProvider policy = null)
        {
            if (policy != null)
                _configuration.EnableCors(policy);
            else
                _configuration.EnableCors();
            return this;
        }

        public WebApiInstaller UseGlobalExceptionLogging()
        {
            _useGlobalExceptionLogging = true;
            return this;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var childContainer = new WindsorContainer();
            container.AddChildContainer(childContainer);

            var accessor = _scopeAccessor ?? typeof(WebRequestScopeAccessor);

            childContainer.Register(
                Component.For<ServiceBusController>()
                    .LifestyleScoped(accessor)
                );

            foreach (var assembly in _fromAssemblies)
            {
                childContainer.Register(
                    assembly.BasedOn<IHttpController>().WithServiceSelf()
                       .LifestyleScoped(accessor)
                    );
            }

            if (_useFluentValidation)
            {
                FluentValidationModelValidatorProvider
                    .Configure(_configuration, provider =>
                         provider.ValidatorFactory = container.Resolve<IValidatorFactory>());
            }

            _configuration.DependencyResolver =
               new WindsorDependencyResolver(childContainer.Kernel);

            ConfigureSwagger();

            if (_useGlobalExceptionLogging)
            {
                var logger = container.Resolve<ILogger>();
                _configuration.Services.Add(typeof(IExceptionLogger), new ServiceBusExceptionLogger(logger));
            }

            _configuration.EnsureInitialized();
        }

        private void DiscoverMediatrRoutes()
        {
            _configuration.Routes.MapHttpRoute(
                "process",
                "process",
                new {controller = "ServiceBus", action = "Process"});

            _configuration.Routes.MapHttpRoute(
                "publish",
                "publish",
                new {controller = "ServiceBus", action = "Publish"});

            _configuration.Routes.MapHttpRoute(
                "tag",
                "tag/{client}/{args*}",
                new {Controller = "ServiceBus", Action = "Process"});

            MediatRInstaller.RequestsSupported += requestMetadatas =>
            {
                foreach (var metadata in requestMetadatas)
                {
                    var request     = metadata.RequestType;
                    var requestPath = ServiceBusRouter.GetRequestPath(request);
                    var template    = $"process/{requestPath}";
                    AddServiceBusRoute("Process", template);
                    AddServiceBusRoute("Process", $"{template}/{{args*}}");
                }
            };

            MediatRInstaller.NotificationsSupported += requestMetadatas =>
            {
                foreach (var metadata in requestMetadatas)
                {
                    var request     = metadata.RequestType;
                    var requestPath = ServiceBusRouter.GetRequestPath(request);
                    var template    = $"publish/{requestPath}";
                    AddServiceBusRoute("Publish", template);
                }
            };
        }

        private void AddServiceBusRoute(string action, string template)
        {
            if (!_configuration.Routes.Any(r => r.RouteTemplate == template))
                _configuration.Routes.MapHttpRoute(template, template,
                    new { Controller = "ServiceBus", Action = action });
        }

        private void ConfigureSwagger()
        {
            _configuration
                .EnableSwagger(x =>
                {
                    x.SingleApiVersion("MediatR", "Api");
                    x.SchemaId(SwaggerMediatRFilter.ModelToSchemaId);
                    x.IgnoreObsoleteProperties();
                    x.DescribeAllEnumsAsStrings();
                    x.DocumentFilter<SwaggerMediatRFilter>();
                    IncludeApiComments(x);
                })
                .EnableSwaggerUi(c =>
                {
                    c.InjectStylesheet(typeof(WebApiInstaller).Assembly,"Improving.AspNet.SwaggerSFTP.css");
                });
        }

        private static void IncludeApiComments(SwaggerDocsConfig config)
        {
            var files =
                (AppDomain.CurrentDomain.SetupInformation.PrivateBinPath ??
                 AppDomain.CurrentDomain.BaseDirectory).Split(';')
                .SelectMany(path => Directory.GetFiles(path, "*.xml"));

            foreach (var file in files)
            {
                var filename = new FileInfo(file).Name;
                if (filename.IndexOf("Api", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    filename.IndexOf("Improving.AspNet", StringComparison.OrdinalIgnoreCase) >= 0)
                    config.IncludeXmlComments(file);
            }
        }
    }
}
