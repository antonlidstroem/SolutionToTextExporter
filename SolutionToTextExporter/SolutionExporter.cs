using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SolutionToTextExporter
{
    public class SolutionExporter
    {
        private readonly string _solutionFolder;
        private const int MaxChunkSizeBytes = 40 * 1024; // 40 KB (AI-safe)

        public SolutionExporter(string solutionFolder)
        {
            _solutionFolder = solutionFolder
                ?? throw new ArgumentNullException(nameof(solutionFolder));
        }

        public void Export()
        {
            string outputFolder = Path.Combine(_solutionFolder, "SolutionTextChunks");
            Directory.CreateDirectory(outputFolder);

            int chunkIndex = 0;
            var buffer = new StringBuilder();
            int bufferBytes = 0;

            var files = Directory
                .EnumerateFiles(_solutionFolder, "*.*", SearchOption.AllDirectories)
                .Where(IsRelevantFile)
                .OrderBy(f => f)
                .ToList();

            foreach (var file in files)
            {
                string content;
                try
                {
                    content = File.ReadAllText(file);
                }
                catch
                {
                    continue;
                }

                content = CleanForAI(content);

                if (string.IsNullOrWhiteSpace(content))
                    continue;

                string relative = Path.GetRelativePath(_solutionFolder, file);
                string block = $"==== {relative} ====\n{content}\n";

                int blockBytes = Encoding.UTF8.GetByteCount(block);

                if (blockBytes > MaxChunkSizeBytes)
                {
                    Flush(ref buffer, ref bufferBytes, outputFolder, ref chunkIndex);
                    SplitLargeBlock(block, outputFolder, ref chunkIndex);
                    continue;
                }

                if (bufferBytes + blockBytes > MaxChunkSizeBytes)
                {
                    Flush(ref buffer, ref bufferBytes, outputFolder, ref chunkIndex);
                }

                buffer.Append(block);
                bufferBytes += blockBytes;
            }

            Flush(ref buffer, ref bufferBytes, outputFolder, ref chunkIndex);

            Console.WriteLine($"Klar. Skapade {chunkIndex} chunk-filer.");
        }

        private string CleanForAI(string content)
        {
            // Ta bort block-kommentarer
            content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);

            // Ta bort enkelradskommentarer
            content = Regex.Replace(content, @"//.*", "");

            // Ta bort extra tomrader
            content = Regex.Replace(content, @"^\s*$\n|\r", "", RegexOptions.Multiline);

            return content.Trim();
        }

        private bool IsRelevantFile(string file)
        {
            string[] allowedExtensions = {
                ".cs", ".java", ".kt", ".py",
                ".vue", ".js", ".ts", ".jsx", ".tsx",
                ".html", ".css", ".scss",
                ".csproj", ".json", ".yml", ".yaml"
            };

            string[] excludedDirs = {
                "bin", "obj", "node_modules", "dist", "build",
                "public", "packages", "venv", "__pycache__",
                ".git", ".vs", ".idea", ".gradle", "target",
                "coverage", "Migrations"
            };

            var relative = Path.GetRelativePath(_solutionFolder, file);
            var parts = relative.Split(Path.DirectorySeparatorChar);

            // Ignorera kataloger som matchar excludedDirs
            if (parts.Any(p => excludedDirs.Contains(p, StringComparer.OrdinalIgnoreCase)))
                return false;

            // Filtypkontroll
            if (!allowedExtensions.Any(ext =>
                    file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                return false;

            // Ignorera lås- och backupfiler
            if (relative.Contains("lock", StringComparison.OrdinalIgnoreCase))
                return false;

            // Ignorera designer-filer och EF snapshot/migrations
            string fileName = Path.GetFileName(file);
            if (Regex.IsMatch(fileName, @"(ModelSnapshot|Migration\.Designer\.cs|.*designer\.cs|BudgetDbContextModelSnapshot\.cs)$",
                              RegexOptions.IgnoreCase))
                return false;

            return true;
        }

        private void Flush(ref StringBuilder buffer,
                           ref int bufferBytes,
                           string folder,
                           ref int index)
        {
            if (bufferBytes == 0)
                return;

            string path = Path.Combine(folder, $"chunk_{index}.txt");
            File.WriteAllText(path, buffer.ToString());

            buffer.Clear();
            bufferBytes = 0;
            index++;
        }

        private void SplitLargeBlock(string block,
                                     string folder,
                                     ref int index)
        {
            int start = 0;

            while (start < block.Length)
            {
                int length = Math.Min(block.Length - start, MaxChunkSizeBytes);
                string chunk = block.Substring(start, length);

                string path = Path.Combine(folder, $"chunk_{index}.txt");
                File.WriteAllText(path, chunk);

                start += length;
                index++;
            }
        }
    }
}
