using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Linq;
using System.Runtime.InteropServices;

namespace DataVortex
{
    public static class Network
    {
        private static Queue<long> lastFiveSeconds = new Queue<long>(5);
        private static long previousTotalBytesReceived = 0;
        private static DateTime previousCheckTime;
        public static long totalBytesToDownload = 0;
        public static long totalBytesReceivedAtStart = 0;
        public static bool stopMonitoring = false;

        public static void StartMonitoring(long totalBytes)
        {
            stopMonitoring = false;
            totalBytesToDownload = totalBytes;
            previousCheckTime = DateTime.Now;
            totalBytesReceivedAtStart = GetTotalBytesReceived();
            previousTotalBytesReceived = 0;

            // Start a new task to monitor the progress
            Thread monitorThread = new Thread(() =>
            {
                while (!stopMonitoring)
                {
                    DisplayNetworkSpeed();

                    // Pause for a second before checking again
                    Thread.Sleep(1000);
                }
            });

            monitorThread.Start();

            // Start the download here
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

            // Display the progress bar
            if (IsWindows())
            {
                DisplayProgressBarWindows(percentageDownloaded, totalBytesReceived, totalBytesToDownload, averageBytesPerSecond, estimatedTimeRemaining);
            }
            else
            {
                DisplayProgressBarNonWindows(percentageDownloaded, totalBytesReceived, totalBytesToDownload, averageBytesPerSecond, estimatedTimeRemaining);
            }

            previousTotalBytesReceived = totalBytesReceived;
            previousCheckTime = checkTime;
        }


        private static void DisplayProgressBarWindows(double percentageDownloaded, long totalBytesReceived, long totalBytesToDownload, long averageBytesPerSecond,double estimatedTimeRemaining)
        {
            // Progress bar for Windows
            int progressBarLength = 20; // Set the desired length for the progress bar
            int progressChars = (int)(percentageDownloaded / 100 * progressBarLength);

            Console.SetCursorPosition(0, 13);

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

            Console.Write("|");
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.Write(new string(' ', progressChars));
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(new string(' ', progressBarLength - progressChars));
            Console.Write("| ");

            Console.Write($"{percentageDownloaded.ToString("0.00")}% ");
            Console.Write($" [{totalBytesReceived / (1024.0 * 1024.0):F2}MB/{totalBytesToDownload / (1024.0 * 1024.0):F2}MB] @ {averageBytesPerSecond / (1024.0 * 1024.0):F2}MB/s");

            Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));
        }

        private static void DisplayProgressBarNonWindows(double percentageDownloaded, long totalBytesReceived, long totalBytesToDownload, long averageBytesPerSecond, double estimatedTimeRemaining)
        {
            // Progress bar for non-Windows
            int progressBarLength = 20;
            int progressChars = (int)(percentageDownloaded / 100 * progressBarLength);

            Console.SetCursorPosition(0, 13);
            Console.Write($"Progress: [{new string('#', progressChars)}{new string('-', progressBarLength - progressChars)}] {percentageDownloaded:F2}% ");
            Console.Write($" [{totalBytesReceived / (1024.0 * 1024.0):F2}MB/{totalBytesToDownload / (1024.0 * 1024.0):F2}MB] @ {averageBytesPerSecond / (1024.0 * 1024.0):F2}MB/s");

            Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));
        }

        private static bool IsWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
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
                return "No default network interface found.";
            }
        }
    }
}
