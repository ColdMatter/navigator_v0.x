﻿using System;
using System.Threading;
using UtilsNS;

using NationalInstruments.ModularInstruments.Interop;

using DAQ.Environment;

namespace DAQ.HAL
{
    /// <summary>
    /// A class to control the PatternList generator using a HSDIO card. This is designed to operate similarly to the DAQMxPatternGenerator
    /// </summary>
    public class HSDIOPatternGenerator : DAQMxPatternGenerator
    {
        //The HSDIO cards do not have Task objects. Instead the card is initialised for generation or acquisition and the waveform is written to the card
        private niHSDIO hsTask;
        private string device;
        private double clockFrequency;
        private int length;
        private bool loop;
        public HSDIOPatternGenerator(String device):base(device)
        {
            this.device = device;
        }
        
        public void Configure(double clockFrequency, bool loop, bool internalClock, bool triggered)
        {
            this.clockFrequency = clockFrequency;
           // this.length = length;

            /**** Configure the output lines ****/
            hsTask = niHSDIO.InitGenerationSession(device, true, false, "");

  
            //configure the card for dynamic generation
            hsTask.AssignDynamicChannels("0-31");

            /**** Configure the clock ****/
            String clockSource = "";
            if (!internalClock) clockSource = (string)Environment.Environs.Hardware.GetInfo("HSClockLine");
            else clockSource = "";

            
            hsTask.ConfigureSampleClock(clockSource, clockFrequency);
            if (loop)
            { hsTask.ConfigureRefClock(niHSDIOConstants.PxiClkStr, 100000000); }

            if (!triggered)
            /**** Configure regeneration ****/
                this.loop = loop;
            /**** Configure triggering ****/
            if (triggered)
            {
                hsTask.ConfigureDigitalEdgeStartTrigger((string)Environs.Hardware.GetInfo("HSTrigger"), niHSDIOConstants.RisingEdge);
            }
            else
            {
                //This card will trigger the others and uses the trigger line defined in the hardware class
                hsTask.ExportSignal(niHSDIOConstants.StartTrigger, "", (string)Environs.Hardware.GetInfo("HSTrigger"));
                //hsTask.ExportSignal(niHSDIOConstants.SampleClock,"",)//
            }
            /*** Write configuration to board ****/
            hsTask.CommitDynamic();
        }
       
        public void OutputPattern(uint[] pattern, int[] loopTimes)
        {
            //Check lengths are equal
            if (pattern.Length != loopTimes.Length)
                throw new Exception("Pattern length not equal to number of loop times");
            //loop over pattern to write the waveforms to the card and build a script string
            string script = "script myScript \n";
            string tabStr = "\t";
            //if (loop) { script += "\t repeat forever \n"; tabStr = "\t\t"; }
            int width;
            uint[] data;
            length = pattern.Length;
            for (int i = 0; i < loopTimes.Length;i++ )
            {
                if (loopTimes[i] % 4 != 0)
                {
                    width = 8;
                    //data = new uint[width];
                    if (loopTimes[i] % 2 == 0)
                    {
                        loopTimes[i] = loopTimes[i] / 2;

                    }
                    else
                    {
                        throw new Exception("Loop time not a multiple of 4 master clock cycles. Change this to avoid digital pattern timing errors.");
                    }
                }
                else
                {
                   width = 4;
                   //data = new uint[width];
                }
                data = new uint[width];
                for (int j = 0; j < width; j++)
                {
                    data[j] = pattern[i];
                }
                hsTask.WriteNamedWaveformU32("waveform" + i, width, data);
                if (loopTimes[i] == 0)
                    script += tabStr + " generate waveform" + i + "\n";
                else 
                    script += tabStr+ " Repeat " + loopTimes[i]/4 + "\n "+ tabStr + " generate waveform" + i +"\n" + tabStr + " end repeat \n";
                }
            //if (loop) script += "\t end repeat \n";
            script += "end script";
            //Writes the script to the card
            hsTask.SetGenerationMode("", niHSDIOConstants.Scripted);
            hsTask.WriteScript(script);
            
            //This assumes the hsdio card triggers the other cards, which is most likely the case
            hsTask.ConfigureScriptToGenerate("myScript");
            
            
            hsTask.Initiate();
            //To avoid timing issues associated with the different pattern generators, I'll add the sleeop one pattern method here
           // SleepOnePattern();
        }

        public void BuildScriptForDebug(uint[] pattern, int[] loopTimes)
        {
            //Check lengths are equal
            if (pattern.Length != loopTimes.Length)
                throw new Exception("Pattern length not equal to number of loop times");
            //loop over pattern to write the waveforms to the card and build a script string
            string script = "script myScript \n";
            int width;
            uint[] data;
            length = pattern.Length;
            for (int i = 0; i < loopTimes.Length; i++)
            {
                if (loopTimes[i] % 4 != 0)
                {
                    width = 8;
                    data = new uint[width];
                    if (loopTimes[i] % 2 == 0)
                    {
                        loopTimes[i] = loopTimes[i] / 2;

                    }
                    else
                    {
                        throw new Exception("Loop time not a multiple of 4 master clock cycles. Change this to avoid digital pattern timing errors.");
                    }
                }
                else
                {
                    width = 4;
                    data = new uint[width];
                }
                for (int j = 0; j < width; j++)
                {
                    data[j] = pattern[i];
                }
                if (loopTimes[i] == 0)
                    script += "\t generate waveform" + i + "\n";
                else
                    script += "\t Repeat " + loopTimes[i] / 4 + "\n \t generate waveform" + i + "\n\t end repeat \n";
            }
            script += "end script";
        }
        private void SleepOnePattern()
		{
			int sleepTime = (int)(((double)length * 1000) / clockFrequency);
			Thread.Sleep(sleepTime);
		}
        public void StopPattern()
        {
            //This is a pretty bad way of waiting until the sequence has finished before trying to delete the waveforms
            WaitUntilDone();
            hsTask.Dispose();
        }
        public void AbortRunning()
        {
            hsTask.Abort();
            hsTask.reset();
            hsTask.Dispose();

        }

        public void StartPattern()
        {
            hsTask.Initiate();
        }

        public void WaitUntilDone()
        {
            bool done;
            int dTime;
            if (hsTask == null)
            {
                throw new Exception("Tried to stop non-existing HSDIO task.");
            }
            try
            {
                hsTask.IsDone(out done);
            }
            catch
            {
                return;
            }
            dTime = hsTask.WaitUntilDone(10000);
            while (dTime != 0)
            {
                dTime = hsTask.WaitUntilDone(10000);
            }

        }

        public void PauseLoop()
        {
            WaitUntilDone();
        }
    }
}
