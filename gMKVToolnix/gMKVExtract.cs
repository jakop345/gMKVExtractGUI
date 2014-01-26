using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace gMKVToolnix
{
    public class gMKVExtract
    {
        private Int32 _eventCounter = 0;
        private String _MKVToolnixPath = String.Empty;
        private String _MKVExtractFilename = String.Empty;
        private StringBuilder _MKVExtractOutput = new StringBuilder();

        public gMKVExtract(String mkvToonlixPath)
        {
            _MKVToolnixPath = mkvToonlixPath;
            _MKVExtractFilename = Path.Combine(_MKVToolnixPath, "mkvextract.exe");
        }

        public void ExtractMKVTracks(String argMKVFile, List<gMKVSegment> argMKVSegmentsToExtract, String argOutputDirectory)
        {
            using (Process myProcess = new Process())
            {
                //List<OptionValue> optionList = new List<OptionValue>();
                //optionList.Add(new OptionValue(MkvInfoOptions.track_info, String.Empty));
                //optionList.Add(new OptionValue(MkvInfoOptions.summary, String.Empty));
                //optionList.Add(new OptionValue(MkvInfoOptions.command_line_charset, "\"UFT-8\""));
                //optionList.Add(new OptionValue(MkvInfoOptions.output_charset, "\"UFT-8\""));

                ProcessStartInfo myProcessInfo = new ProcessStartInfo();
                myProcessInfo.FileName = _MKVExtractFilename;
                myProcessInfo.Arguments = " -?";
                //myProcessInfo.Arguments = ConvertOptionValueListToString(optionList) + " " + argMKVFile;
                //myProcessInfo.Arguments = String.Format("\"{0}\"", argMKVFile);
                myProcessInfo.UseShellExecute = false;
                myProcessInfo.RedirectStandardOutput = true;
                myProcessInfo.StandardOutputEncoding = Encoding.UTF8;
                myProcessInfo.RedirectStandardError = true;
                myProcessInfo.StandardErrorEncoding = Encoding.UTF8;
                myProcessInfo.CreateNoWindow = true;
                myProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
                myProcess.StartInfo = myProcessInfo;
                myProcess.OutputDataReceived += myProcess_OutputDataReceived;

                Debug.WriteLine(myProcessInfo.Arguments);
                // Start the mkvinfo process
                myProcess.Start();
                // Start reading the output
                myProcess.BeginOutputReadLine();
                // Wait for the process to exit
                myProcess.WaitForExit();
                // unregister the event
                myProcess.OutputDataReceived -= myProcess_OutputDataReceived;

                // Debug write the exit code
                Debug.WriteLine("Exit code: " + myProcess.ExitCode);

                // Check the exit code
                if (myProcess.ExitCode > 0)
                {
                    // something went wrong!
                    throw new Exception(String.Format("Mkvmerge exited with error code {0}!", myProcess.ExitCode));
                }
            }

        }

        void myProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                if (e.Data.Trim().Length > 0)
                {
                    // add the line to the output stringbuilder
                    _MKVExtractOutput.AppendLine(e.Data.Trim());
                    // debug write the output line
                    Debug.WriteLine(_eventCounter + " " + e.Data.Trim());
                    _eventCounter++;
                    // log the output
                    gMKVLogger.Log(e.Data);
                }
            }
        }

    }
}
