using NLua;
using System.ComponentModel;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace TextEngine;

public static class Program
{
	private static InstanceJson instance;
	private static Lua lua;
	private static int nodePointer = 0;

	private static List<string> inventory = new();
	private static SoundPlayer sound;

	public static void Main()
	{
		Load();
		ShowIntro();

		while (true)
		{
			NodeJson node = instance.nnodes[nodePointer];
			List<int> wrongOptions = new();
			OnlyWindows(() => sound = new());

			if (node.id == null) throw new InvalidOperationException("Node id can't be null!");
			if (!node.end && node.options == null) throw new InvalidOperationException("Node options can't be null!");
			if (node.text == null) throw new InvalidOperationException("Node text can't be null!");

			if (node.sound != null)
			{
				OnlyWindows(() => PlaySound(node.sound));
			}
			if (node.ambient != null) 
			{
				OnlyWindows(() => PlaySound(node.ambient, true));
			}

			CallLuaFunction(instance.prescript);
			CallLuaFunction(node.prescript);

			Console.WriteLine(node.text);

			CallLuaFunction(instance.postscript);
			CallLuaFunction(node.postscript);

			if (node.end) break;

			#region Option user input
			PrintInventory();
			for (int i = 0; i < node.options.Length; i++)
			{
				OptionJson option = node.options[i];

				if (option.text == null) throw new InvalidOperationException("Option name can't be null!");
				if (option.transfer_id == null) throw new InvalidOperationException("Option transfer id can't be null!");

				if (option.mandatory_items != null)
				{
					if (CanChooseOption(option.mandatory_items))
					{
						Console.ForegroundColor = ConsoleColor.Green;
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
						wrongOptions.Add(i);
					}
				}
				Console.WriteLine($"{i + 1}) {option.text}");
				Console.ForegroundColor = ConsoleColor.White;
			}
			PrintSeparator();

		ask:
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write(":> ");
			Console.ForegroundColor = ConsoleColor.White;
			string userOption = Console.ReadLine();
			int intUserOption;
			try
			{
				intUserOption = int.Parse(userOption) - 1;
				_ = node.options[intUserOption];
				if (wrongOptions.Contains(intUserOption))
				{
					throw new Exception();
				}
			}
			catch (Exception)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Неверный ввод!");
				Console.ForegroundColor = ConsoleColor.White;
				goto ask;
			}

			OptionJson o = node.options[intUserOption];
			if (o.items_removed && o.mandatory_items != null)
			{
				foreach (var item in o.mandatory_items)
				{
					inventory.Remove(item);
				}
			}
			#endregion

			Console.Clear();
			OnlyWindows(sound.Stop);

			nodePointer = GetNodeIndexById(node.options[intUserOption].transfer_id);
		}

		Console.ReadKey();
	}



	public static void Load()
	{
#pragma warning disable CS8601
#pragma warning disable CS8602
		//Load instance.json
		instance = JsonSerializer.Deserialize<InstanceJson>(File.ReadAllText(Constants.INSTANCE_PATH));
		instance.nnodes = instance.nodes.ToList();
		if (instance.title == null) throw new InvalidOperationException("Instance name can't be null!");

		//Load script.lua
		lua = new();
		lua.State.Encoding = Encoding.UTF8;
		lua.LoadCLRPackage();

		lua.RegisterFunction("te_addItem", typeof(Program).GetMethod(nameof(LuaAddItem)));
		lua.RegisterFunction("te_setSound", typeof(Program).GetMethod(nameof(PlaySound)));

		lua.RegisterFunction("te_addOption", typeof(Program).GetMethod(nameof(LuaAddOption)));
		lua.RegisterFunction("te_removeOption", typeof(Program).GetMethod(nameof(LuaRemoveOption)));

		lua.RegisterFunction("te_printSeparator", typeof(Program).GetMethod(nameof(PrintSeparator)));
		lua.RegisterFunction("te_clear", typeof(Program).GetMethod(nameof(LuaClear)));
		lua.RegisterFunction("te_print", typeof(Program).GetMethod(nameof(LuaPrint)));
		lua.DoFile(Constants.SCRIPT_PATH);

		//Init console
		Console.Title = instance.title;
		Console.ForegroundColor = ConsoleColor.White;
		Console.OutputEncoding = Encoding.UTF8;

		//Init sound
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("Sound is not yet supported on Linux!");
			Console.ForegroundColor = ConsoleColor.White;
		}
