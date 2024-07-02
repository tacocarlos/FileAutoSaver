using Config.Net;
using FileAutosaver;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace er_autosave_gui
{
	public sealed partial class MainWindow : WinUIEx.WindowEx
	{
		ISettings settings = new ConfigurationBuilder<ISettings>()
			.UseIniFile("./settings.ini")
			.Build();

		const string SOURCE_SETTING = "source";
		const string DEST_SETTING = "destination";

		AutoSaver saver;

		private void UpdateSource(string newSource)
		{
			if (!AutoSaver.VerifySourcePath(newSource))
				return;

			saver.SourcePath = newSource;
			settings.SourcePath = newSource;
			sourceBlock.Text = newSource;
		}

		private void UpdateDestination(string newDest)
		{
			if (!AutoSaver.VerifyDestinationPath(newDest))
				return;

			saver.DestPath = newDest;
			settings.DestinationPath = newDest;
			destBlock.Text = newDest;
		}

		private async Task<string> GetFile(PickerLocationId suggestedInitialLocation=PickerLocationId.DocumentsLibrary)
		{
			FileOpenPicker picker = new FileOpenPicker();
			picker.ViewMode = PickerViewMode.List;
			picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
			picker.FileTypeFilter.Add("*");
			var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
			WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
			StorageFile file = await picker.PickSingleFileAsync();
			if (file != null)
			{
				userMessage.Text = $"Picked `{file.Path}`";
				return file.Path;
			}
			else
			{
				userMessage.Text = "Failed/Canceled pick operation";
				return String.Empty;
			}
		}

		private async Task<string> GetFolder(PickerLocationId suggestedInitialLocation=PickerLocationId.DocumentsLibrary)
		{
			FolderPicker picker = new FolderPicker();
			picker.ViewMode = PickerViewMode.List;
			picker.SuggestedStartLocation = suggestedInitialLocation;

			var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
			WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
			StorageFolder folder = await picker.PickSingleFolderAsync();
			if (folder != null)
			{
				userMessage.Text = $"Picked `{folder.Path}`";
				return folder.Path;
			}
			else
			{
				userMessage.Text = "Failed/Canceled pick operation";
				return String.Empty;
			}
		}

		public MainWindow()
		{
			uint timeDuration = settings.TimeIntervalInSeconds == 0 ? 60 : settings.TimeIntervalInSeconds;
			saver = new AutoSaver(settings.SourcePath, settings.DestinationPath, timeDuration);
			saver.OnFileWrite += OnFileWrite;

			InitializeComponent();
			SecondsInput.Value = timeDuration;
			sourceBlock.Text = settings.SourcePath;
			destBlock.Text = settings.DestinationPath;
		}

		private async void SelectSource(object sender, RoutedEventArgs e)
		{
			string sourcePath = await GetFile();
			UpdateSource(sourcePath);
		}

		private async void SelectDest(object sender, RoutedEventArgs e)
		{
			string destPath = await GetFolder();
			UpdateDestination(destPath);
		}

		private async void StartAutoSaver(object sender, RoutedEventArgs e)
		{
			Debug.WriteLine(saver.SourcePath);
			Debug.WriteLine(saver.DestPath);
			if (saver.SourcePath == String.Empty || saver.SourcePath == null)
			{
				ContentDialog noSourceDialog = new ContentDialog
				{
					Title = "No Source File",
					Content = "Either invalid choice of file or file not selected.",
					CloseButtonText = "Ok"
				};

				noSourceDialog.XamlRoot = Content.XamlRoot;
				await noSourceDialog.ShowAsync();
				return;
			}

			if(!saver.IsReady()) return;

			SecondsInput.IsEnabled = false;
			userMessage.Text = $"Started Auto Saver at {DateTime.Now.ToLongTimeString()}";
			saver.Start();
		}

		private void StopAutoSaver(object sender, RoutedEventArgs e)
		{
			if (SecondsInput.IsEnabled) return;

			SecondsInput.IsEnabled = true;
			saver.Stop();
			userMessage.Text = "Stopped Auto Saver";
		}

		private uint ClampDouble(double value)
		{
			return Double.IsInteger(value) ? (uint)value: (uint)Math.Floor(value);
		}

		private void SecondsInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
		{
			SecondsInput.Value = ClampDouble(sender.Value);
		}

		private void SecondsInput_LostFocus(object sender, RoutedEventArgs e)
		{
			SecondsInput.Value = Math.Max(ClampDouble(SecondsInput.Value), 1); // prevent a timer of length 0
			uint duration = (uint)SecondsInput.Value;
			settings.TimeIntervalInSeconds = duration;
			saver.TimerDurationInSeconds = (uint) duration;
			Debug.WriteLine("Upated Timer Interval");
		}

		private void OnFileWrite(object sender, OnFileWriteArgs e) => DispatcherQueue.TryEnqueue(() =>
		{
			userMessage.Text = $"File Backup: {e.WriteTime.ToLongTimeString()}";
		});
	}
}
