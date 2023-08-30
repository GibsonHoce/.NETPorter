using System.Text;

namespace Porter
{
    public class WinForms
    {
        private string projectFile;
        private string projectPath;

        private Logger.Logger logger = new Logger.Logger();

        private List<string> elementsToAdd = new List<string>();
        private List<string> elementsToAddNames = new List<string>();

        // Accepts the name of the project file with its extension
        public WinForms(string projectFile)
        {
            this.projectFile = projectFile;

            for (int i = projectFile.Length - 1; i > 0; i--)
            {
                if (projectFile[i] == '\\')
                {
                    projectPath = projectFile.Substring(0, i);
                    break;
                }
            }

        }

        public void AddToProjFile()
        {
            logger.appendMessage("Writting Avalonia UI dependencies to " + projectFile, Logger.Logger.MessageType.Message);

            String[] oldProj = File.ReadAllLines(projectFile);
            List<string> newProj = new List<string>();


            // Remove <UseWidnowsForms>, <ImplicitUsings> and add
            // <BuiltInComInteropSupport>, <ApplicationManifest>

            foreach (String i in oldProj)
            {

                if (i.Contains("<UseWindowsForms>"))
                {
                    newProj.Add("    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>");
                }
                else if (i.Contains("<ImplicitUsings>"))
                {
                    newProj.Add("    <ApplicationManifest>app.manifest</ApplicationManifest>");
                }
                else if (i.Contains("</Project>"))
                {
                    // Write the new <ItemGroups> to end
                    newProj.Add("    <ItemGroup>");
                    newProj.Add("        <TrimmerRootDescriptor Include=\"Roots.xml\" />");
                    newProj.Add("    </ItemGroup>");

                    newProj.Add("   <ItemGroup>");
                    newProj.Add("       <PackageReference Include=\"Avalonia\" Version=\"0.10.18\" />");
                    newProj.Add("       <PackageReference Include=\"Avalonia.Desktop\" Version=\"0.10.18\" />");
                    newProj.Add("       <!--Condition below is needed to remove Avalonia.Diagnostics package from build output in Release configuration.-->");
                    newProj.Add("       <PackageReference Condition=\"'$(Configuration)' == 'Debug'\" Include=\"Avalonia.Diagnostics\" Version=\"0.10.18\" />");
                    newProj.Add("       <PackageReference Include=\"XamlNameReferenceGenerator\" Version=\"1.5.1\" />");
                    newProj.Add("   </ItemGroup>");

                    newProj.Add(i);
                }
                else
                {
                    newProj.Add(i);
                }
            }

            File.WriteAllLines(projectPath + "\\AvaloniaApplication1.csproj", newProj);

            // Delete old cs.proj
            if (File.Exists(projectFile))
            {
                File.Delete(projectFile);
            }
        }

        public async void CreateProgramFile()
        {
            // Create and write boilerplate to Program.cs

            string line = "using Avalonia;\r\nusing System;\r\n\r\nnamespace AvaloniaApplication1\r\n{\r\n    internal class Program\r\n    {\r\n        // Initialization code. Don't use any Avalonia, third-party APIs or any\r\n        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized\r\n        // yet and stuff might break.\r\n        [STAThread]\r\n        public static void Main(string[] args) => BuildAvaloniaApp()\r\n            .StartWithClassicDesktopLifetime(args);\r\n\r\n        // Avalonia configuration, don't remove; also used by visual designer.\r\n        public static AppBuilder BuildAvaloniaApp()\r\n            => AppBuilder.Configure<App>()\r\n                .UsePlatformDetect()\r\n                .LogToTrace();\r\n    }\r\n}";
            
            File.WriteAllText(projectPath + "\\Program.cs", line);
        }

        public void MoveCSFiles()
        {
            string[] oldCSFile = File.ReadAllLines(projectPath + "\\Form1.cs");
            List<string> newCSFile = new List<string>();


            // Append referrences
            newCSFile.Add("using Avalonia.Controls;");
            newCSFile.Add("using Avalonia.Interactivity;\n");
            
            foreach (String i in oldCSFile)
            {
                if (i.Contains(" partial class ")) {
                    newCSFile.Add("     public partial class MainWindow : Window");
                }
                else if (i.Contains("public Form1()")) {
                    newCSFile.Add("     public MainWindow()");
                }
                else if (i.Contains("namespace"))
                {
                    newCSFile.Add("namespace AvaloniaApplication1");
                }
                else if (i.Contains("EventArgs"))
                {
                    newCSFile.Add(i.Replace("EventArgs", "RoutedEventArgs"));
                }
                else if (i.Contains(".Text")) {
                    newCSFile.Add(i.Replace(".Text", ".Content"));
                }
                else
                {
                    newCSFile.Add(i);
                }
            }

            File.WriteAllLines(projectPath + "\\MainWindow.axaml.cs", newCSFile); // TODO: Change to MainWindow.axaml.cs
        }

