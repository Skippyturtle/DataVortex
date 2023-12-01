using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace DataVortex
{
    public static class Network
    {
        private static Queue<long> lastFiveSeconds = new Queue<long>(5);
        private static long previousTotalBytesReceived = 0;
        private static DateTime previousCheckTime;
        public static long totalBytesToDownload = 0; // Ajouté
        public static long totalBytesReceivedAtStart = 0;
        public static bool stopMonitoring = false;

        public static void StartMonitoring(long totalBytes)
        {
            stopMonitoring = false;
            totalBytesToDownload = totalBytes;
            previousCheckTime = DateTime.Now;
            totalBytesReceivedAtStart = GetTotalBytesReceived();
            previousTotalBytesReceived = 0;

            // Démarrer une nouvelle tâche pour la surveillance de la progression
            Task.Run(() =>
            {
                while (!stopMonitoring)
                {
                    DisplayNetworkSpeed();

                    // Pause pendant une seconde avant de vérifier à nouveau
                    Thread.Sleep(1000);
                }
            });

            // Démarrer le téléchargement ici
            // ...
        }

        public static void StopMonitoring()
        {
            stopMonitoring = true;
        }
        public static void DisplayNetworkSpeed()
        {
            long totalBytesReceived = GetTotalBytesReceived() - totalBytesReceivedAtStart;
            DateTime checkTime = DateTime.Now;

            long bytesReceived = totalBytesReceived - previousTotalBytesReceived;
            double secondsPassed = (checkTime - previousCheckTime).TotalSeconds;

            long bytesPerSecond = (long)(bytesReceived / secondsPassed);
            lastFiveSeconds.Enqueue(bytesPerSecond);

            if (lastFiveSeconds.Count > 5)
            {
                lastFiveSeconds.Dequeue();
            }

            long averageBytesPerSecond = (long)lastFiveSeconds.Average();
            double averageKiloBytesPerSecond = averageBytesPerSecond / 1024.0;

            double percentageDownloaded = (double)totalBytesReceived / totalBytesToDownload * 100;
            double estimatedTimeRemaining = (totalBytesToDownload - totalBytesReceived) / averageBytesPerSecond;

            // Progress bar
            int progressBarLength = 20; // Set the desired length for the progress bar
            int progressChars = (int)(percentageDownloaded / 100 * progressBarLength);

            Console.SetCursorPosition(0,2);

            if (estimatedTimeRemaining < 60)
            {
                Console.Write($"{estimatedTimeRemaining.ToString("0")} secs ");
            }
            else
            {
                double minutesRemaining = estimatedTimeRemaining / 60;

                if (minutesRemaining < 2)
                {
                    Console.Write($"{minutesRemaining.ToString("0")} min ");
                }
                else
                {
                    Console.Write($"{minutesRemaining.ToString("0")} mins ");
                }
            }


            // Display the progress bar using System.Console.ProgressBar
            Console.Write("|");
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.Write(new string(' ', progressChars));
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(new string(' ', progressBarLength - progressChars));
            Console.Write("| "); // j'ai ajouté un espace pour que le pourcentage soit pas collé

            Console.Write($"{percentageDownloaded.ToString("0.00")}% ");
            // Display additional information like [26.3MB/93.8MB] @ 5.17MB/s
            Console.Write($" [{totalBytesReceived / (1024.0 * 1024.0):F2}MB/{totalBytesToDownload / (1024.0 * 1024.0):F2}MB] @ {averageBytesPerSecond / (1024.0 * 1024.0):F2}MB/s");

            // Clear the rest of the line in case it's longer than the current text
            Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));

            previousTotalBytesReceived = totalBytesReceived;
            previousCheckTime = checkTime;
        }




        private static long GetTotalBytesReceived()
        {
            string interfaceName = GetDefaultInterface();
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var networkInterface in interfaces)
            {
                if (networkInterface.Description == interfaceName)
                {
                    IPv4InterfaceStatistics stats = networkInterface.GetIPv4Statistics();
                    return stats.BytesReceived;
                }
            }

            return 0;
        }

        public static string GetDefaultInterface()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            NetworkInterface defaultInterface = interfaces.FirstOrDefault(netInterface => netInterface.OperationalStatus == OperationalStatus.Up);

            if (defaultInterface != null)
            {
                return defaultInterface.Description;
            }
            else
            {
                return "Aucune interface réseau par défaut trouvée.";
            }
        }
    }
}
