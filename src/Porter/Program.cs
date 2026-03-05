using Porter.Porter_Packages.Madscience_CommandLineSwitches;

namespace Porter
{
    class Program
    {
        static void Main(string[] args)
        {
            try 
            {
                CommandLineSwitches switches = new CommandLineSwitches(args);

                Console.WriteLine("Porter, a package manager for C#");

                if (switches.InvalidArguments.Any())
                {
                    Console.WriteLine("ERROR : invalid switch(es):");
                    foreach(var r in switches.InvalidArguments)
                        Console.WriteLine(r);

                    System.Environment.Exit(1);
                }
                
                string command = null;
                if (switches.Contains("install") || switches.Contains("i"))
                    command = "install";
                
                if (switches.Contains("version") || switches.Contains("v"))
                    command = "version";

                
                if (command == null || switches.Contains("help") || switches.Contains("h"))
                {
                    Console.WriteLine("Usage:");
                    Console.WriteLine("");
                    Console.WriteLine("--help |-h : this help message");
                    Console.WriteLine("--install | -i <optional PATH> : installs Porter packages.");
                    Console.WriteLine("    <PATH> is optional directory where Porter packages will be installed. ");
                    Console.WriteLine("    If no directory is given, the current working directory is used.");
                    Console.WriteLine("    The directory used must contain a valid porter.json file.");
                }

                if (command == "install")
                {
                    string installPath = switches.Get("install", "i");
                    if (string.IsNullOrEmpty(installPath))
                        installPath = Directory.GetCurrentDirectory();

                    Install install = new Install();
                    install.Work(installPath);
                }

                if (command == "version")
                {
                    Version version = new Version();
                    version.Work();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}