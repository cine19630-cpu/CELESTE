namespace Celeste.Porting;

public interface IPathsProvider
{
    string BaseDataPath { get; }
    string ContentPath { get; }
    string LogsPath { get; }
    string SavePath { get; }
}
