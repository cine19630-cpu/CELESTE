using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Monocle;
using Celeste.Porting;

namespace Celeste;

public static class UserIO
{
	public enum Mode
	{
		Read,
		Write
	}

	[CompilerGenerated]
	private sealed class _003CSaveRoutine_003Ed__29 : IEnumerator<object>, IDisposable, IEnumerator
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public bool file;

		public bool settings;

		private FileErrorOverlay _003Cmenu_003E5__2;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CSaveRoutine_003Ed__29(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				savingFile = file;
				savingSettings = settings;
				goto IL_0038;
			case 1:
				_003C_003E1__state = -1;
				goto IL_00b4;
			case 2:
				{
					_003C_003E1__state = -1;
					goto IL_00ec;
				}
				IL_0038:
				if (savingFile)
				{
					SaveData.Instance.BeforeSave();
					savingFileData = Serialize(SaveData.Instance);
				}
				if (savingSettings)
				{
					savingSettingsData = Serialize(Settings.Instance);
				}
				savingInternal = true;
				SavingResult = false;
				RunThread.Start(SaveThread, "USER_IO");
				SaveLoadIcon.Show(Engine.Scene);
				goto IL_00b4;
				IL_00b4:
				if (savingInternal)
				{
					_003C_003E2__current = null;
					_003C_003E1__state = 1;
					return true;
				}
				SaveLoadIcon.Hide();
				if (SavingResult)
				{
					break;
				}
				_003Cmenu_003E5__2 = new FileErrorOverlay(FileErrorOverlay.Error.Save);
				goto IL_00ec;
				IL_00ec:
				if (_003Cmenu_003E5__2.Open)
				{
					_003C_003E2__current = null;
					_003C_003E1__state = 2;
					return true;
				}
				if (!_003Cmenu_003E5__2.TryAgain)
				{
					_003Cmenu_003E5__2 = null;
					break;
				}
				goto IL_0038;
			}
			Saving = false;
			Celeste.SaveRoutine = null;
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	public const string SaveDataTitle = "Celeste Save Data";

	private const string SavePath = "Save";

	private const string BackupPath = "Backups";

	private const string Extension = ".celeste";

	private static string RootDir
	{
		get
		{
			var p = PortServices.Paths?.SavePath;
			return string.IsNullOrEmpty(p) ? SavePath : p;
		}
	}


	private static bool savingInternal;

	private static bool savingFile;

	private static bool savingSettings;

	private static byte[] savingFileData;

	private static byte[] savingSettingsData;

	public static bool Saving { get; private set; }

	public static bool SavingResult { get; private set; }

	private static string GetHandle(string name)
	{
		return Path.Combine(RootDir, name + Extension);
	}

	private static string GetBackupHandle(string name)
	{
		return Path.Combine(RootDir, BackupPath, name + Extension);
	}

	public static bool Open(Mode mode)
	{
		return true;
	}

	private static void AtomicWriteBytes(string path, byte[] data)
	{
		var dir = new FileInfo(path).Directory;
		if (dir != null && !dir.Exists)
			dir.Create();
		var tmp = path + ".tmp";
		using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.Read))
		{
			fs.Write(data, 0, data.Length);
			fs.Flush(true);
		}
		if (File.Exists(path))
			File.Delete(path);
		File.Move(tmp, path);
	}

	public static bool Save<T>(string Root, byte[] data) where T : class
	{
		string handle = GetHandle(Root);
		bool flag = false;
		try
		{
			string backupHandle = GetBackupHandle(Root);
			DirectoryInfo directory = new FileInfo(handle).Directory;
			if (!directory.Exists)
			{
				directory.Create();
			}
			directory = new FileInfo(backupHandle).Directory;
			if (!directory.Exists)
			{
				directory.Create();
			}
			AtomicWriteBytes(backupHandle, data);
			if (Load<T>(Root, backup: true) != null)
			{
				AtomicWriteBytes(handle, data);
				flag = true;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("ERROR: " + ex.ToString());
			ErrorLog.Write(ex);
		}
		if (!flag)
		{
			Console.WriteLine("Save Failed");
		}
		return flag;
	}

	public static T Load<T>(string Root, bool backup = false) where T : class
	{
		string path2 = ((!backup) ? GetHandle(Root) : GetBackupHandle(Root));
		T result = null;
		try
		{
			if (File.Exists(path2))
			{
				using (FileStream stream = File.OpenRead(path2))
				{
					result = Deserialize<T>(stream);
					return result;
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("ERROR: " + ex.ToString());
			ErrorLog.Write(ex);
		}
		return result;
	}

	private static T Deserialize<T>(Stream stream) where T : class
	{
		return (T)new XmlSerializer(typeof(T)).Deserialize(stream);
	}

	public static bool Exists(string Root)
	{
		return File.Exists(GetHandle(Root));
	}

	public static bool Delete(string Root)
	{
		string handle = GetHandle(Root);
		if (File.Exists(handle))
		{
			File.Delete(handle);
			return true;
		}
		return false;
	}

	public static void Close()
	{
	}

	public static byte[] Serialize<T>(T instance)
	{
		using MemoryStream memoryStream = new MemoryStream();
		new XmlSerializer(typeof(T)).Serialize(memoryStream, instance);
		return memoryStream.ToArray();
	}

	public static void SaveHandler(bool file, bool settings)
	{
		if (!Saving)
		{
			Saving = true;
			Celeste.SaveRoutine = new Coroutine(SaveRoutine(file, settings));
		}
	}

	[IteratorStateMachine(typeof(_003CSaveRoutine_003Ed__29))]
	private static IEnumerator SaveRoutine(bool file, bool settings)
	{
		savingFile = file;
		savingSettings = settings;
		FileErrorOverlay menu;
		do
		{
			if (savingFile)
			{
				SaveData.Instance.BeforeSave();
				savingFileData = Serialize(SaveData.Instance);
			}
			if (savingSettings)
			{
				savingSettingsData = Serialize(Settings.Instance);
			}
			savingInternal = true;
			SavingResult = false;
			RunThread.Start(SaveThread, "USER_IO");
			SaveLoadIcon.Show(Engine.Scene);
			while (savingInternal)
			{
				yield return null;
			}
			SaveLoadIcon.Hide();
			if (SavingResult)
			{
				break;
			}
			menu = new FileErrorOverlay(FileErrorOverlay.Error.Save);
			while (menu.Open)
			{
				yield return null;
			}
		}
		while (menu.TryAgain);
		Saving = false;
		Celeste.SaveRoutine = null;
	}

	private static void SaveThread()
	{
		SavingResult = false;
		if (Open(Mode.Write))
		{
			SavingResult = true;
			if (savingFile)
			{
				SavingResult &= Save<SaveData>(SaveData.GetFilename(), savingFileData);
			}
			if (savingSettings)
			{
				SavingResult &= Save<Settings>("settings", savingSettingsData);
			}
			Close();
		}
		savingInternal = false;
	}
}
