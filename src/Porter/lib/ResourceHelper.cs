using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MadScience_Reflection
{
    public class ResourceHelper
    {
        public static string ReadResourceAsString(Assembly assembly, string resourceName)
        {
            string assemblyName = assembly.ManifestModule.Name;
            
            if (assemblyName.ToLower().EndsWith(".dll") || assemblyName.ToLower().EndsWith(".exe"))
                assemblyName = assemblyName.Substring(0, assemblyName.Length - 4); // clip off ".dll/.exe"

            string resourceFullName = $"{assemblyName}.{resourceName}";

            using (Stream stream = assembly.GetManifestResourceStream(resourceFullName))
            {
                if (stream == null)
                    throw new Exception($"Failed to load resource {resourceFullName} in assembly {assembly.FullName}.");

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static string ReadResourceAsString(Type typeInTargetAssembly, string resourceName)
        {
            Assembly assembly = Assembly.GetAssembly(typeInTargetAssembly);
            if (assembly == null)
                throw new Exception($"Failed to find assemby for type {typeInTargetAssembly.FullName}");

            return ReadResourceAsString(assembly, resourceName);
        }

        public static string LoadFromLocalJsonOrLocalResourceAsString(Type callerType, string resourcePath)
        {
            if (!ResourceHelper.ResourceExists(callerType, resourcePath))
                return null;

            return ResourceHelper.ReadResourceAsString(callerType, resourcePath);
        }


        public static bool ResourceExists(Assembly assembly, string resourceName)
        {
            string resourceFullName = $"{assembly.GetName().Name}.{resourceName}";

            using (Stream stream = assembly.GetManifestResourceStream(resourceFullName))
            {
                return stream != null;
            }
        }

        public static bool ResourceExists(Type typeInTargetAssembly, string resourceName)
        {
            Assembly assembly = Assembly.GetAssembly(typeInTargetAssembly);
            if (assembly == null)
                throw new Exception($"Failed to find assemby for type {typeInTargetAssembly.FullName}");

            return ResourceExists(assembly, resourceName);
        }
    }
}
