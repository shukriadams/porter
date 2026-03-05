using System.Text;
using System.Text.RegularExpressions;
using Porter.Porter_Packages.MadScience_Shell;
using Newtonsoft.Json;

namespace Porter
{
    public class Install
    {
        // files to ignore when transferring porter CS files
        private string[] _csFileBlacklist = new string[] {
            // assembly definition from package will break parent project's properties
            "Properties/CustomAssemblyInfo.cs"
        };

        private string _workDirPath;

        public void Work(string installPath)
        {

            // confirm dir exists
            if (!Directory.Exists(installPath))
            {
                Console.WriteLine($"ERROR : Install directory \"{installPath}\" not found");
                Environment.Exit(1);
                return;
            }

            string absolutePath = Path.GetFullPath(installPath);
            if (absolutePath == installPath)
                absolutePath = string.Empty;
            else
                absolutePath = $" (abs path {absolutePath})";

            Console.WriteLine($"Attempting to install in path \"{installPath}\"{absolutePath}");

            // verify that install path has a porter.json file
            try
            {
                if (!File.Exists(Path.Join(installPath, "porter.json")))
                {
                    Console.WriteLine($"ERROR : Cound not find a porter.json file in \"{installPath}\"");
                    Environment.Exit(1);
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR trying to read contents of directory \"{installPath}\": {ex.Message}");
                Environment.Exit(1);
                return;
            }

            // ensure git installed
            Shell gitCall = new Shell("git --help");
            int result = gitCall.Run();
            if (result != 0)
            {
                Console.WriteLine($"Failed to get a response from git - is it installed? Error {result}, {gitCall.Err}");
                Environment.Exit(1);
            }

            // create work directory for porter, it needs this to temporarily story stuff in
            _workDirPath = Path.Join(installPath, ".porter");
            try
            {
                Directory.CreateDirectory(_workDirPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR : could not create working dir {_workDirPath}");
                Environment.Exit(1);
                return;
            }

            // start recursing process from top level dir
            this.ProcessDirectoryLevel(installPath, new List<string>(), null, true);

            Console.WriteLine("Done installing");
        }

        private bool GlobMatch(string globPattern, string path)
        {
            return new Regex(
                "^" + Regex.Escape(globPattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            ).IsMatch(path);
        }

        private void ProcessDirectoryLevel(string currentDir, List<string> context, string[] required_run_times, bool isTopLevelPackage)
        {
            // load porter.json that we already proved exists in this dir
            string porterFilePath = Path.Join(currentDir, "porter.json");
            string rawPorterFileContent;
            try
            {
                rawPorterFileContent = File.ReadAllText(porterFilePath);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ERROR could not read file \"{porterFilePath}\": {ex.Message}");
                Environment.Exit(1);
                return;
            }

            // attempt to parse porter.json content from JSON 
            PorterPackage porterPackage;
            try
            {
                porterPackage = JsonConvert.DeserializeObject<PorterPackage>(rawPorterFileContent);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ERROR could not deserialize JSON in file \"{porterFilePath}\": {ex.Message}. JSON is :");
                Console.WriteLine(rawPorterFileContent);
                Environment.Exit(1);
                return;
            }

            // top-level package must declare a runtime requirement, this will be passed down to all nested packages
            if (isTopLevelPackage)
            {
                if (porterPackage.runtimes == null || !porterPackage.runtimes.Any())
                {
                    Console.WriteLine("top level project must declare at least one expected runtime");
                    Environment.Exit(1);
                    return;
                }

                required_run_times = porterPackage.runtimes;
            }

            List<PackageAddress> packages_to_install = new List<PackageAddress>();
            string package_name = porterPackage.name;

            context = context.GetRange(0, context.Count); // copy array so we don't end up cascading additions to adjacent recursive calls
            context.Add(package_name);

            // generate package to install from conf packages
            if (porterPackage.packages != null)
                foreach(string package in porterPackage.packages)
                {
                    // get package source from package string. format is expected to be 
                    // source.author+repo@tag
                    Regex packageSourceRegex = new Regex("([^.]*).(.*)@(.*)");
                    Match parts = packageSourceRegex.Match(package);
                    if(!parts.Success || parts.Groups.Count < 4)
                    {
                        Console.WriteLine($"Package reference {package} in {porterFilePath} is malformed.");
                        Environment.Exit(1);
                        return;
                    }
                        
                    string package_source = parts.Groups[1].ToString();
                    string auth_repo = parts.Groups[2].ToString();
                    string tag = parts.Groups[3].ToString();

                    if (package_source != "github")
                    {
                        Console.WriteLine($"Error : only github currently supported : {package_source}");
                        Environment.Exit(1);
                        return;
                    }
                    
                    //github repos dont have . they must be / instead
                    auth_repo = auth_repo.Replace(".", "/");

                    PackageAddress p = new PackageAddress{
                        Repo = auth_repo, 
                        Tag = tag};

                    packages_to_install.Add(p);
                }

            // ensure that a child packages are not referenced more than once
            foreach(PackageAddress package in packages_to_install)
                if (packages_to_install.Where(p => p.Repo == package.Repo).Count() > 1)
                {
                    Console.WriteLine($"Error : package {package.Repo} is reference dmore than once by {porterFilePath}");
                    Environment.Exit(1);
                    return;                
                }

            // create a child directory to in install porter into it
            string porter_packages_dir = Path.Join(currentDir, "porter");
            try
            {
                Directory.CreateDirectory(porter_packages_dir);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error : could not create porter dir {porter_packages_dir} {ex}");
                Environment.Exit(1);
                return;                
            }

            // install each package
            foreach(PackageAddress package in packages_to_install)
            {   
                /*
                    create temp dir in porter's own work dir, use deterministic path so we always
                    clean up after ourselves. Path is based on package's total namespace depth, so
                    no chance of collision with other packages. make it filesystem safe by safe 
                    base64 encoding. Before cloning always delete the temp path
                */
                string package_nested_name = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join("_", context)));
                string package_temp_dir = Path.Join(_workDirPath, package_nested_name);
                
