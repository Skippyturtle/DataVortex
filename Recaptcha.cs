// ReCaptchaV2Example.cs
using System;
using System.IO;
using System.Linq;
using TwoCaptcha.Captcha;

namespace TwoCaptcha.Examples
{
    public class ReCaptchaV2Example
    {
        private static string _captchaCode;

        public static string CaptchaCode
        {
            get { return _captchaCode; }
        }

        public static void SolveCaptcha()
        {
            // Lire la clé API depuis le fichier
            string apiKey = ReadApiKeyFromFile("api_key.txt");

            TwoCaptcha solver = new TwoCaptcha(apiKey);

            ReCaptcha captcha = new ReCaptcha();
            captcha.SetSiteKey("6LdWB0caAAAAAKfVe3he0FqXQXOepICF-5aZh_rQ");
            captcha.SetUrl("https://passculture.app/connexion?preventCancellation=true");

            try
            {
                solver.Solve(captcha).Wait();
                _captchaCode = captcha.Code; // Stocker la valeur du code
                Console.WriteLine("Captcha résolu : " + _captchaCode);
            }
            catch (AggregateException e)
            {
                Console.WriteLine("Une erreur s'est produite : " + e.InnerExceptions.First().Message);
            }
        }

        private static string ReadApiKeyFromFile(string filePath)
        {
            try
            {
                // Lire le contenu du fichier et retourner la clé API
                return File.ReadAllText(filePath).Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la lecture de la clé API à partir du fichier : " + ex.Message);
                return null;
            }
        }
    }
}
