namespace Improving.AspNet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization.Formatters;
    using System.Web.Http.Description;
    using global::MediatR;
    using MediatR;
    using MediatR.Inspect;
    using MediatR.ServiceBus;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Swashbuckle.Swagger;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Kernel;

    public class SwaggerMediatRFilter : IDocumentFilter
    {
        private readonly Fixture _examples;

        private static readonly MethodInfo CreateExampleMethod =
            typeof(SwaggerMediatRFilter).GetMethod("CreateExample",
         BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly JsonSerializerSettings SerializerSettings 
            = new JsonSerializerSettings
        {
            ContractResolver       = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling       = TypeNameHandling.Auto,
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
        };

        private static readonly string[] JsonFormats = {"application/json", "text/json"};

        public SwaggerMediatRFilter()
        {
            _examples = CreateExamplesGenerator();
        }

        public static string ModelToSchemaId(Type type)
        {
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Message<>))
            {
                var message = type.GetGenericArguments()[0];
                return $"{typeof(Message).FullName}<{message.FullName}>";
            }
            return type.FullName;
        }

        public void Apply(SwaggerDocument document, SchemaRegistry registry, IApiExplorer apiExplorer)
        {
            //remove the paths added from default routes
            var removeThese = document.paths.Where(p =>
                p.Key != "/process"  &&
                p.Key != "/publish"  &&
                p.Value.post != null &&
                p.Value.post.tags.Any(t => t == "ServiceBus"))
                .ToList();

            foreach (var valuePair in removeThese)
                document.paths.Remove(valuePair);

            AddPaths(document, registry, "process", MediatRInstaller.SupportedRequests);
            AddPaths(document, registry, "publish", MediatRInstaller.SupportedNotifications);

            document.paths = document.paths.OrderBy(e => e.Key)
                .ToDictionary(e => e.Key, e => e.Value);

            document.definitions[typeof(Message).FullName].example = new Message();
            document.definitions[typeof(ValidationFailureShim).FullName].example
                = new ValidationFailureShim
                {
                    PropertyName   = "FailingProperty",
                    AttemptedValue = "",
                    ErrorMessage   = "FailingProperty cannot be empty",
                    ErrorCode      = "1" 
                };
        }

        private void AddPaths(SwaggerDocument document, SchemaRegistry registry,
            string resource, IEnumerable<RequestMetadata> metadatas)
        {
            foreach (var path in BuildPaths(resource, registry, metadatas))
            {
                if(!document.paths.ContainsKey(path.Key))
                    document.paths.Add(path.Key, path.Value);
            }
        }

        private IEnumerable<KeyValuePair<string, PathItem>> BuildPaths(
            string resource, SchemaRegistry registry, IEnumerable<RequestMetadata> requests)
        {
            var stringSchema        = registry.GetOrRegister(typeof(string));
            var unprocessableSchema = registry.GetOrRegister(typeof(ValidationFailureShim));

            return requests.Select(x =>
            {
                var assembly        = x.RequestType.Assembly.GetName();
                var tag             = $"{assembly.Name} [{assembly.Version}]";
                var requestSchema   = GetMessageSchema(registry, x.RequestType);
                var responseSchema  = GetMessageSchema(registry, x.ResponseType);
                var requestPath     = ServiceBusRouter.GetRequestPath(x.RequestType);

                var requestSummary  = GetReferencedSchema(registry,
                    registry.GetOrRegister(x.RequestType))?.description;

                var handlerAssembly = x.HandlerType.Assembly.GetName();
                var handlerNotes    = $"Handled by {x.HandlerType.FullName} in {handlerAssembly.Name} [{handlerAssembly.Version}]";

                return new KeyValuePair<string, PathItem>($"/{resource}/{requestPath}", new PathItem
                {
                    post = new Operation
                    {
                        summary     = requestSummary,
                        operationId = x.RequestType.FullName,
                        description = handlerNotes,
                        tags        = new [] { tag },
                        consumes    = JsonFormats,
                        produces    = JsonFormats,
                        parameters  = new []
                        {
                            new Parameter
                            {
                                @in         = "body",
                                name        = "message",
                                description = "request to process",
                                schema      = requestSchema,
                                required    = true
                            }
                        },
                        responses = new Dictionary<string, Response>
                        {
                            {
                                "200", new Response {
                                    description = "OK",
                                    schema      = responseSchema
                                }
                            },
                            {
                                "422", new Response {
                                    description = "Unprocessable",
                                    schema      = unprocessableSchema
                                }
                            },
                            {
                                "409", new Response {
                                    description = "Concurrency Conflict",
                                    schema      = stringSchema
            }
                            }
                        }
                    }
                });
            });
        }

        private static Schema GetReferencedSchema(SchemaRegistry registry, Schema reference)
        {
            var parts = reference.@ref.Split('/');
            var name = parts.Last();
            return registry.Definitions[name];
        }

        private Schema GetMessageSchema(SchemaRegistry registry, Type message)
        {
            if (message == null || typeof(Unit).IsAssignableFrom(message))
                return registry.GetOrRegister(typeof(Message));
            var schema     = registry.GetOrRegister(typeof(Message<>).MakeGenericType(message));
            var definition = GetReferencedSchema(registry, schema);
            definition.example = CreateExampleMessage(message);
            return schema;
        }

        private object CreateExampleMessage(Type message)
        {
            try
            {
                var creator    = CreateExampleMethod.MakeGenericMethod(message);
                var example    = creator.Invoke(null, new [] {_examples});
                var jsonString = JsonConvert.SerializeObject(example, SerializerSettings);
                return JsonConvert.DeserializeObject(jsonString);
            }
            catch
            {
                return null;
            }
        }

        private static Message<T> CreateExample<T>(ISpecimenBuilder builder)
        {
            return new Message<T> { Payload = builder.Create<T>() };
        }

        private static Fixture CreateExamplesGenerator()
        {
            var generator = new Fixture { RepeatCount = 1 };
            var customization = new SupportMutableValueTypesCustomization();
            customization.Customize(generator);
            return generator;
        }
    }

    public class Message<T>
    {
        [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
        public T Payload { get; set; }
    }
}
