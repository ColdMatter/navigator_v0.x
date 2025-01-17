﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NationalInstruments.DAQmx;

using DAQ.Environment;
using DAQ.HAL;
//using Data;
//using Data.Scans;
//using DAQ.Analog;

using UtilsNS;

namespace DAQ.Analog
{
    public class MMAIWrapper
    {
        #region ClassAttributes
        private Task AITask;
        private Task runningTask = null;
        private String device;
        private AnalogMultiChannelReader AIDataReader;
        private int samples;
        private int nChannels;
        public MMAIConfiguration AIConfig;
        private bool asyncRun = false;
        #endregion

        public MMAIWrapper(String device)
        {
            this.device = device;
        }

        public void Configure(MMAIConfiguration aiConfig, bool loop = false)
        {
        //For now lets just deal with adding a single analog input channel. Want things like sample rate to be specified in the mot master sequance.
            AIConfig = aiConfig;
            samples = aiConfig.Samples;
           // asyncRun = loop;
            AITask = new Task(this.device.Substring(1) + "AITask");

            foreach (string keys in aiConfig.AIChannels.Keys)
            {
                AddToAnalogInputTask(AITask, keys, aiConfig.AIChannels[keys].AIRangeLow,aiConfig.AIChannels[keys].AIRangeHigh);
            }
            AIConfig.AIData = new double[AIConfig.AIChannels.Count, samples];
        //For the timiming - for now just derive the ai sample clock from the PCI card, but this isn't synchronised with the PXI Card, so in future will
        //need to create a counting task on the AICard and count an exported timiming signal from the PXI or something similar.

            AITask.Timing.ConfigureSampleClock("", aiConfig.SampleRate, SampleClockActiveEdge.Rising,
                    SampleQuantityMode.FiniteSamples, aiConfig.Samples);

            AITask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(
                     (string)Environs.Hardware.GetInfo("AIAcquireTrigger"), DigitalEdgeStartTriggerEdge.Rising);

            Utils.Trace("config AI");
            //AITask.Stream.ReadWaitMode = ReadWaitMode.Yield;
            //AITask.Stream.ReadSleepTime = 0.2;
            AITask.Stream.Timeout = 50000;
            AIDataReader = new AnalogMultiChannelReader(AITask.Stream);  

            AITask.Control(TaskAction.Verify);
            AITask.Control(TaskAction.Commit);            
        }

        public void UpdateSamplesCount(MMAIConfiguration aiConfig)
        {
            if (Utils.isNull(AITask)) return;
            AITask.Timing.ConfigureSampleClock("", aiConfig.SampleRate, SampleClockActiveEdge.Rising,
                    SampleQuantityMode.FiniteSamples, aiConfig.Samples);

        }
        #region private methods for creating timed Tasks/channels

        private void AddToAnalogInputTask(Task task, string channel, double RangeLow, double RangeHigh)
        {
        //The configuration settings for the analog input channel live in the RFMOTHardware class
        //Need to specify the input range high/low somewhere - perhaps in the scripts would be best. Then MOTMasterScript would have to return
        //something like an AIConfigurationObject, which we'd pass to the public Configure() method.
            AnalogInputChannel c = ((AnalogInputChannel)Environs.Hardware.AnalogInputChannels[channel]);
            c.AddToTask(task, RangeLow, RangeHigh);
        }
        #endregion
        public void StopPattern()
        {
            Utils.Trace("stop AI");
            AITask.Stop();
            AITask.Dispose();
        }
        public void StartTask()
        {
            Utils.Trace("start AI");
            if (asyncRun) { runningTask = AITask; AIDataReader.BeginReadMultiSample(AIConfig.Samples, new AsyncCallback(OnAnalogDataReady), null); }
            else { /*AITask.Stop();*/ AITask.Start(); }
        }
        public void ReadAnalogDataFromBuffer()
        {           
            try
            {                
                if (AITask.IsDone) return;
                Utils.Trace("read buff AI");
                AIDataReader = new AnalogMultiChannelReader(AITask.Stream);
                AIConfig.AIData = AIDataReader.ReadMultiSample(samples);
                Utils.Trace("read buff AI-2");
            }
            catch (System.ObjectDisposedException e)
            {
                Console.WriteLine("AI problem: "+e.Message);
            }
            finally
            {
                AIDataReader = null;
            }            
        }
        public double[,] GetAnalogData(bool pseudoDoubleChn = true) // EMERGENCY CHANGE, IT MUST false IN NORMAL CONDITIONS !!!
        {
            if (Environs.Hardware.config.DoubleAxes && AIConfig.AIData.GetLength(0).Equals(4))
            {
                int c1 = AIConfig.AIData.GetLength(1);
                if (pseudoDoubleChn)
                {
                    for (int i = 0; i < c1; i++) AIConfig.AIData[0, i] = AIConfig.AIData[1, i]; // both channels are taken from Y-PD
                }
             }            
            return AIConfig.AIData;
        }
        public double[] GetAnalogDataSingleArray()
        {
            double[] data = new double[AIConfig.AIData.Length];
            for (int i = 0; i < data.Length; i++) data[i] = AIConfig.AIData[0, i];
            return data;
        }

        private void OnAnalogDataReady(IAsyncResult i)
        {
            if (runningTask != null)
            {
                AIConfig.AIData = AIDataReader.EndReadMultiSample(i);
                AIDataReader.BeginReadMultiSample(AIConfig.Samples, new AsyncCallback(OnAnalogDataReady), null);
                AnalogDataReceived.Invoke(this, null);
            }
            else
            {
                AIDataReader = null;
            }
        }

        public void AbortRunning()
        {
            runningTask = null;
            StopPattern();
            AITask = null;
            AIDataReader = null;
        }
        public event EventHandler<EventArgs> AnalogDataReceived;

        public void PauseLoop()
        {
            AITask.WaitUntilDone();
            //ReadAnalogDataFromBuffer();
            AITask.Stop();
        }

        public void ClearBuffer()
        {
            AIConfig.AIData = null;
        }
    }
}
