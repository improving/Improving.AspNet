using System.Configuration;
using Castle.Components.DictionaryAdapter;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace Improving.AspNet
{
    public class TypedConfigurationInstaller : IWindsorInstaller
    {
        private readonly FromAssemblyDescriptor[] _fromAssemblies;

        public TypedConfigurationInstaller(params FromAssemblyDescriptor[] fromAssemblies)
        {
            _fromAssemblies = fromAssemblies;
        }

        /// <summary>
        /// Running this installer will regester all interfaces that end in "Config" with the windsor container
        /// and provide a proxy implementation over the AppSettings 
        /// </summary>
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var daf = new DictionaryAdapterFactory();

            foreach (var assembly in _fromAssemblies)
            {
                container.Register(
                    assembly
                        .Where(type => type.IsInterface && type.Name.EndsWith("Config"))
                        .Configure(
                            reg => reg.UsingFactoryMethod(
                                (k, m, c) => daf.GetAdapter(m.Implementation, ConfigurationManager.AppSettings)
                            )
                        ));
            }
        }
    }
}
