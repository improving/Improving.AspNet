namespace Improving.AspNet
{
    using Castle.MicroKernel.Lifestyle;
    using Castle.MicroKernel.Lifestyle.Scoped;
    using Castle.MicroKernel.Registration;
    using Castle.MicroKernel.SubSystems.Configuration;
    using Castle.Windsor;
    using FluentValidation;
    using FluentValidation.Mvc;
    using System;
    using System.Web.Mvc;
    using System.Web.Routing;

    public class MvcInstaller : IWindsorInstaller
    {
        private readonly FromAssemblyDescriptor[] _fromAssemblies;
        private bool _useFluentValidation;
        private bool _useFeaturePaths;
        private Type _scopeAccessor;

        public MvcInstaller(params FromAssemblyDescriptor[] fromAssemblies)
        {
            _fromAssemblies = fromAssemblies;
        }

        public MvcInstaller UseFeaturePaths()
        {
            _useFeaturePaths = true;
            return this;
        }

        public MvcInstaller UseDefaultRoutes()
        {
            return UseRoutes(routes =>
            {
                routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
                routes.MapRoute(
                    name: "Default",
                    url: "{controller}/{action}/{id}",
                    defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
                );
            });
        }

        public MvcInstaller UseRoutes(Action<RouteCollection> routes)
        {
            if (routes != null)
                routes(RouteTable.Routes);
            return this;
        }

        public MvcInstaller UseFilters(Action<GlobalFilterCollection> filters)
        {
            if (filters != null)
                filters(GlobalFilters.Filters);
            return this;
        }

        public MvcInstaller UseFluentValidation()
        {
            _useFluentValidation = true;
            return this;
        }

        public MvcInstaller UseScopeAccessor<T>() where T : IScopeAccessor
        {
            _scopeAccessor = typeof(T);
            return this;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var childContainer = new WindsorContainer();

            childContainer.Register(
                Component.For<IControllerFactory>()
                         .ImplementedBy<WindsorControllerFactory>()
                         );

            foreach (var assemebly in _fromAssemblies)
            {
                childContainer.Register(
                    assemebly.BasedOn<IController>()
                        .WithServiceSelf()
                        .LifestyleScoped(_scopeAccessor ?? typeof(WebRequestScopeAccessor))
                    );
            }
              
            container.AddChildContainer(childContainer);

            ControllerBuilder.Current.SetControllerFactory(childContainer.Resolve<IControllerFactory>());

            if (_useFluentValidation)
            {
                FluentValidationModelValidatorProvider.Configure(provider =>
                {
                    provider.ValidatorFactory = container.Resolve<IValidatorFactory>();
                });
            }

            if (_useFeaturePaths)
            {
                ViewEngines.Engines.Clear();
                ViewEngines.Engines.Add(new FeatureViewLocationRazorViewEngine());
            }
        }
    }
}
