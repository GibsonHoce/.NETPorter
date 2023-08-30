using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer
{
    public class Analyzer : CSharpSyntaxWalker
    {
        String FilePath;
        String ProjectDirectory;
        Logger.Logger logger;

        int referenceCount = 0;
        int methodCount = 0;
        int classCount = 0;

        //constructor
        public Analyzer(String FilePath) : base(Microsoft.CodeAnalysis.SyntaxWalkerDepth.Trivia)
        {
            this.FilePath = FilePath;
            this.ProjectDirectory = Path.GetDirectoryName(FilePath);

            logger = new Logger.Logger();

        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            methodCount++;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            classCount++;
            base.VisitClassDeclaration(node);
        }

        public override void VisitUsingDirective (UsingDirectiveSyntax node)
        {
            referenceCount++;
        }

        //return FilePath
        public String GetFilePath()
        {
            return this.FilePath;
        }

        //set FilePath
        public void SetFilePath(String FilePath)
        {
            this.FilePath=FilePath;
        }

        //Returns number of references, classes, and methods for each file in the directory
        public int[] Analyze()
        {
            // Find the amount of classes, methods, and references in all cs files
            string[] files = Directory.GetFiles(ProjectDirectory, "*.cs");

            foreach(string file in files)
            {
                string text = File.ReadAllText(file);

                // Create our AST tree and find the root node
                SyntaxTree tree = CSharpSyntaxTree.ParseText(text);
                CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

                Visit(root);

            }

            return new int[] { referenceCount, classCount, methodCount };
        }

        //Analyze if using packages.config, returns true if there is packages.config in project directory
        public bool AnalyzePackageStyle()
        {
            //TODO: add error handling if there is no filepath also if the file is corrupt, wrong format,, etc.
            if (Directory.GetFiles(ProjectDirectory, "packages.config").Length > 0)
            {
                return true;
            }
            return false;
        }
        //Analyze if using old project style, returns true if project file is not in SDK-Style
        public bool AnalyzeProjectStyle()
        {
            //TODO: add error handling if there is no filepath also if the file is corrupt, wrong format,, etc.
            if (!(File.ReadLines(FilePath).First().Equals("<Project Sdk=\"Microsoft.NET.Sdk\">")))
            {
                return true;
            }
            return false;
        }

        

    }
}