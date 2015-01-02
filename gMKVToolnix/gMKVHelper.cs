using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

namespace gMKVToolnix
{
    public class gMKVHelper
    {
        /// <summary>
        /// Gets the mkvmerge GUI executable filename
        /// </summary>
        public static String MKV_MERGE_GUI_FILENAME 
        {
            get { return "mmg.exe"; }
        }

        public static String UnescapeString(String argString)
        {
            return argString.
                Replace(@"\s", " ").
                Replace(@"\2", "\"").
                Replace(@"\c", ":").
                Replace(@"\h", "#").
                Replace(@"\\", @"\");
        }

        public static String EscapeString(String argString)
        {
            return argString.
                Replace(" ", @"\s").
                Replace("\"", @"\2").
                Replace(":", @"\c").
                Replace("#", @"\h").
                Replace(@"\", @"\\");
        }

        /// <summary>
        /// Returns the path from MKVToolnix.
        /// It tries to find it via the registry keys.
        /// If it doesn't find it, it throws an exception.
        /// </summary>
        /// <returns></returns>
        public static String GetMKVToolnixPathViaRegistry()
        {
            RegistryKey regUninstall = null;
            RegistryKey regMkvToolnix = null;
            String valuePath = String.Empty;
            Boolean subKeyFound = false;
            Boolean valueFound = false;

            // First check for Installed MkvToolnix
            // First check Win32 registry
            regUninstall = Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("Microsoft").
                OpenSubKey("Windows").OpenSubKey("CurrentVersion").OpenSubKey("Uninstall");

            foreach (String subKeyName in regUninstall.GetSubKeyNames())
            {
                if (subKeyName.ToLower().Equals("MKVToolNix".ToLower()))
                {
                    subKeyFound = true;
                    regMkvToolnix = regUninstall.OpenSubKey("MKVToolNix");
                    break;
                }
            }

            // if sub key was found, try to get the executable path
            if (subKeyFound)
            {
                foreach (String valueName in regMkvToolnix.GetValueNames())
                {
                    if (valueName.ToLower().Equals("DisplayIcon".ToLower()))
                    {
                        valueFound = true;
                        valuePath = (String)regMkvToolnix.GetValue(valueName);
                        break;
                    }
                }
            }

            // if value was not found, let's Win64 registry
            if (!valueFound)
            {
                subKeyFound = false;

                regUninstall = Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("Wow6432Node").OpenSubKey("Microsoft").
                    OpenSubKey("Windows").OpenSubKey("CurrentVersion").OpenSubKey("Uninstall");

                foreach (String subKeyName in regUninstall.GetSubKeyNames())
                {
                    if (subKeyName.ToLower().Equals("MKVToolNix".ToLower()))
                    {
                        subKeyFound = true;
                        regMkvToolnix = regUninstall.OpenSubKey("MKVToolNix");
                        break;
                    }
                }

                // if sub key was found, try to get the executable path
                if (subKeyFound)
                {
                    foreach (String valueName in regMkvToolnix.GetValueNames())
                    {
                        if (valueName.ToLower().Equals("DisplayIcon".ToLower()))
                        {
                            valueFound = true;
                            valuePath = (String)regMkvToolnix.GetValue(valueName);
                            break;
                        }
                    }
                }
            }

            // if value was still not found, we may have portable installation
            // let's try the CURRENT_USER registry
            if (!valueFound)
            {
                RegistryKey regSoftware = Registry.CurrentUser.OpenSubKey("Software");
                subKeyFound = false;
                foreach (String subKey in regSoftware.GetSubKeyNames())
                {
                    if (subKey.ToLower().Equals("mkvmergeGUI".ToLower()))
                    {
                        subKeyFound = true;
                        regMkvToolnix = regSoftware.OpenSubKey("mkvmergeGUI");
                        break;
                    }
                }

                // if we didn't find the MkvMergeGUI key, all hope is lost
                if (!subKeyFound)
                {
                    throw new Exception("Couldn't find MKVToolNix in your system!\r\nPlease download and install it or provide a manual path!");
                }
                RegistryKey regGui = null;
                Boolean foundGuiKey = false;
                foreach (String subKey in regMkvToolnix.GetSubKeyNames())
                {
                    if (subKey.ToLower().Equals("GUI".ToLower()))
                    {
                        foundGuiKey = true;
                        regGui = regMkvToolnix.OpenSubKey("GUI");
                        break;
                    }
                }
                // if we didn't find the GUI key, all hope is lost
                if (!foundGuiKey)
                {
                    throw new Exception("Found MKVToolNix in your system but not the registry Key GUI!");
                }

                foreach (String valueName in regGui.GetValueNames())
                {
                    if (valueName.ToLower().Equals("mkvmerge_executable".ToLower()))
                    {
                        valueFound = true;
                        valuePath = (String)regGui.GetValue("mkvmerge_executable");
                        break;
                    }
                }
                // if we didn't find the mkvmerge_executable value, all hope is lost
                if (!valueFound)
                {
                    throw new Exception("Found MKVToolNix in your system but not the registry value mkvmerge_executable!");
                }
            }

            // Now that we found a value (otherwise we would not be here, an exception would have been thrown)
            // let's check if it's valid
            if (!File.Exists(valuePath))
            {
                throw new Exception("Found a registry value (" + valuePath + ") for MKVToolNix in your system but it is not valid!");
            }

            // Everything is A-OK! Return the valid Directory value! :)
            return Path.GetDirectoryName(valuePath);
        }

        public static List<gMKVSegment> GetMergedMkvSegmentList(String argMkvToolnixPath, String argInputFile)
        {
            gMKVMerge g = new gMKVMerge(argMkvToolnixPath);
            List<gMKVSegment> segmentList = g.GetMKVSegments(argInputFile);
            gMKVInfo gInfo = new gMKVInfo(argMkvToolnixPath);
            List<gMKVSegment> segmentListInfo = gInfo.GetMKVSegments(argInputFile);
            foreach (gMKVSegment seg in segmentListInfo)
            {
                if (seg is gMKVSegmentInfo)
                {
                    segmentList.Insert(0, seg);
                }
                else if (seg is gMKVTrack)
                {
                    // Update CodecPrivate info from mkvinfo to mkvextract segments
                    if (!String.IsNullOrEmpty(((gMKVTrack)seg).CodecPrivate))
                    {
                        foreach (gMKVSegment seg2 in segmentList)
                        {
                            if (seg2 is gMKVTrack)
                            {
                                if (((gMKVTrack)seg2).TrackID == ((gMKVTrack)seg).TrackID)
                                {
                                    ((gMKVTrack)seg2).CodecPrivate = ((gMKVTrack)seg).CodecPrivate;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            gInfo.FindAndSetDelays(segmentList, argInputFile);

            gInfo = null;
            segmentListInfo = null;

            return segmentList;
        }
    }
}
