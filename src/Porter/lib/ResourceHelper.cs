using System.Reflection;

namespace MadScience_Reflection
{
    public class ResourceHelper
    {

        public static string ReadNamedResourceAsString(Assembly assembly, string resourceFullname)
        {

            using (Stream stream = assembly.GetManifestResourceStream(resourceFullname))
            {
                if (stream == null)
                    throw new Exception($"Failed to load resource {resourceFullname} in assembly {assembly.FullName}.");

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

    }
}
