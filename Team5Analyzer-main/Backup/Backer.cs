using System.Diagnostics;
using System.IO;

namespace Backer
{
    public class Backer
    {

        String ProjectDirectory;

        // We have to remove the csproj portion of path to get to the route.
        public Backer()
        {
            
        }

        public static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            string folderName = "";
            Debug.WriteLine(sourcePath);

            // Create the new folder 
            for (int i = sourcePath.Length - 1; i > 0; i--)
            {
                if (sourcePath[i] == '\\')
                {
                    folderName = sourcePath.Substring(i + 1);
                    break;
                }
            }

            Debug.WriteLine("FOlder name ios " + folderName);

            targetPath = targetPath + "\\" + folderName + ".backup";

            System.IO.Directory.CreateDirectory(targetPath);

            // Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }
    }
}