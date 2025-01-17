using System;

using NationalInstruments.DAQmx;

using DAQ.Pattern;

namespace DAQ.HAL
{
	/// <summary>
	/// 
	/// </summary>
	public class DigitalOutputChannel : DAQMxChannel
	{
		private string device;
		private int port;
		public int line;

		public DigitalOutputChannel( string name, string device, int port, int line )
		{
			this.nameIt(name);
			this.device = device;
			this.port = port;
			this.line = line;
           
			physicalChannel = device + "/port" + port + "/line" + line;
		}

		public int BitNumber
		{
			get { return PatternBuilder32.ChannelFromNIPort(port, line); }
		}

		public void AddToTask(Task task)
		{
			task.DOChannels.CreateChannel(
				PhysicalChannel,
				name,
				ChannelLineGrouping.OneChannelForAllLines
				);
		}
        public string Device
        {
            get { return device; }
        }
	}
}
