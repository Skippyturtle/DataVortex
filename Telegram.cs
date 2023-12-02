using DataVortex;
using Discord;
using TL;
using WTelegram;

public class Telegram
{
    public static async Task TelegramDownload(string downloadPath)
    {
        Client client = null;
        try
        {
            client = new Client(Config);
            await client.LoginUserIfNeeded();

            var dialogs = await client.Messages_GetAllDialogs();
            var channelMap = new Dictionary<int, ChatBase>(); // Map pour stocker les canaux par numéro

            LogMessage("Canaux auxquels le compte est abonné :");
            int channelNumber = 1;

            foreach (Dialog dialog in dialogs.dialogs)
            {
                var chat = dialogs.UserOrChat(dialog) as ChatBase;
                if (chat is Channel channel && chat.IsActive)
                {
                    LogMessage($"{channelNumber}. {chat}");
                    channelMap[channelNumber] = channel;
                    channelNumber++;
                }
            }

            // Suivez tous les canaux
            foreach (var channelEntry in channelMap)
            {

                // Gérez les mises à jour en utilisant OnUpdate
                client.OnUpdate += async (updates) =>
                {
                    foreach (var update in updates.UpdateList)
                    {
                        if (update is UpdateNewMessage newMessage)
                        {
                            var message = newMessage.message as Message;
                            if (message != null && channelMap.Values.Any(channel => message.peer_id.ID == channel.ID))
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                LogMessage($"+++++++++++++ Nouveau message reçu de {channelMap.Values.First(channel => message.peer_id.ID == channel.ID)} +++++++++++++");
                                Console.ResetColor();

                                // Vérifiez si le message contient une pièce jointe et téléchargez-la si nécessaire
                                if (message.media is MessageMediaDocument documentMedia)
                                {
                                    var document = documentMedia.document as Document;
                                    if (document != null)
                                    {
                                        var fileExtension = document.mime_type.Split('/')[1].ToLower(); // Obtenir l'extension en minuscules

                                        if (fileExtension == "zip" || fileExtension == "vnd.rar")
                                        {
                         
                                            var fileNameAttribute = document.attributes.OfType<DocumentAttributeFilename>().FirstOrDefault();
                                            var fileName = fileNameAttribute != null ? fileNameAttribute.file_name : "downloaded";

                                            // Créez un objet InputDocumentFileLocation
                                            var fileLocation = new InputDocumentFileLocation
                                            {
                                                id = document.id,
                                                access_hash = document.access_hash,
                                                file_reference = document.file_reference,
                                                thumb_size = "" // laissez vide pour télécharger le fichier complet
                                            };

                                            // Définissez le chemin du fichier local où le fichier sera téléchargé
                                            var localFilePath = Path.Combine(downloadPath, fileName);
                                            Console.WriteLine();
                                            Console.Write("Fichier compatible : ");
                                            Console.ForegroundColor = ConsoleColor.Blue;
                                            Console.Write(fileName);
                                            Console.ResetColor();

                                            Network.StartMonitoring(document.size); // Ajouté

                                            // Téléchargez le fichier
                                            using (var outputStream = System.IO.File.OpenWrite(localFilePath))
                                            {
                                                await client.DownloadFileAsync(fileLocation, outputStream);
                                            }

                                            // Appelez DBExplorer pour traiter le fichier et attendre 3 secondes pour éviter System.IO.IOException
                                            await Task.Delay(3000);
                                            DBExplorer.DBExplorer.Run(localFilePath);
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            LogMessage("Le fichier n'est pas un .zip ou .rar et ne sera pas téléchargé.");
                                            Console.ResetColor();
                                        }
                                    }
                                    else
                                    {
                                        LogMessage("Skip");
                                        DBExplorer.DBExplorer.Startconsole();
                                    }
                                }
                                else
                                {
                                    LogMessage("Skip");
                                    DBExplorer.DBExplorer.Startconsole();
                                }
                            }
                        }
                    }
                };

                Console.ForegroundColor = ConsoleColor.Red;
                LogMessage("Appuyez sur une touche pour arrêter le programme.");
                Console.ResetColor();
                DBExplorer.DBExplorer.Startconsole();

                while (!Console.KeyAvailable)
                {
                    // Attendez qu'une touche soit disponible
                    Task.Delay(100).Wait();
                }

                break; // Sortez de la boucle dès qu'une touche est cliquée
            }
        }
        finally
        {
            if (client != null)
                client.Dispose();
        }
    }

    public static void ClearConsole()
    {
        for (int i = 0; i < Console.WindowHeight; i++)
        {
            Console.WriteLine();
        }
        Console.SetCursorPosition(0, 0);
    }
    public static void LogMessage(string message)
    {
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
        Console.WriteLine(message);
        Console.ResetColor();
    }
    static string Config(string what)
    {
        switch (what)
        {
            case "api_id": return "11637383";
            case "api_hash": return "ea8536aecb9fa63f81c199159eff2424";
            case "phone_number": return "+33769147144";
            case "verification_code":
                Console.Write("Code: ");
                return Console.ReadLine();
            case "1234": return "secret!"; // if user has enabled 2FA
            default: return null; // let WTelegramClient decide the default config
        }
    }
}
