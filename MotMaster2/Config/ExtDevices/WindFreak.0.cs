﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MOTMaster2;
using MOTMaster2.SequenceData;
using ErrorManager;
using UtilsNS;

namespace MOTMaster2.ExtDevices
{
    /// <summary>
    /// Interaction logic for WindFreak.xaml
    /// </summary>
    public partial class WindFreak : ucExtDevice
    {

        public override bool OptEnabled()
        {
            if (Utils.isNull(genOpt)) return false; 
            else return genOpt.WindFreakEnabled;
        }

        public override bool CheckHardware()
        {
            if (Controller.config.Debug) lastCheckHardware = true;
            // ping pong with the device
            else lastCheckHardware = false;
            //Controller.microSynth.
            return (bool)lastCheckHardware;              
        }

        public override bool Send2Dvc(string fctName, double fctValue) // hardware update
        {
            return true;
        }

        public WindFreak(string _dvcName) : base(_dvcName)
        {
            
        }

        public override void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions) // params, opts
        {            
            Factors.Add("Amplitude chn.A", new Factor("Amplitude chn.A","amplitude:A"));
            Factors.Add("Frequency chn.A", new Factor("Frequency chn.A", "frequency:A"));
            Factors.Add("Amplitude chn.B", new Factor("Amplitude chn.B", "amplitude:B"));
            Factors.Add("Frequency chn.B", new Factor("Frequency chn.B", "frequency:B"));
            Factors.Add("c33", new Factor("c33"));
            UpdateFactors();
            base.Init(ref _sequenceData, ref _genOptions);
        }



     }
}
