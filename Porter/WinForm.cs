using AvaloniaEdit;
using Microsoft.Data.Sqlite;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Porter
{
    public class WinForms
    {
        private string projectFile;
        private string projectPath;

        private Logger.Logger logger = new Logger.Logger();

        public Dictionary<string, string> winformsComponents = new Dictionary<string, string>();

        // Accepts the name of the project file with its extension
        public WinForms(string projectFile)
        {
            this.projectFile = projectFile;
            Debug.WriteLine("Project file is " + projectFile);

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

        public void ConvertDesignerToAxaml()
        {
            // We will grab anything with private modifer after we find #endregion
            // Then we will grab properties of each element

            string[] oldDesigner = File.ReadAllLines(projectPath + "\\Form1.Designer.cs");
            List<string> textToWrite = new List<string>();

            // First step is two fill our dictionary with the component type and name
            foreach (String i in oldDesigner)
            {
                if (i.Contains("new System.Windows.Forms."))
                {
                    // Grab the name of the component
                    string[] initSplit = i.Split("=");
                    string[] nameSplit = initSplit[0].Split(".");
                    string name = nameSplit[1].Trim();

                    // Grab the type of the component
                    string[] typeSplit = initSplit[1].Split(".");
                    string type = typeSplit[typeSplit.Length - 1];
                    type = type.Substring(0, type.Length - 3); // We have to trim the ();

                    winformsComponents.Add(name, type); // Add to our dictionary
                }
                
            }

            // We need to write the header boilerplate
            textToWrite.Add("<Window xmlns=\"https://github.com/avaloniaui\"");
            textToWrite.Add("   xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
            textToWrite.Add("   xmlns:d=\"http://schemas.microsoft.com/expression/blend/2008\"");
            textToWrite.Add("   xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"");
            textToWrite.Add("   mc:Ignorable=\"d\" d:DesignWidth=\"800\" d:DesignHeight=\"450\"");
            textToWrite.Add("   x:Class=\"AvaloniaApplication1.MainWindow\"");
            textToWrite.Add("   Title=\"AvaloniaApplication1\" >\r\n");
            textToWrite.Add("   <Canvas>\n");


            // Establish connnect to our sqlite db
            // Each winforms component is a table in the db in lower case
            using (var connection = new SqliteConnection("Data Source=C:\\Users\\chris\\OneDrive\\Desktop\\Senior Design\\Team5Analyzer\\Porter\\properties.db"))
            {
                connection.Open();

                foreach(var element in winformsComponents)
                {

                    List<string> winformProp = new List<string>();
                    List<string> inBetween = new List<string>();
                    List<string> avaloniaProp = new List<string>();
                    List<string> propertyValue = new List<string>();

                    foreach (String i in oldDesigner)
                    {
                        if (i.Contains("this." + element.Key + ".")) // Key is the name of the component
                        {
                            string[] propertySplit;
                            // We need to grab the property and its value
                            if (i.Contains(" = "))
                            {
                                propertySplit = i.Split(" = ");
                            }
                            else
                            {
                                propertySplit = i.Split(" += ");
                            }
                            string[] leftPropertySplit = propertySplit[0].Split(".");

                            winformProp.Add(leftPropertySplit[2].Trim());
                            string tmp = propertySplit[1].Trim();
                            propertyValue.Add(tmp.Remove(tmp.Length - 1, 1));

                            // Now we query database for the avalonia property name equivalent
                            var command = connection.CreateCommand();
                            command.CommandText = "SELECT * FROM " + element.Value.ToLower() + " WHERE winform = '" + winformProp[winformProp.Count() - 1] + "'";

                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    for (int x = 0; x < reader.FieldCount; x++)
                                    {
                                        if (x == 1) // in_between column
                                        {
                                            inBetween.Add(reader.GetValue(x).ToString());
                                        }
                                        if (x == 2) // avalonia column
                                        {
                                            avaloniaProp.Add(reader.GetValue(x).ToString());
                                        }

                                    }
                                }
                            }
                        }
                        
                    }

                    // We finished getting data for current winforms component
                    // Now we write that to the the file
                    if (winformProp.Count() > 0)
                    {
                        string temp = "     <" + element.Value;
                        string middleValue = "";


                        for (int x = 0; x < avaloniaProp.Count(); x++)
                        {

                            if (inBetween[x].Equals("1"))
                            {
                                middleValue += propertyValue[x].Substring(1, propertyValue[x].Length - 2);
                            }
                            else
                            {
                                if (winformProp[x].Equals("Text"))
                                {
                                    middleValue += propertyValue[x];
                                    break;
                                }
                                if (winformProp[x].Equals("Location"))
                                {
                                    int startIndex = propertyValue[x].IndexOf("(");
                                    int endIndex = propertyValue[x].IndexOf(")", startIndex);
                                    string substring = propertyValue[x].Substring(startIndex + 1, endIndex - startIndex - 1);
                                    string[] points = substring.Split(", ");
                                    temp += " Canvas.Left=\"" + points[0] + "\" " + " Canvas.Top=\"" + points[1] +"\"";
                                }
                                else if (winformProp[x].Equals("Size"))
                                {
                                    int startIndex = propertyValue[x].IndexOf("(");
                                    int endIndex = propertyValue[x].IndexOf(")", startIndex);
                                    string substring = propertyValue[x].Substring(startIndex + 1, endIndex - startIndex - 1);
                                    string[] points = substring.Split(", ");
                                    temp += " Width=\"" + points[0] + "\"" + " Height=\"" + Int32.Parse(points[1]) * 2 + "\"";
                                }
                                else if (winformProp[x].Equals("Name"))
                                {
                                    temp += " " + avaloniaProp[x] + "=" + propertyValue[x];
                                }
                                else if (winformProp[x].Equals("Click"))
                                {
                                    int startIndex = propertyValue[x].IndexOf("(") + 5;
                                    int endIndex = propertyValue[x].IndexOf(")", startIndex);
                                    string substring = propertyValue[x].Substring(startIndex + 1, endIndex - startIndex - 1);
                                    temp += " " + avaloniaProp[x] + "=" + "\"" + substring + "\"";
                                }
                                else 
                                {
                                    temp += " " + avaloniaProp[x] + "=" + "\"" + propertyValue[x] + "\"";
                                }
                            }
                        }
                        temp += ">" + middleValue + "</" + element.Value + ">";
                        textToWrite.Add(temp);

                        // Clear all the lists for the next component
                        winformProp.Clear();
                        inBetween.Clear();
                        avaloniaProp.Clear();
                        propertyValue.Clear();
                    }

                }          
            }

            // End the window
            textToWrite.Add("   </Canvas>\n");
            textToWrite.Add("</Window>");

            File.WriteAllLines(projectPath + "\\MainWindow.axaml", textToWrite);


            // We now have all the information we need
            // Now we have to write the information in axaml form
        }

        public void MoveCSFiles()
        {
            string[] oldCSFile = File.ReadAllLines(projectPath + "\\Form1.cs");
            List<string> newCSFile = new List<string>();


            // Append referrences
            newCSFile.Add("using Avalonia.Controls;");
            newCSFile.Add("using Avalonia.Interactivity;\n");

            //Delete this
            Debug.WriteLine("I am printing the dictionary");
            foreach (var element in winformsComponents)
            {
                Debug.WriteLine("Key " + element.Key + " Value " + element.Value);
            }

            foreach (String i in oldCSFile)
            {
                if (i.Contains(" partial class "))
                {
                    newCSFile.Add("     public partial class MainWindow : Window");
                }
                else if (i.Contains("public Form1()"))
                {
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
                else if (i.Contains("using System.Collections.Generic;"))
                {
                    // Conflicts with Avalonia
                }
                else if (i.Contains("using System.ComponentModel;"))
                {
                    // Conflicts with Avalonia
                }
                else if (i.Contains("using System.Data;"))
                {
                    // Conflicts with Avalonia
                }
                else if (i.Contains("using System.Drawing;"))
                {
                    // Conflicts with Avalonia
                }
                else if (i.Contains("using System.Linq;"))
                {
                    // Conflicts with Avalonia
                }
                else if (i.Contains("using System.Content;"))
                {
                    // Conflicts with Avalonia
                }
                else if (i.Contains("using System.Threading.Tasks;"))
                {
                    // Conflicts with Avalonia
                }
                else if (i.Contains("using System.Windows.Forms;"))
                {
                    // Conflicts with Avalonia
                }
                else if (i.Contains(".Checked"))
                {
                    Debug.WriteLine("contains .Checked");
                    Debug.WriteLine("pre checked replace string");
                    Debug.WriteLine(i);
                    string newI = i.Replace("Checked", "IsChecked == true");
                    Debug.WriteLine("post checked replace string");
                    Debug.WriteLine(newI);
                    newCSFile.Add(newI);
                }
                else if (i.Contains(".Text"))
                {
                    string newI = "";
                    Debug.WriteLine("i contains .Text");
                    // We have to check our dictionary of compontents to see if its referring to Text of a textbox
                    // If it is, we have to change .Text to .Content
                    foreach (var element in winformsComponents)
                    {
                        if (element.Value.Equals("Button") || element.Value.Equals("Label"))
                        {
                            Debug.WriteLine("the current element value is " + element.Value + " and key is " + element.Key);
                            if (i.Contains(element.Key + ".Text"))
                            {
                                Debug.WriteLine("The offensive line is " + i);
                                newI = i.Replace(element.Key + ".Text", element.Key + ".Content");
                                Debug.WriteLine("after replace " + newI);
                            }
                        }
                        else
                        {
                            if (!(newI.Length > 1))
                                newI = i;
                        }
                        
                    }
                    Debug.WriteLine("writing " + newI);
                    newCSFile.Add(newI);
                }
                else
                {
                    newCSFile.Add(i);
                }
            }

            File.WriteAllLines(projectPath + "\\MainWindow.axaml.cs", newCSFile); // TODO: Change to MainWindow.axaml.cs
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
