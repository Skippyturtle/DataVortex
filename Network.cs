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
                    DisplayNetworkSpeed(null);

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
        public static void DisplayNetworkSpeed(object state)
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

            // Déplace le curseur au début de la ligne
            Console.SetCursorPosition(0, Console.CursorTop);

            Console.Write("Vitesse de débit : " + averageKiloBytesPerSecond.ToString("0.00") + " Ko/s, ");
            Console.Write("Téléchargement : " + percentageDownloaded.ToString("0.00") + " %, ");
            Console.Write("Temps restant estimé : " + estimatedTimeRemaining.ToString("0.00") + " secondes");

            // Efface le reste de la ligne au cas où elle serait plus longue que le texte actuel
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
