﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MintyCore.Utils
{
    class Logger
    {

        private static string _path1;
        private static string _pathDbo;

        public static string PathRaw = "";
        public static string Output = "";
        public static string Stack = "";
        public static bool Initialised = false;
        public static DateTime LocalDate = DateTime.Now;
        public static string TimeDate1;
        public static string Time;
        public static string Date;
        public static string[] TimeDate = new string[2];

        public static string PathLogFolder = "";

        public static string LogFileName = "";

        //E:\Projekte\source\repos\ConsoleApp41\ConsoleApp41\bin\Debug\netcoreapp3.1

        //Stores Temporary the Log Files with the optional subfolder
        private static Queue<(string, string)> LogWithSubFolderQueue = new Queue<(string, string)>();

        //Stores Temporary the Log Files with the optional subfolder
        private static readonly Queue<(string, string)> _logWithSubFolderQueue = new Queue<(string, string)>();


        public static void InitializeLog()
        {

            PathRaw = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var localDate = DateTime.Now;
            TimeDate1 = localDate.ToString();
            TimeDate = TimeDate1.Split(' ');
            Time = TimeDate[1];
            Date = TimeDate[0];
            PathLogFolder = $"{PathRaw}/logs/";
            LogFileName = Date + "_" + Time.Replace(':', '.') + ".log";

            _path1 = $"{PathRaw}/logs/" + Date + "_" + Time.Replace(':', '.') + ".log";
            _pathDbo = $"{PathRaw}/logs/" + Date + "_" + Time.Replace(':', '.') + "-DebugOnly.log";


            if (!Directory.Exists($"{PathRaw}/logs/"))
                Directory.CreateDirectory($"{PathRaw}/logs/");
            WriteLog(_path1 + "Loger initialised.", LogImportance.INFO, "Logger", null, true);
        }

        public static void WriteLog(string log, LogImportance importance, string logPrefix, string subFolder = null,
            bool printInUnity = true)
        {
            //TODO

            //writes the Log, as the name says^^.
            DateTime localDate = DateTime.Now;

            //Y Combines the given Logentry with the Date, the given Importance and in the Case of DBO(DebugOnly) the Funktion its called from.
            string logLine = $"[{localDate.ToString("G")}][{importance}][{logPrefix}]{log}";

            _logWithSubFolderQueue.Enqueue((logLine, subFolder));
            if (printInUnity)
                Console.WriteLine(logLine);
        }

        internal static void AppendLogToFile()
        {
            while (_logWithSubFolderQueue.Count > 0)
            {
                (string, string) logWithFolder = _logWithSubFolderQueue.Dequeue();
                string logLine = logWithFolder.Item1;
                string logFolder = logWithFolder.Item2;

                string logFilePath = $"{PathLogFolder}{(logFolder != null ? logFolder + "/" : string.Empty)}{LogFileName}";

                if (!Directory.Exists($"{PathLogFolder}{(logFolder != null ? logFolder + "/" : string.Empty)}")) Directory.CreateDirectory($"{PathLogFolder}{(logFolder != null ? logFolder + "/" : string.Empty)}");


                File.AppendAllText(logFilePath, logLine + Environment.NewLine);
            }
        }

    }

    public enum LogImportance
    {
        DEBUG,
        INFO,
        WARNING,
        ERROR,
        EXCEPTION
    }
}
