using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Simple.Data
{
    using System.Composition.Hosting;
    using System.Composition.Hosting.Core;
    using System.Diagnostics;
    using System.Runtime.Loader;

    class MefHelper : Composer
    {
        public override T Compose<T>()
        {
            using (var container = CreateAppDomainContainer())
            {
                var exports = container.GetExports<T>().ToList();
                if (exports.Count == 1)
                {
                    return exports.Single();
                }
            }
            using (var container = CreateFolderContainer())
            {
                var exports = container.GetExports<T>().ToList();
                if (exports.Count == 0) throw new SimpleDataException("No ADO Provider found.");
                if (exports.Count > 1) throw new SimpleDataException("Multiple ADO Providers found; specify provider name or remove unwanted assemblies.");
                return exports.Single();
            }
        }

        public override T Compose<T>(string contractName)
        {
            try
            {
                using (var container = CreateAppDomainContainer())
                {
                    var exports = container.GetExports<T>(contractName).ToList();
                    if (exports.Count == 1)
                    {
                        return exports.Single();
                    }
                }
                using (var container = CreateFolderContainer())
                {
                    var exports = container.GetExports<T>(contractName).ToList();
                    if (exports.Count == 0) throw new SimpleDataException(string.Format("No {0} Provider found.", contractName));
                    if (exports.Count > 1) throw new SimpleDataException("Multiple ADO Providers found; specify provider name or remove unwanted assemblies.");
                    return exports.Single();
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                SimpleDataTraceSources.TraceSource.TraceEvent(TraceEventType.Error, SimpleDataTraceSources.GenericErrorMessageId,
                    "Compose failed: {0}", ex.Message);
                throw;
            }
        }

        public static T GetAdjacentComponent<T>(Type knownSiblingType) where T : class
        {
            var assembly = knownSiblingType.Assembly;

            var configuration = new ContainerConfiguration()
                .WithAssembly(ThisAssembly)
                .WithAssembly(assembly);

            using (var container = configuration.CreateContainer())
            {
                container.TryGetExport<T>(out var obj);
                return obj;
            }
        }

        private static CompositionHost CreateFolderContainer()
        {
            var path = GetSimpleDataAssemblyPath ();
            var assemblies = Directory.GetFiles(path, "Simple.Data.*.dll")
                    .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath);

            var configuration = new ContainerConfiguration()
                .WithAssembly(ThisAssembly)
                .WithAssemblies(assemblies);

            return configuration.CreateContainer();
        }

        private static CompositionHost CreateAppDomainContainer()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(IsSimpleDataAssembly);

            var configuration = new ContainerConfiguration()
                .WithAssemblies(assemblies);

            return configuration.CreateContainer();
        }

        private static bool IsSimpleDataAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetFullName().StartsWith("Simple.Data.", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
