using System.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Backer;
using Avalonia.Controls;
using Avalonia.Media;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

namespace ConsoleApp
{
    class Program
    {
        Analyzer.Analyzer analyzer;
        Logger.Logger logger = new Logger.Logger();

        public Program()
        {

        }

        static void Main(string[] args)
        {

            //Create program object
            Program program = new Program();

            //Intro
            program.intro();

            //Main program loop
            while (true) 
            {
                int ret = program.run(program);
                if (ret == 0) 
                {
                    //Exit
                    break;
                }
                else if (ret == 1) 
                {
                    //Loop
                }
            }

            Console.WriteLine("\tGoodbye");
            return;

        }

        public int run(Program program)
        {
            //Select Project File
            string[] FilePaths = program.SelectFile(0);

            Console.WriteLine("\t---Saving Filepath---");

            //Save project filepath
            string projectFilePath = FilePaths[0];
            string projectFilePathLong = FilePaths[1];

            Console.WriteLine("\t---Filepath Saved---");

            //Select Backup File
            FilePaths = program.SelectFile(1);

            Console.WriteLine("\t---Creating Backup---");

            //Safe backup filepath
            string backupFilePath = FilePaths[0];
            string backupFilePathLong = FilePaths[1];

            //Save Backup
            Debug.WriteLine("Backing up " + projectFilePathLong + " to location " + backupFilePathLong);
            try
            {
                string sourcePath = "";
                for (int i = projectFilePathLong.Length - 1; i > 0; i--)
                {
                    if (projectFilePathLong[i] == '\\')
                    {
                        sourcePath = projectFilePathLong.Substring(0, i);
                        break;
                    }
                }

                Backer.Backer.CopyFilesRecursively(sourcePath, backupFilePathLong);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return 1;
            }

            Console.WriteLine("\t---Backup Created---");

            Console.WriteLine("\t---Beginning Analysis---");

            //Initialize analyzer
            Analyzer.Analyzer analyzer = new Analyzer.Analyzer(projectFilePathLong);

            //Analyze
            int[] data = analyzer.Analyze();
            string version = analyzer.getVersion();

            //Print analysis
            string references = "References: " + data[0] + "\n";
            string classes = "Classes: " + data[1] + "\n";
            string methods = "Methods: " + data[2] + "\n";
            string versionString = "Version: " + version;

            Console.WriteLine("\n" + references + classes + methods + versionString);

            Console.WriteLine("\t---Analysis Complete---");

            //.Net 6 Project Selected
            if (version == "net6")
            {
                Console.WriteLine("\tWarning! Project version is already .net 6. Would you like to port another project? Y/N");
                while (true)
                {
                    string answer = Console.ReadLine();
                    if (answer == "Y")
                    {
                        //Loop
                        return 1;
                    }
                    else if (answer == "N")
                    {
                        //End program
                        return 0;
                    }
                }
            }

            //Proceed Check
            Console.WriteLine("\tThe project is ready to be ported to .net 6, would you like to proceed? Y/N");
            while (true)
            {
                string answer = Console.ReadLine();
                if (answer == "Y")
                {
                    break;
                }
                else if (answer == "N")
                {
                    //Loop Check
                    Console.WriteLine("\tWould you like to port another project? Y/N");
                    while (true)
                    {
                        answer = Console.ReadLine();
                        if (answer == "Y")
                        {
                            //Loop program
                            return 1;
                        }
                        else if (answer == "N")
                        {
                            //End program
                            return 0;
                        }
                    }
                }
            }

            Console.WriteLine("\t---Beginning Port---");

            //Port
            Porter.Porter port = new Porter.Porter(projectFilePathLong);
            port.FixProjectStyle();
            port.FixPackageStyle();
            string[] filesindirectory = Directory.GetFiles(projectFilePath);
            filesindirectory = filesindirectory.Where(file => file.EndsWith(".cs")).ToArray();

            Console.WriteLine("\t---Port Complete---");

            return 0;

            //Loop Check
            Console.WriteLine("\tThe project has been ported to .net 6, would you like to port another project? Y/N");
            while (true)
            {
                string answer = Console.ReadLine();
                if (answer == "Y")
                {
                    //Loop program
                    return 1;
                }
                else if (answer == "N")
                {
                    //End program
                    return 0;
                }
            }
        }

        //Intro
        public void intro()
        {
            Console.WriteLine("\t.Net Porter");
            Console.WriteLine("\tVersion:\t1.0");
            Console.WriteLine("\tCreated by:\tSaif Alkaabi\tWilliam Hoce\tChris Mamatas\tDaniel Stiller");
            Console.WriteLine("\t------------------------------");
            return;
        }

        //Select Project File
        public string[] SelectFile(int state)
        {
            string[] filePaths;
            string file_Path;
            string file_Path_Long;

            //state 0: Project Selection (default)
            //state 1: Backup Selection

            while (true)
            {
                //Ask for input
                if (state == 0)
                {
                    Console.WriteLine("\tEnter csproj filepath");
                }
                else if (state == 1)
                {
                    Console.WriteLine("\tEnter backup filepath");
                }

                //Obtain input
                string result = Console.ReadLine();

                //Ensure input exists
                if (result != null)
                {

                    Debug.WriteLine(result);

                    try
                    {
                        string temp = result;
                        file_Path_Long = temp;
                        int projectLabel = temp.LastIndexOf("\\");
                        file_Path = temp.Substring(0, projectLabel);
                        string fileExt = System.IO.Path.GetExtension(file_Path_Long);

                        //State 0, ensure .csproj and file exists
                        if (state == 0 && (fileExt != ".csproj" || !File.Exists(file_Path_Long)))
                        {
                            throw new Exception("File does not meet criteria");
                        }

                        //Add to array
                        filePaths = new string[] { file_Path, file_Path_Long };

                        Debug.WriteLine(file_Path + "\n");
                        Debug.WriteLine(file_Path_Long + "\n");

                        return filePaths;

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("\tPlease enter a valid filepath");
                        Debug.WriteLine(ex.ToString());
                    }
                }
            }
        }
    }
}