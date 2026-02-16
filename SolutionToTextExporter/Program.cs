using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SolutionToTextExporter
{
    class Program
    {
        static void Main()
        {
            // Läs JSON-fil
            string jsonText = File.ReadAllText("appsettings.json");
            var settings = JsonSerializer.Deserialize<AppSettings>(jsonText);

            if (settings == null || string.IsNullOrWhiteSpace(settings.SolutionFolder))
            {
                Console.WriteLine("Ingen SolutionFolder angiven i appsettings.json");
                return;
            }

            var exporter = new SolutionExporter(settings.SolutionFolder);
            exporter.Export();
        }
    }
}
