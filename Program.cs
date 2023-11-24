using System.Text;

class Program
{
    static async Task Main()
    {
        // Définissez un gestionnaire d'exceptions global
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

        string downloadPath = GetDownloadPath();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Lancez la tâche Telegram.TelegramDownload() ou DBExplorer.DBExplorer.Run()
        await Task.Run(() => Telegram.TelegramDownload(downloadPath));
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            GlobalExceptionHandler.HandleException((Exception)e.ExceptionObject);
        };
    }

    private static string GetDownloadPath()
    {
        Console.Write("Entrez le chemin de téléchargement : ");
        string downloadPath = Console.ReadLine();

        if (string.IsNullOrEmpty(downloadPath))
        {
            Console.WriteLine("Le chemin de téléchargement ne peut pas être vide. Utilisation du chemin par défaut.");
            downloadPath = @"D:\Téléchargements\Telegram Desktop";
        }

        return downloadPath;
    }

    // Gestionnaire d'exceptions global
    private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = e.ExceptionObject as Exception;

        if (ex != null)
        {
            // Enregistrez l'exception dans un fichier d'erreurs
            string errorText = $"[{DateTime.Now}] Exception: {ex.Message}\nStack Trace: {ex.StackTrace}\n\n";

            File.AppendAllText("errors.txt", errorText, Encoding.UTF8);

            // Affichez l'erreur dans la console (facultatif)
            Console.WriteLine("Une erreur s'est produite. Veuillez consulter le fichier errors.txt pour plus de détails.");
        }
    }
    public static class GlobalExceptionHandler
    {
        public static void HandleException(Exception exception)
        {
            string errorLogPath = "errors.txt";
            File.AppendAllText(errorLogPath, $"[{DateTime.Now}] {exception.ToString()}{Environment.NewLine}");
        }
    }
}
