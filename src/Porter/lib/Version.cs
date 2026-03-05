using System.Reflection;
using MadScience_Reflection;

namespace Porter
{
    public class Version
    {
        public void Work()
        {
            string currentVersion = ResourceHelper.ReadResourceAsString(Assembly.GetExecutingAssembly(), "currentVersion.txt");
            Console.WriteLine($"version : {currentVersion}");
        }
    }    
}

