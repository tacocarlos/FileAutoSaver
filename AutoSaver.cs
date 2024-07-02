using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Timers;
using Windows.Storage.Search;

namespace er_autosave_gui
{
	public class OnFileWriteArgs : EventArgs
	{
		public string SourcePath;
		public string DestinationPath;
		public DateTime WriteTime;
	}
	public delegate void delEventHandler(object sender, OnFileWriteArgs e);

	public class AutoSaver
	{
		public delEventHandler OnFileWrite;

		public uint TimerDurationInSeconds 
		{ 
			set 
			{
				timerDurationInMilli = value * 1000;
				InitTimer(timerDurationInMilli);
			}
		}

		private ulong timerDurationInMilli = 1 * 1000;
		private Timer timer;

		private DateTime startTime;
		public DateTime StartTime { get { return startTime; } }

		private string sourcePath = String.Empty;
		public string SourcePath
		{
			get { return sourcePath; }
			set
			{
				if(AutoSaver.VerifySourcePath(value))
				{
					sourcePath = value;
				}
			}
		}

		private string destPath = String.Empty;
		public string DestPath
		{ 
			get { return destPath; }
			set
			{
				if (AutoSaver.VerifyDestinationPath(value))
				{
					destPath = value;
				}
			}
		}

		private void InitTimer(ulong duration)
		{
			if (timer != null)
				timer.Stop();

			timer = new Timer(duration);
			timer.Elapsed += TimerElapsedEvent;
			timer.AutoReset = true;
		}

		public AutoSaver(string sourcePath, string destPath, uint timeDurationInSeconds)
		{
			this.sourcePath = sourcePath;
			this.destPath = destPath;
			TimerDurationInSeconds = timeDurationInSeconds;
			InitTimer(timerDurationInMilli);
		}

		public void Start()
		{
			startTime = DateTime.Now;
			timer.Start();

			Debug.WriteLine("Started AutoSaver");
		}
		
		public void Stop()
		{
			timer.Stop();
			Debug.WriteLine("Stoppped AutoSaver");
		}

		private void TimerElapsedEvent(object sender, ElapsedEventArgs e)
		{
			Debug.WriteLine($"Timer Trigger: {DateTime.Now.ToLongTimeString()}");
			try
			{
				DateTime now = DateTime.Now;
				string fileName = Path.GetFileNameWithoutExtension(SourcePath);
				string ext = Path.GetExtension(SourcePath);
				File.Copy(SourcePath, 
					Path.Join(DestPath, 
						fileName + now.ToString("--MM_dd_yy--H-mm-ss") + ext)
				);
				OnFileWrite?.Invoke(this, new OnFileWriteArgs { DestinationPath = DestPath, SourcePath = SourcePath, WriteTime = DateTime.Now });
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return;
			}
		}

		public bool IsReady()
		{
			bool status = VerifyDestinationPath(DestPath) && VerifySourcePath(SourcePath);
			if(!status)
			{
				Debug.WriteLine("Paths are not valid.");
			}
			return status;
		}

		// verify that it exits
		public static bool VerifySourcePath(string sourcePath)
		{
			if (sourcePath == String.Empty || sourcePath == null || !File.Exists(sourcePath)) return false;

			try
			{
				File.OpenRead(sourcePath).Dispose();
				return true;
			} 
			catch(IOException ex) 
			{ 
				Debug.WriteLine(ex); 
				return false; 
			}
		}

		// verify that we have write access
		public static bool VerifyDestinationPath(string destPath)
		{
			if (destPath == String.Empty || destPath == null || !Path.Exists(destPath)) 
				return false;

			return WritableDirectory(destPath);
		}

		// probably a better way to do this but other methods caused an exception that I don't want to debug
		private static bool WritableDirectory(string dir)
		{
			try
			{
				string fileName = Path.GetRandomFileName();
				string filePath = Path.Combine(dir, fileName);
				File.WriteAllText(filePath, "test content");
				File.Delete(filePath);
				return true;
			}
			catch (Exception)
			{

				throw;
			}
		}
	}
}
