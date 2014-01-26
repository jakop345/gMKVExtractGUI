using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace gMKVToolnix
{
    public class gMKVExtract
    {

        public List<gMKVTrack> GetMKVTracks(String argMKVFile)
        {
            using (Process myProcess = new Process())
            {
                ProcessStartInfo myProcessInfo = new ProcessStartInfo();
                myProcessInfo.FileName = "\"D:\\Program Files (x86)\\MKVToolNix\\mkvextract.exe\"";
                myProcessInfo.Arguments = " -?";
                myProcessInfo.UseShellExecute = false;
                myProcessInfo.RedirectStandardOutput = true;
                myProcessInfo.StandardOutputEncoding = Encoding.UTF8;
                myProcessInfo.RedirectStandardError = true;
                myProcessInfo.StandardErrorEncoding = Encoding.UTF8;
                myProcess.StartInfo = myProcessInfo;
                myProcess.OutputDataReceived += myProcess_OutputDataReceived;

                myProcess.Start();
                myProcess.BeginOutputReadLine();
                myProcess.WaitForExit();
                return new List<gMKVTrack>();
            }
        }

        void myProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine(e.Data);
        }

        public void ExtractMKVTracks(String argMKVFile, List<gMKVTrack> argMKVTracksToExtract, String argOutputDirectory)
        {

        }
    }
}
