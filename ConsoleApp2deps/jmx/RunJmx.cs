using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace JMeterRunner
{
    public class Program
    {
        static void Main(string[] args)
        {
            JMeterManager jmeterManager = new JMeterManager();
            jmeterManager.SelectThreadGroup();
            jmeterManager.RunJMeterTest();

            Console.ReadKey();
        }
    }

    public class JMeterManager
    {
        private string JmxFilePath = @"C:\Users\md003142\Desktop\jmx\Test-i Ornek Calisma.jmx";
        public string ResultFilePath = @"C:\Users\md003142\Desktop\jmx\results.jtl";
        private string JmeterPath = @"C:\Users\md003142\Desktop\apache-jmeter-5.4\bin\jmeter.bat";

        public void SelectThreadGroup()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(JmxFilePath);
            doc.PreserveWhitespace = true;

            XmlNodeList threadGroups = doc.SelectNodes("(//ThreadGroup[@testclass and @guiclass and @testname])");

            if (threadGroups == null) return;

            Console.WriteLine("JMX dosyasındaki Thread Groupları listele:");
            foreach (XmlNode threadGroup in threadGroups)
            {
                Console.WriteLine($"{threadGroup.Attributes["testname"].Value} (enabled: {threadGroup.Attributes["enabled"].Value})");
            }

            Console.WriteLine("\nTH Degeri Gir: ");
            string selectedThreadGroup = Console.ReadLine();

            string xpath = $"(//ThreadGroup[@testclass and @guiclass and @testname='{selectedThreadGroup}'])[1]";
            XmlNode threadGroupNode = doc.SelectSingleNode(xpath);

            if (threadGroupNode == null)
            {
                Console.WriteLine("xpath yok ");
                return;
            }

            Console.WriteLine($"\nSeçilen Thread Group'un Mevcut Durumu: {threadGroupNode.Attributes["enabled"].Value}");

            Console.WriteLine("TH Statü Gir: ");
            string enabledStatus = Console.ReadLine();

            threadGroupNode.Attributes["enabled"].Value = enabledStatus;
            doc.Save(JmxFilePath);
            Console.WriteLine($"Seçilen Thread Group'un Yeni Durumu: {threadGroupNode.Attributes["enabled"].Value}");
        }

        public void RunJMeterTest()
        {
            try
            {
                // Eğer resultFilePath varsa sil
                if (File.Exists(ResultFilePath))
                {
                    File.Delete(ResultFilePath);
                }

                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = JmeterPath,
                        Arguments = $"-n -t \"{JmxFilePath}\" -l \"{ResultFilePath}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();

                // Burada JMeter'ın bitip bitmediğini kontrol ediyoruz
                while (!process.HasExited)
                {
                    System.Threading.Thread.Sleep(1000); // 1 saniye bekle
                }

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("\nJMeter testi başarıyla tamamlandı.");
                    if (File.Exists(ResultFilePath))
                    {
                        Console.WriteLine("Sonuç dosyası başarıyla oluşturuldu.");
                        ParseJTLResults(ResultFilePath);
                    }
                    else
                    {
                        Console.WriteLine("Sonuç dosyası oluşturulmadı. JMeter testinde bir sorun olabilir.");
                    }
                }
                else
                {
                    Console.WriteLine($"\nJMeter testi hata koduyla sonlandı: {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
            }
        }


        private void ParseJTLResults(string resultFilePath)
        {
            var lines = File.ReadAllLines(resultFilePath).Skip(1);
            foreach (var line in lines)
            {
                var columns = line.Split(',');
                if (columns.Length >= 5)
                {
                    Console.WriteLine($"Request Name: {columns[2]}");
                    Console.WriteLine($"Time Stamp: {columns[0]}");
                    Console.WriteLine($"Response Code: {columns[1]}");
                    Console.WriteLine($"Response Message: {columns[3]}");
                    Console.WriteLine($"Successful: {columns[4]}");
                    Console.WriteLine("-------------------------------");
                }
            }
        }





    }
}
