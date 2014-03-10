using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

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

        private static String _SETTINGS_FILE = "gMKVExtractGUI.ini";
        private String _ApplicationPath = String.Empty;

        public gSettings(String appPath)
        {
            _ApplicationPath = appPath;
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
                    }
                }
            }
        }

        public void Save()
        {
            using (StreamWriter sw = new StreamWriter(Path.Combine(_ApplicationPath, _SETTINGS_FILE), false, Encoding.UTF8))
            {
                sw.WriteLine(String.Format("MKVToolnix Path:{0}", _MkvToolnixPath));
                sw.Write(String.Format("Chapter Type:{0}", _ChapterType));
            }
        }
    }
}
