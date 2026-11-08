using System.IO;

namespace LastTide
{
    /// <summary>Reads and writes the save file. Writes go to a temporary file first, so a crash can't corrupt the save.</summary>
    public static class SaveStore
    {
        public static void Write(string path, string contents)
        {
            string folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string temporary = path + ".tmp";
            File.WriteAllText(temporary, contents);
            if (File.Exists(path))
            {
                File.Replace(temporary, path, null);
            }
            else
            {
                File.Move(temporary, path);
            }
        }

        /// <summary>The file's text, or null when there is no save.</summary>
        public static string Read(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public static void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (File.Exists(path + ".tmp"))
            {
                File.Delete(path + ".tmp");
            }
        }
    }
}
