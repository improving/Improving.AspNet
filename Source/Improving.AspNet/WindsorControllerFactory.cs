namespace Improving.AspNet
{
    using System;
    using System.Diagnostics;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Castle.MicroKernel;

    public class WindsorControllerFactory : DefaultControllerFactory
    {
        private readonly IKernel _kernel;

        public WindsorControllerFactory(IKernel kernel)
        {
            _kernel = kernel;
        }

        [DebuggerStepThrough]
        protected override IController GetControllerInstance(
            RequestContext requestContext,
            Type controllerType)
        {
            if (controllerType == null)
            {
                throw new HttpException(404, string.Format(
                    "The controller for path '{0}' could not be found.",
                    requestContext.HttpContext.Request.Path));
            }
            return (IController)_kernel.Resolve(controllerType);
        }

        public override void ReleaseController(IController controller)
        {
            _kernel.ReleaseComponent(controller);
        }
    }
}