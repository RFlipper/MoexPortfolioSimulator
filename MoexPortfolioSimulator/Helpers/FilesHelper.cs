namespace MoexPortfolioSimulator.Helpers
{
    public class FilesHelper
    {
        private static readonly string resourceFolderName = "resources";

        public static void AppendToFile(string fileName, string fileData)
        {
            System.IO.Directory.CreateDirectory(resourceFolderName);
            System.IO.File.AppendAllText($"{resourceFolderName}\\{fileName}.txt", fileData);
        }
        
        public static void SaveToFile(string fileName, string fileData)
        {
            System.IO.Directory.CreateDirectory(resourceFolderName);
            System.IO.File.WriteAllText($"{resourceFolderName}\\{fileName}.txt", fileData);
        }
        
        public static string ReadFromFile(string fileName)
        {
            System.IO.Directory.CreateDirectory(resourceFolderName);
            return System.IO.File.ReadAllText($"{resourceFolderName}\\{fileName}.txt");
        }
        
        public static bool IsFileExists(string fileName)
        {
            return System.IO.File.Exists($"{resourceFolderName}\\{fileName}.txt");
        }
    }
}