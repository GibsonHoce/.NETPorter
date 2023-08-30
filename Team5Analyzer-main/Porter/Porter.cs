//Converts to sdk-style
using Logger;
using System.Text;
namespace Porter
{
    public class Porter
    {

        string FilePath;
        string ProjectDirectory;

        //Logger.Logger logger = new Logger.Logger();

        public Porter(string FilePath)
        {
            this.FilePath = FilePath;
            System.Diagnostics.Debug.WriteLine("Filepath: "+FilePath);
            // Get the directory
            for (int i = FilePath.Length-1; i > 0; i--)
            {
                if (FilePath[i] == '\\')
                {
                    ProjectDirectory = FilePath.Substring(0, i);
                    break;
                }
            }
        }

        public int FixProjectStyle()
        {
            System.Diagnostics.Debug.WriteLine("Fixing project style");
            //logger.appendMessage("Fixing project style", Logger.Logger.MessageType.Message);

            List<String> references = new List<string>(); //references
            List<String> projectReferences = new List<string>();
            try
            {
                String[] inputLines = File.ReadAllLines(FilePath);
                foreach (String i in inputLines)
                {
                    //references
                    //System.Diagnostics.Debug.WriteLine("current line: "+i);
                    String pre = "";
                    String post = "";
                    String subS = "";
                    if (i.Contains("<Reference Include=\""))
                    {
                        pre = "<Reference Include=\"";
                        post = "\" />";
                        int preI = i.IndexOf(pre) + pre.Length;
                        int postI = i.IndexOf(post);
                        if (!((postI - preI) < 0))
                        {
                            subS = i.Substring(preI, postI - preI);
                            System.Diagnostics.Debug.WriteLine("Reference added: " + subS);
                            //logger.appendMessage("Reference added: " + subS, Logger.Logger.MessageType.Message);

                            references.Add(subS);
                        }

                    }
                    else if (i.Contains("<ProjectReference Include=\"")) //project references
                    {
                        pre = "<ProjectReference Include=\"";
                        post = "\">";
                        int preI = i.IndexOf(pre) + pre.Length;
                        int postI = i.IndexOf(post);
                        if (!((postI - preI) < 0))
                        {
                            subS = i.Substring(preI, postI - preI);
                            System.Diagnostics.Debug.WriteLine("ProjectReference added: " + subS);
                            //logger.appendMessage("ProjectReference added: " + subS, Logger.Logger.MessageType.Message);
                            projectReferences.Add(subS);
                        }
                    }

                }
            }
            catch (System.UnauthorizedAccessException)
            {
                //TODO: handle exception properly
                System.Diagnostics.Debug.WriteLine("Unauthorized to access file");
            }
            
            

            //build new file
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            sb.AppendLine("  <PropertyGroup>");
            sb.AppendLine("    <TargetFramework>net6</TargetFramework>");
            sb.AppendLine("    <OutputType>Library</OutputType>");
            sb.AppendLine("    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine("  <PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' \">");
            sb.AppendLine("    <DefineConstants>TRACE;USE_ODM2</DefineConstants>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine("  <PropertyGroup>");
            sb.AppendLine("    <AssemblyOriginatorKeyFile>");
            sb.AppendLine("    </AssemblyOriginatorKeyFile>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine("  <ItemGroup>");
            foreach (string s in references)
            {
                sb.AppendLine("    <Reference Include=\"" + s + "\" />");
            }
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine("  <ItemGroup>");
            foreach (string s in projectReferences)
            {
                sb.AppendLine("    <ProjectReference Include=\"" + s + "\" />");
            }
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine("</Project>");
            //String testfile = ProjectDirectory+"\\testconvert\\test.csproj";
            try { 
            using (StreamWriter sw = new StreamWriter(FilePath))
            {
                sw.Write(sb.ToString());
            }
            }catch (System.UnauthorizedAccessException)
            {
                //TODO: handle exception properly
                System.Diagnostics.Debug.WriteLine("Unauthorized to access file");
            }
            return 0;
        }
        //Converts to packagereference
        public int FixPackageStyle()
        {
            System.Diagnostics.Debug.WriteLine("Fixing package style");
            //logger.appendMessage("Fixing package style", Logger.Logger.MessageType.Message);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("  <ItemGroup>");

            String packagesFile = ProjectDirectory + "\\packages.config";
            String[] inputLines = File.ReadAllLines(packagesFile);
            foreach (String i in inputLines)
            {
                //references
                //System.Diagnostics.Debug.WriteLine("current line: "+i);
                String pre = "";
                String post = "";
                String packageName = "";
                String packageVersion = "";
                if (i.Contains("<package id=\""))
                {
                    pre = "<package id=\"";
                    post = "\" version=\"";
                    int preI = i.IndexOf(pre) + pre.Length;
                    int postI = i.IndexOf(post);
                    if (!((postI - preI) < 0))
                    {
                        packageName = i.Substring(preI, postI - preI);
                        //System.Diagnostics.Debug.WriteLine("PackageReference name: " + packageName);
                        //logger.appendMessage("PackageReference name " + packageName, Logger.Logger.MessageType.Message);
                    }

                    pre = "version=\"";
                    post = "\" targetFramework";
                    preI = i.IndexOf(pre) + pre.Length;
                    postI = i.IndexOf(post);
                    if (!((postI - preI) < 0))
                    {
                        packageVersion = i.Substring(preI, postI - preI);
                        //System.Diagnostics.Debug.WriteLine("PackageReference version: " + packageVersion);
                        //logger.appendMessage("PackageReference version " + packageVersion, Logger.Logger.MessageType.Message);

                    }
                    sb.AppendLine("    <PackageReference Include=\"" + packageName + "\" Version=\"" + packageVersion + "\" />");

                }

            }

            sb.AppendLine("  </ItemGroup>");

            List<string> lines = File.ReadAllLines(FilePath).ToList();
            lines.Insert(lines.Count - 1, sb.ToString());
            File.WriteAllLines(FilePath, lines);

            //delete packages file
            if (File.Exists(Path.GetFullPath(packagesFile)))
            {
                File.Delete(Path.GetFullPath(packagesFile));
            }

            return 0; //error
        }

    }
}