#pragma warning restore CS8601
#pragma warning restore CS8602
	}

	#region Utilz
	public static int GetNodeIndexById(string id) 
	{
		return instance.nnodes.FindIndex(x => x.id == id);
	}
	public static void OnlyWindows(Action action) 
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
		{
			action();
		}
	}
	public static void PlaySound(string wavFilename, bool loop = false) 
	{
		sound = new(Constants.SOUND_PATH + wavFilename + ".wav");
		if (loop) sound.PlayLooping();
		else sound.Play();
	}
	public static bool CanChooseOption(string[] mandatoryItems)
	{
		if (mandatoryItems == null) return true;
		foreach (var item in mandatoryItems)
		{
			if (!inventory.Contains(item)) return false;
		}
		return true;
	}
	public static void CallLuaFunction(string name)
	{
		if (name == null) return;
		var state = lua[name];
		((LuaFunction)state).Call();
	}
	#endregion

	#region Prints
	public static void ShowIntro()
	{
		string title = """
		
		  _______           _     ______                _              
		 |__   __|         | |   |  ____|              (_)             
		    | |  ___ __  __| |_  | |__    _ __    __ _  _  _ __    ___ 
		    | | / _ \\ \/ /| __| |  __|  | '_ \  / _` || || '_ \  / _ \
		    | ||  __/ >  < | |_  | |____ | | | || (_| || || | | ||  __/
		    |_| \___|/_/\_\ \__| |______||_| |_| \__, ||_||_| |_| \___|
		                                          __/ |                
		                                         |___/                          
		""";
		Console.WriteLine(title);
		Thread.Sleep(1000);
		Console.Clear();
	}
	public static void PrintSeparator() 
	{
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("---------------------------");
		Console.ForegroundColor = ConsoleColor.White;
	}
	public static void PrintInventory()
	{
		PrintSeparator();
		Console.ForegroundColor = ConsoleColor.DarkYellow;
		Console.WriteLine("Инвентарь:");
		foreach (var item in inventory)
		{
			Console.WriteLine("• " + item);
		}
		PrintSeparator();
	}

	#endregion
	#region LuaAPI
	public static void LuaPrint(string value, string color = "White") 
	{
		Console.ForegroundColor = Enum.Parse<ConsoleColor>(color);
		Console.WriteLine(value);
		Console.ForegroundColor = ConsoleColor.White;
	}
	public static void LuaClear() 
	{
		Console.Clear();
	}
	public static void LuaAddItem(string name)
	{
		if (inventory.Contains(name)) return;
		inventory.Add(name);
		Console.ForegroundColor = ConsoleColor.Magenta;
		Console.WriteLine($"Вы получили {name}");
	}
	public static void LuaAddOption(string id, int index,
		string f__text, 
		string f__tranfer_id, 
		string[] f__mandatory_items = null, 
		bool f__items_removed = false
	)
	{
		List<OptionJson> ol = instance.nnodes[GetNodeIndexById(id)].options.ToList();
		ol.Insert(index, new OptionJson
		{
			text = f__text,
			transfer_id = f__tranfer_id,
			mandatory_items = f__mandatory_items,
			items_removed = f__items_removed
		});
		instance.nnodes[GetNodeIndexById(id)].options = ol.ToArray();

	}
	public static void LuaRemoveOption(string id, int index) 
	{
		List<OptionJson> ol = instance.nnodes[GetNodeIndexById(id)].options.ToList();
		ol.RemoveAt(index);
		instance.nnodes[GetNodeIndexById(id)].options = ol.ToArray();

	}
	#endregion
}