        public void ConvertDesignerToAxaml()
        {
            // We will grab anything with private modifer after we find #endregion
            // Then we will grab properties of each element

            string[] oldDesigner = File.ReadAllLines(projectPath + "\\Form1.Designer.cs");
            List<string> textToWrite = new List<string>();

            bool readingIn = false;
            foreach (String i in oldDesigner)
            {
                if (i.Contains("#endregion"))
                {
                    readingIn = true;
                }

                if (readingIn)
                {
                    // Grab the item out of the declaration
                    if (i.Contains("private")){
                        string[] temp = i.Trim().Split(' ');
                        elementsToAdd.Add(temp[1]);
                        elementsToAddNames.Add(temp[2].Remove(temp[2].Length - 1)); // Remove semicol
                    }

                }
            }

            //Debug.WriteLine("I shall now print all the element names");
            //foreach (String i in elementsToAdd)
            //{
            //    Debug.WriteLine("ELEMENT TO ADD: " + i);

            //}

            // we know each winform object and its name
            // we need to grab their properties next

            List<StringBuilder> properties = new List<StringBuilder>();
            List<StringBuilder> propertiesValues = new List<StringBuilder>();

            // We need to write the header boilerplate
            textToWrite.Add("<Window xmlns=\"https://github.com/avaloniaui\"");
            textToWrite.Add("   xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
            textToWrite.Add("   xmlns:d=\"http://schemas.microsoft.com/expression/blend/2008\"");
            textToWrite.Add("   xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"");
            textToWrite.Add("   mc:Ignorable=\"d\" d:DesignWidth=\"800\" d:DesignHeight=\"450\"");
            textToWrite.Add("   x:Class=\"AvaloniaApplication1.MainWindow\"");
            textToWrite.Add("   Title=\"AvaloniaApplication1\" >\r\n");
            textToWrite.Add("   <Canvas>\n");

            int index = 0;
            foreach(String name in elementsToAddNames)
            {
                foreach (String i in oldDesigner)
                {
                    if (i.Contains("this." + name + '.'))
                    {
                        // This is a property line. we want this
                        string[] temp = i.Trim().Split(' ', '.'); // split at space

                        properties.Add(new StringBuilder(temp[2])); // add to the property list

                        // Now we get the property values
                        if (temp.Length == 5)
                        {
                            // This means we only have one value to worry about
                            propertiesValues.Add(new StringBuilder(temp[4].Trim().Remove(temp[4].Length - 1).Replace("\"", "")));
                        }
                        else if (temp.Length == 8)
                        {
                            // Grab the on_click function name, remove the trailing );
                            propertiesValues.Add(new StringBuilder(temp[7].Trim().Remove(temp[7].Length - 2))); 
                        }
                        else if (temp.Length == 9)
                        {
                            // We have to grab multiple values here (EX: X,Y coords)
                            propertiesValues.Add(new StringBuilder(temp[7].Trim() + temp[8].Trim().Remove(temp[8].Length - 1)));
                        }
                    }

                    
                }

                // Here we will write all the values and properties to a string
                StringBuilder output = new StringBuilder(); // We will build the string here
                string currElement = elementsToAdd[index].ToString();

                // FOR BUTTON
                if (currElement.Equals("Button"))
                {
                    output.Append("     <Button");

                    int indexOfText = 0;

                    for (int x = 0; x < properties.Count(); x++)
                    {

                        if (properties[x].Equals("Location"))
                        {
                            // We need to change Location to Canvas.Left and Canvas.Bottom
                            string[] coords = propertiesValues[x].ToString().Split(",");
                            coords[0] = coords[0].Replace("Point(", ""); // left coord
                            coords[1] = coords[1].Replace(")", "");
                            output.Append(" " + "Canvas.Left=\"" + coords[0] + "\" Canvas.Bottom=\"" + coords[1] + "\"");
                        }
                        else if (properties[x].Equals("Text"))
                        {
                            indexOfText = x; // Save this for later
                        }
                        else if (properties[x].Equals("UseVisualStyleBackColor"))
                        {

                        }
                        else if (properties[x].Equals("Size")) 
                        {

                        }
                        else if (properties[x].Equals("Text"))
                        {
                            output.Append(" " + "Content=\"" + propertiesValues[x] + "\"");
                        }
                        else
                        {
                            output.Append(" " + properties[x] + "=\"" + propertiesValues[x] + "\"");
                        }
                    }
                    output.Append(">" + propertiesValues[indexOfText] + "</Button>");
                }

                textToWrite.Add(output.ToString());

                // Clear the lists and update the index
                properties.Clear();
                propertiesValues.Clear();
                output.Clear();

                index++;

            }

            // End the window
            textToWrite.Add("   </Canvas>\n");
            textToWrite.Add("</Window>");

            File.WriteAllLines(projectPath + "\\MainWindow.axaml", textToWrite);


            // We now have all the information we need
            // Now we have to write the information in axaml form
        }

        public void DeleteFiles()
        {
            File.Delete(projectPath + "\\Form1.resx");
            File.Delete(projectPath + "\\Form1.cs");
            File.Delete(projectPath + "\\Form1.Designer.cs");
            File.Delete(projectPath + "\\WinFormsApp1.csproj");
        }

        public void CreateAppXaml()
        {
            string[] txt = 
            {
                "<Application xmlns=\"https://github.com/avaloniaui\"",
                "      xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                "       x:Class=\"AvaloniaApplication1.App\">",
                "   <Application.Styles>",
                "       <FluentTheme Mode=\"Light\"/>",
                "   </Application.Styles>",
                "</Application>"
            };

            File.WriteAllLines(projectPath + "\\App.axaml", txt);

        }

        public void CreateAppXamlCs()
        {
            string[] txt =
            {
                "using Avalonia;",
                "using Avalonia.Controls.ApplicationLifetimes;",
                "using Avalonia.Markup.Xaml;",
                "",
                "namespace AvaloniaApplication1",
                "{",
                "   public partial class App : Application",
                "   {",
                "       public override void Initialize()",
                "       {",
                "           AvaloniaXamlLoader.Load(this);",
                "       }",
                "",
                "       public override void OnFrameworkInitializationCompleted()",
                "       {",
                "           if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)",
                "           {",
                "               desktop.MainWindow = new MainWindow();",
                "           }",
                "",
                "           base.OnFrameworkInitializationCompleted();",
                "       }",
                "   }",
                "}"
            };

            File.WriteAllLines(projectPath + "\\App.axaml.cs", txt);

        }

        public void CreateAppManifest()
        {
            string[] txt = 
            {
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
                "<assembly manifestVersion=\"1.0\" xmlns=\"urn:schemas-microsoft-com:asm.v1\">",
                "<!-- This manifest is used on Windows only.",
                "   Don't remove it as it might cause problems with window transparency and embeded controls.",
                "   For more details visit https://learn.microsoft.com/en-us/windows/win32/sbscs/application-manifests -->",
                "   <assemblyIdentity version=\"1.0.0.0\" name=\"AvaloniaTest.Desktop\"/>",
                "",
                "   <compatibility xmlns=\"urn:schemas-microsoft-com:compatibility.v1\">",
                "   <application>",
                "   <!-- A list of the Windows versions that this application has been tested on",
                "       and is designed to work with. Uncomment the appropriate elements",
                "       and Windows will automatically select the most compatible environment. -->",
                "",
                "       <!-- Windows 10 -->",
                "       <supportedOS Id=\"{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}\" />",
                "       </application>",
                "   </compatibility>",
                "</assembly>"
            };

            File.WriteAllLines(projectPath + "\\app.manifest", txt);
        }

        public void CreateRoots()
        {
            string[] txt =
            {
                "<linker>",
                "   <!-- Can be removed if CompiledBinding and no reflection are used -->",
                "   <assembly fullname=\"AvaloniaApplication1\" preserve=\"All\" />",
                "   <assembly fullname=\"Avalonia.Themes.Fluent\" preserve=\"All\" />",
                "</linker>"
            };

            File.WriteAllLines(projectPath + "\\Roots.xml", txt);
        }
    }
}
