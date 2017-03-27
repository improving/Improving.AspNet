using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Dependencies;
using Castle.MicroKernel;
using Castle.MicroKernel.Lifestyle;
using IDependencyResolver = System.Web.Http.Dependencies.IDependencyResolver;

namespace Improving.AspNet
{
    public class WindsorDependencyResolver 
        : WindsorDependencyScope, IDependencyResolver
    {
        public WindsorDependencyResolver(IKernel kernel)
            : base(kernel)
        {
        }

        public IDependencyScope BeginScope()
        {
            return new WindsorDependencyScope(Kernel);
        }
    }

    public class WindsorDependencyScope : IDependencyScope
    {
        private readonly IDisposable _scope;

        public WindsorDependencyScope(IKernel kernel)
        {
            Kernel = kernel;
            _scope  = kernel.BeginScope();
        }

        public IKernel Kernel { get; }

        public object GetService(Type t)
        {
            return Kernel.HasComponent(t) ? Kernel.Resolve(t) : null;
        }

        public IEnumerable<object> GetServices(Type t)
        {
            return Kernel.ResolveAll(t).Cast<object>().ToArray();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
