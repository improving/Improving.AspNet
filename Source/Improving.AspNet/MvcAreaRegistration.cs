using System.Web.Mvc;
using Owin;

namespace Improving.AspNet
{
    public static class MvcAreaRegistration
    {
        public static IAppBuilder UseMvcAreas(this IAppBuilder builder)
        {
            AreaRegistration.RegisterAllAreas();
            return builder;
        }
    }
}