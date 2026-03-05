using System.Reflection;
using MadScience_Reflection;

namespace Porter
{
    public class Version
    {
        public void Work()
        {
            string currentVersion = ResourceHelper.ReadNamedResourceAsString(Assembly.GetCallingAssembly(), "Porter.currentVersion.txt");
            Console.WriteLine($"version : {currentVersion}");
        }
    }    
}

