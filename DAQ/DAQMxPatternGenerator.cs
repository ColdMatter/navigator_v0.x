
using System;
using System.Threading;

using NationalInstruments.DAQmx;

using DAQ.Environment;

namespace DAQ.HAL
{
	/// <summary>
	/// A class to control the PatternList generator using DAQMx. This class is not horrible, doesn't
	/// suffer from huge memory leaks, and doesn't frequently crash the computer. W00t.
	/// </summary>
	public class DAQMxPatternGenerator : PatternGenerator
	{
		private Task pgTask;
		private String device;
		private DigitalSingleChannelWriter writer;
		private double clockFrequency;
		private int length;
        // this task is used to generate the sample clock on the "integrated" 6229-type PGs
        private Task counterTask;

		public DAQMxPatternGenerator(String device)
		{
			this.device = device;
		}

		// use this method to output a PatternList to the whole PatternList generator
		public void OutputPattern(UInt32[] pattern, bool wait)
		{
			
            writer.WriteMultiSamplePort(true, pattern);
			// This Sleep is important (or at least it may be). It's here to guarantee that the correct PatternList is
			// being output by the time this call returns. This is needed to make the tweak
			// and pg scans work correctly. It has the side effect that you have to wait for
			// at least one copy of the PatternList to output before you can do anything. This means
			// pg scans are slowed down by a factor of two. I can't think of a better way to do
			// it at the moment.
			// It might be possible to speed it up by understanding the timing of the above call
			// - when does it return ?
            if (wait)
			    SleepOnePattern();
		}
        public void OutputPattern(UInt32[] pattern)
        {
            OutputPattern(pattern, true);
        }
		// use this method to output a PatternList to half of the PatternList generator
		public void OutputPattern(Int16[] pattern)
		{
			writer.WriteMultiSamplePort(true, pattern);
			// see above
			SleepOnePattern();
		}
		
		private void SleepOnePattern()
		{
			int sleepTime = (int)(((double)length * 1000) / clockFrequency);
			Thread.Sleep(sleepTime);
		}

		public virtual void Configure( double clockFrequency, bool loop, bool fullWidth,
                                    bool lowGroup, int length, bool internalClock, bool triggered)
		{	
			this.clockFrequency = clockFrequency;
			this.length = length;

			pgTask = new Task("pgTask");

            /**** Configure the output lines ****/

			// The underscore notation is the way to address more than 8 of the PatternList generator
			// lines at once. This is really buried in the NI-DAQ documentation !
			String chanString = "";
            if ((string)Environs.Hardware.GetInfo("PGType") == "dedicated")
            {
                if (fullWidth) chanString = device + "/port0_32";
                else
                {
                    if (lowGroup) chanString = device + "/port0_16";
                    else chanString = device + "/port3_16";
                }
            }
            // as far as I know you can only address the whole 32-bit port on the 6229 type integrated PatternList generators
            if ((string)Environs.Hardware.GetInfo("PGType") == "integrated")
            {
                chanString = device + "/port0";
            }

			DOChannel doChan = pgTask.DOChannels.CreateChannel(
				chanString,
				"pg",
				ChannelLineGrouping.OneChannelForAllLines
				);

            /**** Configure the clock ****/

            String clockSource = "";
            if ((string)Environs.Hardware.GetInfo("PGType") == "dedicated")
            {
                if (!internalClock) clockSource = (string)Environment.Environs.Hardware.GetInfo("PGClockLine");
                else clockSource = "";
            }

            if ((string)Environs.Hardware.GetInfo("PGType") == "integrated")
            {
                // clocking is more complicated for the 6229 style PG boards as they don't have their own internal clock.

                // if external clocking is required it's easy:
                if (!internalClock) clockSource = (string)Environment.Environs.Hardware.GetInfo("PGClockLine");
                else
                {
                    // if an internal clock is requested we generate it using the card's timer/counters.
                    counterTask = new Task();
                    counterTask.COChannels.CreatePulseChannelFrequency(
                        device + (string)Environs.Hardware.GetInfo("PGClockCounter"),
                        "PG Clock",
                        COPulseFrequencyUnits.Hertz,
                        COPulseIdleState.Low,
                        0.0,
                        clockFrequency,
                        0.5
                        );
                    counterTask.Timing.SampleQuantityMode = SampleQuantityMode.ContinuousSamples;
                    counterTask.Start();

                    clockSource = device + (string)Environs.Hardware.GetInfo("PGClockCounter") + "InternalOutput";
                }
            }



            /**** Configure regeneration ****/
            
            SampleQuantityMode sqm;
			if (loop)
			{
				sqm = SampleQuantityMode.ContinuousSamples;
				pgTask.Stream.WriteRegenerationMode = WriteRegenerationMode.AllowRegeneration;
			}
			else
			{
				sqm = SampleQuantityMode.FiniteSamples;
				pgTask.Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
			}

			pgTask.Timing.ConfigureSampleClock(
				clockSource,
				clockFrequency,
				SampleClockActiveEdge.Rising,
				sqm,
				length
				);

            /**** Configure buffering ****/

            if ((string)Environs.Hardware.GetInfo("PGType") == "dedicated")
            {
                // these lines are critical - without them DAQMx copies the data you provide
                // as many times as it can into the on board FIFO (the cited reason being stability).
                // This has the annoying side effect that you have to wait for the on board buffer
                // to stream out before you can update the patterns - this takes ~6 seconds at 1MHz.
                // These lines tell the board and the software to use buffers as close to the size of
                // the PatternList as possible (on board buffer size is coerced to be related to a power of
                // two, so you don't quite get what you ask for).
                // note that 6229 type integrated PGs only have 2kB buffer, so this isn't needed for them (or allowed, in fact)
                pgTask.Stream.Buffer.OutputBufferSize = length;
                pgTask.Stream.Buffer.OutputOnBoardBufferSize = length;
            }

            /**** Configure triggering ****/
            if (triggered)
            {
                pgTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(
                    (string)Environs.Hardware.GetInfo("PGTrigger"),
                    DigitalEdgeStartTriggerEdge.Rising);
            }

            /**** Write configuration to board ****/

			pgTask.Control(TaskAction.Commit);
			writer = new DigitalSingleChannelWriter(pgTask.Stream);
		}
		
		public void StopPattern()
		{
			pgTask.Dispose();
            if ((string)Environs.Hardware.GetInfo("PGType") == "integrated") counterTask.Dispose();
        }
	}
}
