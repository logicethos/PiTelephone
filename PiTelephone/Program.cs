//Copyright (c) 2012, Logic Ethos Ltd, logicethos.com.
// Open Source BSD License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Mono.Posix;
using Mono.Unix;

namespace PiTelephone
{
	class Program
	{
		static void Main (string[] args)
		{

			//Set up IO - (TODO, allow to be set in config file)
			var RingerPower = new Raspberry.IO.GeneralPurpose.OutputPinConfiguration (Raspberry.IO.GeneralPurpose.ProcessorPin.Pin17);
			var RingerOscillator = new Raspberry.IO.GeneralPurpose.OutputPinConfiguration (Raspberry.IO.GeneralPurpose.ProcessorPin.Pin18);
			var HookSwitch = new Raspberry.IO.GeneralPurpose.InputPinConfiguration (Raspberry.IO.GeneralPurpose.ProcessorPin.Pin22)
			{
				Reversed = true
			};
			var DialPulseSwitch = new Raspberry.IO.GeneralPurpose.InputPinConfiguration (Raspberry.IO.GeneralPurpose.ProcessorPin.Pin27);


			//Start Ringer and DialListener GPIO classes
			using (var ringer = new clsRinger (RingerPower, RingerOscillator))
			{
				using (var dialListener = new clsDialHookListener(HookSwitch,DialPulseSwitch))
				{


					//Simple bell test function. 0 = UK ring, 1 = USA Ring
					dialListener.NumberDialed += (uint NumberDialed) => 
					{
						Console.WriteLine("Number Dialed:{0}",NumberDialed);

						if (NumberDialed == 0) {
							ringer.SetRingPattern (clsRinger.ringPattern_UK);
							ringer.StartRing ();
						} else if (NumberDialed == 9) {
							ringer.SetRingPattern (clsRinger.ringPattern_USA);
							ringer.StartRing ();
						}
					};

					//Cancel the Ringer
					dialListener.HookSwitchChange += (bool OnHook, uint Pulse) => 
					{
						if (!OnHook)
							ringer.StopRing ();
					};


					UnixSignal[] signals = new UnixSignal [] {
						new UnixSignal (Mono.Unix.Native.Signum.SIGINT),
						new UnixSignal (Mono.Unix.Native.Signum.SIGUSR1),
					};

					while (true)
					{

						int index = UnixSignal.WaitAny (signals, -1); //Wait for any Unix Signals
						
						Mono.Unix.Native.Signum signal = signals [index].Signum;
						Console.Write("SIGNAL:{0}",signal.ToString());
						break;
					}
						;
				}
			}
			Console.WriteLine ("**end**");
		}
	}


}