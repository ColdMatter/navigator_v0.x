﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAQ
{
    public class MMConfig
    {
        public MMConfig()
        {

        }

        public MMConfig(bool camera, bool translation, bool reporter, bool dbg)
        {
            CameraUsed = camera;
            TranslationStageUsed = translation;
            ReporterUsed = reporter;
            Debug = dbg;
            DigitalPatternClockFrequency = 100000; //default value
            AnalogPatternClockFrequency = 100000; //default value
            ExternalFilePattern = null;
            hsdioCard = false;
            useAI = false;
            useMuquans = false;//
            useMMScripts = true;
            useMSquared = false;
        }

        private bool debug;
        public bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }

        public bool PlexalMachine { get { return (Debug || ((string)System.Environment.GetEnvironmentVariables()["COMPUTERNAME"] == "DESKTOP-IHEEQUU")); } }

        private bool cameraUsed;
        public bool CameraUsed
        {
            get { return cameraUsed; }
            set { cameraUsed = value; }
        }

        private bool translationStageUsed;
        public bool TranslationStageUsed
        {
            get { return translationStageUsed; }
            set { translationStageUsed = value; }
        }

        private bool reporterUsed;
        public bool ReporterUsed
        {
            get { return reporterUsed; }
            set { reporterUsed = value; }
        }

        private int digitalClockFreq;
        public int DigitalPatternClockFrequency
        {
            get { return digitalClockFreq; }
            set { digitalClockFreq = value; }
        }

        private int analogClockFreq;
        public int AnalogPatternClockFrequency
        {
            get { return analogClockFreq; }
            set { analogClockFreq = value; }
        }

        private string externalFilePattern;
        // Filename pattern for files generated by an external program to be zipped up along with the MOTMaster files, e.g. "*.tif" for image files generated by an external camera control program
        public string ExternalFilePattern
        {
            get { return externalFilePattern; }
            set { externalFilePattern = value; }
        }

        private bool hsdioCard;
        //Used to flag if an NI-HSDIO card is used to generate digital patterns
        public bool HSDIOCard
        {
            get { return hsdioCard; }
            set { hsdioCard = value; }
        }
        //Used to flag analog inputs
        private bool useAI;
        public bool UseAI
        {
            get { return useAI; }
            set { useAI = value; }
        }

        //Used to flag the use of triggered serial commands
        private bool useMuquans;
        public bool UseMuquans
        {
            get { return useMuquans; }
            set { useMuquans = value; }
        }

        private bool useMSquared;
        public bool UseMSquared
        {
            get {return useMSquared;}
            set {useMSquared = value;}
        }
        //Used to flag the use of motmaster scripts or the UI to generate a sequence
        private bool useMMScripts;
        public bool UseMMScripts
        {
            get { return useMMScripts; }
            set { useMMScripts = value; }
        }

    }
}