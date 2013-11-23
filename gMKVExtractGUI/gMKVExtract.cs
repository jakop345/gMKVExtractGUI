using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace gMKVExtractGUI
{
    public class gMKVExtract
    {
        public String GetMKVToolnixPath()
        {
            RegistryKey regSoftware = Registry.CurrentUser.OpenSubKey("Software");
            Boolean foundMkvMergeGuiKey = false;
            foreach (String subKey in regSoftware.GetSubKeyNames())
            {
                if (subKey == "mkvmergeGUI")
                {
                    foundMkvMergeGuiKey = true;
                    break;
                }
            }
            if (!foundMkvMergeGuiKey)
            {
                throw new Exception("Couldn't find MkvMergeGUI in your system!\r\nPlease download and install it or provide a manual path!");
            }
            RegistryKey regMkvMergeGui = regSoftware.OpenSubKey("mkvmergeGUI");
            Boolean foundGuiKey = false;
            foreach (String subKey in regMkvMergeGui.GetSubKeyNames())
            {
                if (subKey == "GUI")
                {
                    foundGuiKey = true;
                    break;
                }
            }
            if (!foundGuiKey)
            {
                throw new Exception("Found MkvMergeGUI in your system but not the registry Key GUI!\r\nPlease download and reinstall or provide a manual path!");
            }
            RegistryKey regGui = regMkvMergeGui.OpenSubKey("GUI");
            Boolean foundExecutableValue = false;
            foreach (String regValue in regGui.GetValueNames())
            {
                if (regValue == "mkvmerge_executable")
                {
                    foundExecutableValue = true;
                    break;
                }
            }
            if (!foundExecutableValue)
            {
                throw new Exception("Found MkvMergeGUI in your system but not the registry value mkvmerge_executable!\r\nPlease download and reinstall or provide a manual path!");
            }
            return (String)regGui.GetValue("mkvmerge_executable");
        }

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
