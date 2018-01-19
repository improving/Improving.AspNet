namespace Improving.AspNet
{
    using System;
    using System.Net.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Dispatcher;

    public class WindsorHttpControllerActivator : IHttpControllerActivator
    {
        public IHttpController Create(
            HttpRequestMessage request,
            HttpControllerDescriptor controllerDescriptor,
            Type controllerType)
        {
            var scope = request.GetDependencyScope();
            return scope.GetService(controllerType) as IHttpController;
        }
    }
}