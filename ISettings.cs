namespace FileAutosaver
{
	public interface ISettings
	{
		string SourcePath { get; set; }
		string DestinationPath { get; set; }
		uint TimeIntervalInSeconds { get; set; }
	}
}
