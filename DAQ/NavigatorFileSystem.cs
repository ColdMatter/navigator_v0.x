﻿using System;
using System.IO;

namespace DAQ.Environment
{
    public class NavigatorFileSystem : DAQ.Environment.FileSystem
    {
        public NavigatorFileSystem()
        {
            string user = System.Environment.ExpandEnvironmentVariables("%USERPROFILE%");
            string relativePath = AppDomain.CurrentDomain.BaseDirectory;
            string basePath = Directory.GetParent(Directory.GetParent(System.Windows.Forms.Application.ExecutablePath).Parent.FullName).FullName;
            string configPath = basePath + "\\Config\\";
            Paths.Add("configPath", configPath);

            string baseDataPath = basePath + "\\Data\\";
            Paths.Add("baseDataPath", baseDataPath); 

            Paths.Add("mathPath", "C:\\Program Files\\Wolfram Research\\Mathematica\\10.4\\mathkernel.exe");
            Paths.Add("scriptListPath", basePath + "\\Scripts");
            Paths.Add("MOTMasterEXEPath", relativePath);

            Paths.Add("cameraAttributesPath", configPath + "cam0.icd");
            Paths.Add("CameraAttributesPath", configPath + "cam0_remote.icd");
            Paths.Add("daqDLLPath", relativePath + "DAQ.dll");

            string sYear = DateTime.Today.Year.ToString();
            string sMonth = DateTime.Today.Month.ToString().PadLeft(2, '0');
            string sDay = DateTime.Today.Day.ToString().PadLeft(2, '0');
            Paths.Add("DataPath", baseDataPath + sYear + "\\" + sMonth + "\\" + sDay);

            Paths.Add("settingsPath", user + "\\Settings");
            if (!Directory.Exists((string)Paths["DataPath"]))
            {
                Directory.CreateDirectory((string)Paths["DataPath"]);
            }
            Paths.Add("MuquansExePath", configPath + "ukus_dds_comm_gw\\");
            Paths.Add("HardwareClassPath", Directory.GetParent(basePath).FullName + "\\DAQ\\NavigatorHardware.cs");
        }
    }
}