                // convert to abs path for remapping later
                package_temp_dir = Path.GetFullPath(package_temp_dir);

                if (Directory.Exists(package_temp_dir))
                {
                    try
                    {
                        Directory.Delete(package_temp_dir, true);
                        Directory.CreateDirectory(package_temp_dir);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error : could not recreate porter temp dir {package_temp_dir} {ex}");
                        Environment.Exit(1);
                        return; 
                    }
                }

                // clone package to temp location, we need to analyse it first
                Shell shell = new Shell($"git clone --branch {package.Tag} {package.Source}/{package.Repo} {package_temp_dir}");
                int exitCode = shell.Run();
                if (exitCode != 0)
                {
                    Console.WriteLine($"ERROR : failed to clone package {package.Source}/{package.Repo} at tag {package.Tag}. Exit code {exitCode}");
                    Environment.Exit(1);
                    return; 
                }

                // package we just cloned must have a porter.json file in it, else we ignore it
                string package_porter_file_path = Path.Join(package_temp_dir, "porter.json");
                bool isPackagePorter = true;
                if (!File.Exists(package_porter_file_path))
                {
                    Console.WriteLine($"Warning : package @ path {package_temp_dir} is not a porter package");
                    continue;
                }

                
                PorterPackage this_package_porter_conf;
                try
                {
                    this_package_porter_conf = JsonConvert.DeserializeObject<PorterPackage>(File.ReadAllText(package_porter_file_path));
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"ERROR : failed to load/parse package file {package_porter_file_path}");
                    Environment.Exit(1);
                    return; 
                }

                string this_package_name = this_package_porter_conf.name;

                string[] ignore_paths = new string[] {};
                if (this_package_porter_conf.ignore != null)
                    ignore_paths = this_package_porter_conf.ignore;

                string package_copy_root = package_temp_dir;
                if (!string.IsNullOrEmpty(this_package_porter_conf.export))
                    package_copy_root = Path.Join(package_temp_dir, this_package_porter_conf.export);

                // enforce top level runtimes on this
                var this_package_runtimes = this_package_porter_conf.runtimes;
                if (!this_package_runtimes.Intersect(required_run_times).Any())
                {
                    Console.WriteLine($"{this_package_name} runtimes {this_package_runtimes} do not align with required runtimes {required_run_times}");
                    Environment.Exit(1);
                    return; 
                }

                // destroy then create public directory of this package
                string child_package_dir = Path.Join(porter_packages_dir, this_package_name);
                // convert to absolute for remapping
                child_package_dir = Path.GetFullPath(child_package_dir);

                if (Directory.Exists(child_package_dir))
                    Directory.Delete(child_package_dir, true);

                if (!Directory.Exists(child_package_dir))
                    Directory.CreateDirectory(child_package_dir);

                // find all .cs files in package temp, we want to wrap and copy them
                string[] cs_files = Directory.GetFileSystemEntries(package_copy_root, "*", SearchOption.AllDirectories);
                cs_files = cs_files.Where(f => f.ToLower().EndsWith(".cs")).ToArray();
                foreach (string c_file in cs_files)
                {
                    // convert to abs path for easier remap
                    string cs_file = Path.GetFullPath(c_file);

                    // is CS file on blacklist?
                    if (_csFileBlacklist.Contains(Path.GetFileName(cs_file)))
                    {
                        Console.WriteLine($"Ignoring blacklisted file {cs_file}");
                        continue;
                    }

                    bool ignore = false;
                    foreach(string ignorePath in ignore_paths)
                    {
                        if (GlobMatch(ignorePath, cs_file))
                        {
                            ignore = true;
                            break;
                        }
                    }

                    if (ignore)
                    {
                        continue;
                    }

                    string file_content = File.ReadAllText(cs_file);

                    // wrap file contents in namespace stack threaded down package stack 
                    string namespace_lead = "//PORTER-WRAPPER!\n";
                    string namespace_tail= "";
                    foreach (var this_context in context)
                    {
                        namespace_lead = namespace_lead +
                            "namespace " + this_context + ".Porter_Packages {\n";

                        namespace_tail = namespace_tail + "}\n";
                    }

                    namespace_lead = $"{namespace_lead}//PORTER-WRAPPER!\n\n\n";
                    namespace_tail = $"\n\n//PORTER-WRAPPER!\n{namespace_tail}//PORTER-WRAPPER!";
                    file_content =  $"{namespace_lead}{file_content}{namespace_tail}";

                    // remap .cs file in temp dir to public child package dir
                    string remapped_file_path = cs_file.Replace(package_copy_root, child_package_dir);

                    // create target directory
                    string remapped_file_dir = Path.GetDirectoryName(remapped_file_path);
                    if (!Directory.Exists(remapped_file_dir))
                        Directory.CreateDirectory(remapped_file_dir);

                    File.WriteAllText(remapped_file_path, file_content);
                }

                // add our own metadata to porter.json
                this_package_porter_conf.__installed = DateTime.Now.ToString();

                // write porter.json to target location for reference.
                File.WriteAllText(Path.Join(child_package_dir, "porter.json"), JsonConvert.SerializeObject(this_package_porter_conf));

                // clean up temp child package dir
                Directory.Delete(package_temp_dir, true);
            
                Console.WriteLine($"Installed package {this_package_name} @ {package.Tag}");

                // finally recurse by running in child package dir,
                ProcessDirectoryLevel(child_package_dir, context, required_run_times, false);
            }
        }
    }
}