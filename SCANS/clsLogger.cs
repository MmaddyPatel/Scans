using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;

namespace SCANS
{
    public static class Logger
    {

        private static FileInfo file = null;

        public static void createFile()
        {
            String filePath = Directory.GetCurrentDirectory() + "\\LogFile\\log" + System.DateTime.Now.ToString().Substring(0, 10).Replace('/', '-') + ".txt";
            file = new FileInfo(filePath);
            if (!file.Exists)
            {
                file.Create();
            }

        }
        public static void writeInLogFile(String message)
        {
            // createFile();
            try
            {
                StreamWriter fileWriter = File.AppendText(Directory.GetCurrentDirectory() + "\\LogFile\\LogFile" + System.DateTime.Now.ToString().Substring(0, 10).Replace('/', '-') + ".txt");
                fileWriter.WriteLine(System.DateTime.Now.ToLongDateString().ToString() + " -- " + System.DateTime.Now.ToLongTimeString().ToString() + " - " + message);
                fileWriter.Close();
            }
            catch (Exception ex)
            {

            }
            
        }


    }
}
