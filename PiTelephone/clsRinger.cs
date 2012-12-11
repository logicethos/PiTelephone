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
    public class clsRinger : IDisposable
    {
		public static readonly float[] ringPattern_UK = { 0.4f, 0.2f, 0.4f, 2f }; //UK ring timing goes .4 sec on, .2 sec off, .4 sec on, 2 sec off.
		public static readonly float[] ringPattern_USA = { 2f, 4f }; //USA 2 seconds on and 4 seconds off

		public int ringHz = 25;

		private float[] ringPattern;

        Thread RingerThread = null;
	    
        
		OutputPinConfiguration RingerPowerPin;
		OutputPinConfiguration RingerOscillatorPin;

		GpioConnection GPIO;


		public clsRinger (OutputPinConfiguration RingerPower, OutputPinConfiguration RingerOscillator, float[] RingPattern = null)
        {
			if (RingPattern==null) RingPattern = ringPattern_UK;
			ringPattern = RingPattern;

			RingerPowerPin = RingerPower;
			RingerOscillatorPin = RingerOscillator;

			var GPIOconfig = new GpioConnectionSettings();
			GPIO = new GpioConnection(GPIOconfig,RingerPowerPin,RingerOscillatorPin);

            RingerThread = new Thread(Ring);
            RingerThread.IsBackground = true;
			RingerThread.Name = "Ringer Thread";

        }
               

        public void SetRingPattern(float[] secsOnOffArray)
        {
            ringPattern = secsOnOffArray;
        }


        public void StartRing ()
		{

			//REMOVE ME Dont know why, but had problems starting the thread without reinitialising it. Pi/Mono bug?
			//****
			RingerThread = new Thread(Ring);
			RingerThread.IsBackground = true;
			RingerThread.Name = "Ringer Thread";
			//****

			if ((RingerThread.ThreadState & (ThreadState.Unstarted | ThreadState.WaitSleepJoin | ThreadState.Stopped)) != ThreadState.Running)
			{
				try
				{
					RingerThread.Start();
				} catch (Exception ex)
				{
					Console.WriteLine ("Error starting Ringer Thread: " + ex.Message);
				}
			} 
        }

        public void StopRing()
        {
            RingerThread.Abort();
        }


        private void Ring()
        {
			Console.WriteLine("Ringer Start");

			if (!GPIO[RingerPowerPin.Pin])  GPIO.Toggle(RingerPowerPin.Pin);

            try
            {
                while (true)
                {
					int ms = 1000 / ringHz;
					                   
					for (int f = 0; f < ringPattern.Length; f++)  //Loop for each number in the ring pattern array
                    {

						for (int i = 0; i < ringHz * ringPattern[f]; i++)  //Oscillate solenoid for period defind in pattern
                        {
							GPIO.Toggle(RingerOscillatorPin);
                            Thread.Sleep(ms);
                        }

						Thread.Sleep((int)(ringPattern[++f] * 1000));
                    }
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                Console.WriteLine("Ringer Stop");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally //reset outputs
            {
				if (GPIO[RingerOscillatorPin.Pin])  GPIO.Toggle(RingerOscillatorPin.Pin);
				if (GPIO[RingerPowerPin.Pin])  GPIO.Toggle(RingerPowerPin.Pin);
            }
        }

		public void Dispose ()
		{
			RingerThread.Abort();
			GPIO.Close();
		}
    }
}
