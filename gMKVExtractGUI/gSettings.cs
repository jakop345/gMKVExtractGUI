using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace gMKVToolnix
{
    public class gSettings
    {
        private String _MkvToolnixPath = String.Empty;
        public String MkvToolnixPath
        {
            get { return _MkvToolnixPath; }
            set { _MkvToolnixPath = value; }
        }

        private MkvChapterTypes _ChapterType = MkvChapterTypes.XML;
        public MkvChapterTypes ChapterType
        {
            get { return _ChapterType; }
            set { _ChapterType = value; }
        }

        private Boolean _LockedOutputDirectory;
        public Boolean LockedOutputDirectory
        {
            get { return _LockedOutputDirectory; }
            set { _LockedOutputDirectory = value; }
        }

        private String _OutputDirectory;
        public String OutputDirectory
        {
            get { return _OutputDirectory; }
            set { _OutputDirectory = value; }
        }

        private Int32 _WindowPosX;
        public Int32 WindowPosX
        {
            get { return _WindowPosX; }
            set { _WindowPosX = value; }
        }

        private Int32 _WindowPosY;
        public Int32 WindowPosY
        {
            get { return _WindowPosY; }
            set { _WindowPosY = value; }
        }

        private Int32 _WindowSizeWidth;
        public Int32 WindowSizeWidth
        {
            get { return _WindowSizeWidth; }
            set { _WindowSizeWidth = value; }
        }

        private Int32 _WindowSizeHeight;
        public Int32 WindowSizeHeight
        {
            get { return _WindowSizeHeight; }
            set { _WindowSizeHeight = value; }
        }

        private Boolean _JobMode;
        public Boolean JobMode
        {
            get { return _JobMode; }
            set { _JobMode = value; }
        }

        private FormWindowState _WindowState;
        public FormWindowState WindowState
        {
            get { return _WindowState; }
            set { _WindowState = value; }
        }

        private Boolean _ShowPopup = true;
        public Boolean ShowPopup
        {
            get { return _ShowPopup; }
            set { _ShowPopup = value; }
        }

        private static String _SETTINGS_FILE = "gMKVExtractGUI.ini";
        private String _ApplicationPath = String.Empty;

        public gSettings(String appPath)
        {
            // check if user has permission for appPath
            Boolean userHasPermission = false;
            try
            {
                using (FileStream tmp = File.Open(Path.Combine(appPath, _SETTINGS_FILE), FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    tmp.Flush();
                }
                userHasPermission = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                userHasPermission = false;
            }

            // If user doesn't have permissions to the application path,
            // use the current user appdata folder
            if (userHasPermission)
            {
                _ApplicationPath = appPath;
            }
            else
            {
                _ApplicationPath = Application.UserAppDataPath;
            }
        }

        public void Reload()
        {
            if (!File.Exists(Path.Combine(_ApplicationPath, _SETTINGS_FILE)))
            {
                Save();
            }
            else
            {
                using (StreamReader sr = new StreamReader(Path.Combine(_ApplicationPath, _SETTINGS_FILE), Encoding.UTF8))
                {
                    String line = String.Empty;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("MKVToolnix Path:"))
                        {
                            try
                            {
                                _MkvToolnixPath = line.Substring(line.IndexOf(":") + 1);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                _MkvToolnixPath = String.Empty;
                            }
                        }
                        else if (line.StartsWith("Chapter Type:"))
                        {
                            try
                            {
                                _ChapterType = (MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), line.Substring(line.IndexOf(":") + 1));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                _ChapterType = MkvChapterTypes.XML;
                            }
                        }
                        else if (line.StartsWith("Output Directory:"))
                        {
                            try
                            {
                                _OutputDirectory = line.Substring(line.IndexOf(":") + 1);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                _OutputDirectory = String.Empty;
                            }
                        }
                        else if (line.StartsWith("Lock Output Directory:"))
                        {
                            try
                            {
                                _LockedOutputDirectory = Boolean.Parse(line.Substring(line.IndexOf(":") + 1));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                _LockedOutputDirectory = false;
                            }
                        }
                        else if (line.StartsWith("Initial Window Position X:"))
                        {
                            try
                            {
                                _WindowPosX = Int32.Parse(line.Substring(line.IndexOf(":") + 1));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                _WindowPosX = 0;
                            }
                        }
                        else if (line.StartsWith("Initial Window Position Y:"))
                        {
                            try
                            {
                                _WindowPosY = Int32.Parse(line.Substring(line.IndexOf(":") + 1));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                _WindowPosY = 0;
                            }
                        }
                        else if (line.StartsWith("Initial Window Size Width:"))
                        {
                            try
                            {
                                _WindowSizeWidth = Int32.Parse(line.Substring(line.IndexOf(":") + 1));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                _WindowSizeWidth = 640;
                            }
                        }
                        else if (line.StartsWith("Initial Window Size Height:"))
                        {
                            try
                            {
                                _WindowSizeHeight = Int32.Parse(line.Substring(line.IndexOf(":") + 1));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                _WindowSizeHeight = 600;
                            }
                        }
                        else if (line.StartsWith("Job Mode:"))
                        {
                            try
                            {
                                _JobMode = Boolean.Parse(line.Substring(line.IndexOf(":") + 1));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                _JobMode = false;
                            }
                        }
                        else if (line.StartsWith("Window State:"))
                        {
                            try
                            {
                                _WindowState = (FormWindowState)Enum.Parse(typeof(FormWindowState), line.Substring(line.IndexOf(":") + 1), true);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                _WindowSizeHeight = 600;
                            }
                        }
                        else if (line.StartsWith("Show Popup:"))
                        {
                            try
                            {
                                _ShowPopup = Boolean.Parse(line.Substring(line.IndexOf(":") + 1));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                _ShowPopup = true;
                            }
                        }
                    }
                }
            }
        }

        public void Save()
        {
            using (StreamWriter sw = new StreamWriter(Path.Combine(_ApplicationPath, _SETTINGS_FILE), false, Encoding.UTF8))
            {
                sw.WriteLine(String.Format("MKVToolnix Path:{0}", _MkvToolnixPath));
                sw.WriteLine(String.Format("Chapter Type:{0}", _ChapterType));
                sw.WriteLine(String.Format("Output Directory:{0}", _OutputDirectory));
                sw.WriteLine(String.Format("Lock Output Directory:{0}", _LockedOutputDirectory));
                sw.WriteLine(String.Format("Initial Window Position X:{0}", _WindowPosX));
                sw.WriteLine(String.Format("Initial Window Position Y:{0}", _WindowPosY));
                sw.WriteLine(String.Format("Initial Window Size Width:{0}", _WindowSizeWidth));
                sw.WriteLine(String.Format("Initial Window Size Height:{0}", _WindowSizeHeight));
                sw.WriteLine(String.Format("Job Mode:{0}", _JobMode));
                sw.WriteLine(String.Format("Window State:{0}", _WindowState.ToString()));
                sw.WriteLine(String.Format("Show Popup:{0}", _ShowPopup));
            }
        }
    }
}
