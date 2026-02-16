using System;
using System.IO;
using System.Text;

namespace SolutionToTextExporter
{
    public class SolutionExporter
    {
        private readonly string _solutionFolder;
        private readonly int _chunkSizeBytes;

        public SolutionExporter(string solutionFolder, int chunkSizeBytes = 1 * 1024 * 1024)
        {
            _solutionFolder = solutionFolder ?? throw new ArgumentNullException(nameof(solutionFolder));
            _chunkSizeBytes = chunkSizeBytes;
        }

        public void Export()
        {
            string outputFolder = Path.Combine(_solutionFolder, "SolutionTextChunks");
            Directory.CreateDirectory(outputFolder);

            StringBuilder buffer = new StringBuilder();
            int chunkIndex = 0;

            // Endelser som är intressanta för AI/felsökning
            string[] extensions = { ".cs", ".xaml", ".csproj", ".config", ".json" };

            // Mappar vi vill ignorera
            string[] excludedDirs = { "bin", "obj", ".vs", "packages" };

            foreach (string file in Directory.EnumerateFiles(_solutionFolder, "*.*", SearchOption.AllDirectories))
            {
                // Hoppa över filer i exkluderade mappar
                if (Array.Exists(excludedDirs, dir => file.Contains(Path.DirectorySeparatorChar + dir + Path.DirectorySeparatorChar)))
                    continue;

                // Hoppa över filer som inte är av rätt typ
                if (!Array.Exists(extensions, ext => ext.Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase)))
                    continue;

                buffer.AppendLine($"\n==== {file} ====\n");
                buffer.AppendLine(File.ReadAllText(file));

                // Skriv till fil om buffer > chunkSize
                if (Encoding.UTF8.GetByteCount(buffer.ToString()) >= _chunkSizeBytes)
                {
                    string chunkPath = Path.Combine(outputFolder, $"chunk_{chunkIndex}.txt");
                    File.WriteAllText(chunkPath, buffer.ToString());
                    Console.WriteLine($"Skriver {chunkPath}");
                    buffer.Clear();
                    chunkIndex++;
                }
            }

            // Skriv sista chunken
            if (buffer.Length > 0)
            {
                string chunkPath = Path.Combine(outputFolder, $"chunk_{chunkIndex}.txt");
                File.WriteAllText(chunkPath, buffer.ToString());
                Console.WriteLine($"Skriver {chunkPath}");
            }

            Console.WriteLine("Klar!");
        }
    }
}
