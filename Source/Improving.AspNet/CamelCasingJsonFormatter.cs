namespace Improving.AspNet
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;
    using System.Web.Http.Controllers;
    using System.Web.Http.Filters;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class CamelCasingAttribute : ActionFilterAttribute
    {
        public CamelCasingAttribute() : this(true)
        {    
        }

        public CamelCasingAttribute(bool useCamelCase)
        {
            UseCamelCase = useCamelCase;
        }

        public bool UseCamelCase { get; private set; }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);

            if (!actionContext.Request.Properties.ContainsKey("CamelCasing"))
                actionContext.Request.Properties.Add("CamelCasing", this);
        }
    }

    public class CamelCasingJsonFormatter : JsonMediaTypeFormatter
    {
        public override MediaTypeFormatter GetPerRequestFormatterInstance(
            Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
        {
            object property;
            if (request.Properties.TryGetValue("CamelCasing", out property))
            {
                var camelCasing = property as CamelCasingAttribute;
                if (camelCasing != null)
                {
                    var formatter = new JsonMediaTypeFormatter
                    {
                        SerializerSettings = new JsonSerializerSettings
                        {
                            NullValueHandling          = SerializerSettings.NullValueHandling,
                            Formatting                 = SerializerSettings.Formatting,
                            ReferenceLoopHandling      = SerializerSettings.ReferenceLoopHandling,
                            Converters                 = SerializerSettings.Converters,
                            ReferenceResolverProvider  = SerializerSettings.ReferenceResolverProvider,
                            PreserveReferencesHandling = SerializerSettings.PreserveReferencesHandling,
                            MaxDepth                   = SerializerSettings.MaxDepth,
                            TypeNameHandling           = SerializerSettings.TypeNameHandling,
                            TypeNameAssemblyFormat     = SerializerSettings.TypeNameAssemblyFormat
                        }
                    };
                    if (camelCasing.UseCamelCase)
                        formatter.SerializerSettings.ContractResolver =
                           new CamelCasePropertyNamesContractResolver();
                    return formatter;
                }
            }
            return base.GetPerRequestFormatterInstance(type, request, mediaType);
        }
    }
}
