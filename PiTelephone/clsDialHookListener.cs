//Copyright (c) 2012, Logic Ethos Ltd, logicethos.com.
// Open Source BSD License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Raspberry.IO.GeneralPurpose;

namespace PiTelephone
{
	/// <summary>
	/// Dial pulse & Hook switch listener
	/// </summary>
    public class clsDialHookListener : IDisposable
	{

		InputPinConfiguration HookIO;
		InputPinConfiguration DialIO;

		GpioConnection GPIO;


        public delegate void EventHandler_HookSwitchChange(Boolean OnHook, uint Pulse);
        public event EventHandler_HookSwitchChange HookSwitchChange;

        public delegate void EventHandler_NumberDialed(uint NumberDialed);
        public event EventHandler_NumberDialed NumberDialed;
    

		private ManualResetEvent HookWaitEvent = new ManualResetEvent(false);
		private ManualResetEvent DialWaitEvent = new ManualResetEvent(false);

		DateTime LastHookPulse = DateTime.UtcNow;
		DateTime LastDialPulse = DateTime.UtcNow;

		const uint DialPulseMaxMs = 300;
		const uint HookSwitchMaxMs = 800;
       
		Boolean KeepRunning = true;

        Thread DialListenerThread = null;
		Thread HookListenerThread = null;

       
		uint DialPulseCount=0;
		uint HookPulseCount=0;

		public Boolean Debug = false;


		public clsDialHookListener (InputPinConfiguration HookInput, InputPinConfiguration PulseDialInput)
        {
			HookIO = HookInput;
			DialIO = PulseDialInput;


			var config = new GpioConnectionSettings()
			{
				PollInterval = 5,
			};


			GPIO = new GpioConnection(config,HookIO,DialIO);

					

			GPIO.PinStatusChanged += (object sender, PinStatusEventArgs e) => {  //switch change event handler

				if (e.Configuration.Pin == HookIO.Pin)
				{
						if (GPIO[HookIO]) HookPulseCount++;
						HookWaitEvent.Set();
				}
				else if (e.Configuration.Pin == DialIO.Pin)
				{
					if (GPIO[DialIO])
					{
						DialPulseCount++;
						DialWaitEvent.Set();
					}
				}
				else
				{
					Console.WriteLine("Huh?! Wrong IO: "+e.Configuration.Name);
				}

			};

			DialListenerThread = new Thread(ListenDial)
			{ 
				Name = "DialListener",
				IsBackground = true,
			};
			DialListenerThread.Start();

			HookListenerThread = new Thread(ListenHookSwitch)
			{ 
				Name = "HookListener",
				IsBackground = true,
			};
			HookListenerThread.Start();

        }
               
		       

		void HookChange()
		{
			Console.WriteLine ("HOOK "+ (GPIO[HookIO.Pin]?"ON":"OFF"));
		}


		private void ListenDial ()
		{
			int MillisecondsToDialedNumber = (int)DialPulseMaxMs + 100;
			Console.WriteLine ("ListenDial Thread Started");

			while (KeepRunning)
			{
				DialWaitEvent.WaitOne(); //Wait for pulse
				DialWaitEvent.Reset();
				LastDialPulse = DateTime.UtcNow;
				Console.Write("|+");

				while (KeepRunning)  //InnerLoop waiting for next number to be dialed
				{

					DialWaitEvent.WaitOne(MillisecondsToDialedNumber);  //Wait for next pulse, or timout

					var milliseconds = (DateTime.UtcNow-LastDialPulse).TotalMilliseconds;
					Console.Write("+");
					Console.WriteLine ("+ "+milliseconds.ToString());
					LastDialPulse = DateTime.UtcNow;
					if (milliseconds>DialPulseMaxMs)  //Finished counting pulses
					{
						if (Debug) Console.WriteLine ("Dial: {0} {1}",DialPulseCount,GPIO[DialIO]?"On":"Off");
						if (NumberDialed != null) NumberDialed(DialPulseCount > 9 ? 0 : DialPulseCount);
						DialPulseCount = 0;
						DialWaitEvent.Reset();
						break; //return to main loop
					}
					DialWaitEvent.Reset();
				}
			}
			Console.WriteLine ("ListenDial Thread STOPPED");
		}

		private void ListenHookSwitch ()
		{
			int MillisecondsToHookEvent = (int)HookSwitchMaxMs + 100;
			Console.WriteLine ("ListenHookSwitch: Thread Started");
			
			while (KeepRunning)
			{
				HookWaitEvent.WaitOne(); //Wait for hook switch to toggle
				HookWaitEvent.Reset();
				LastHookPulse = DateTime.UtcNow;
				Console.Write("|-");
				
				while (KeepRunning)  //InnerLoop waiting for next number to be dialed
				{
					
					HookWaitEvent.WaitOne(MillisecondsToHookEvent);  //Wait for next pulse, or timout
					
					var milliseconds = (DateTime.UtcNow-LastHookPulse).TotalMilliseconds;
					Console.Write("-");
					//Console.WriteLine ("+ "+milliseconds.ToString());
					LastDialPulse = DateTime.UtcNow;
					if (milliseconds>HookSwitchMaxMs)  //Finished counting pulses
					{
						Boolean switchState = GPIO[HookIO];
						if (switchState) HookPulseCount = 0;
						if (Debug) Console.WriteLine ("Hook: {0} {1}",switchState?"On":"Off",HookPulseCount);
						if (HookSwitchChange != null) HookSwitchChange(switchState, HookPulseCount);
						HookPulseCount = 0;
						HookWaitEvent.Reset();
						break; //return to main loop
					}
					HookWaitEvent.Reset();
				}
				
			}
			Console.WriteLine ("ListenHookSwitch: Thread STOPPED");
		}

		public void Dispose()
		{
			KeepRunning = false;
			HookListenerThread.Abort();
			NumberDialed = null;
			HookSwitchChange = null;
			HookWaitEvent.Close();
			DialWaitEvent.Close();
			GPIO.Close();
		}

    }
}
