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
		PrintIntro();

		while (true)
		{
			//Prepare node
			NodeJson node = instance.NodesList[nodePointer];
			List<int> unavailableOptions = new();
			sound = new();

			if (node.Id == null) throw new InvalidOperationException("[Node] Id is null!");
			if (!node.End && node.Options == null) throw new InvalidOperationException("[Node] Options is null!");
			if (node.Text == null) throw new InvalidOperationException("[Node] Text is null!");

			if (node.Sound != null)
			{
				PlaySound(node.Sound);
			}
			if (node.Ambient != null) 
			{
				PlaySound(node.Ambient, true);
			}

			//Call lua
			CallLuaFunction(instance.PreScript);
			CallLuaFunction(node.PreScript);

			Console.WriteLine(node.Text);

			CallLuaFunction(instance.PostScript);
			CallLuaFunction(node.PostScript);

			if (node.End) break;

			PrintInventory();

			for (int i = 0; i < node.Options.Length; i++)
			{
				OptionJson option = node.Options[i];

				if (option.Text == null) throw new InvalidOperationException("[Option] Text is null!");
				if (option.TransferId == null) throw new InvalidOperationException("[Option] Transfer id is null!");

				if (option.MandatoryItems != null)
				{
					if (CanChooseOption(option.MandatoryItems))
					{
						Console.ForegroundColor = ConsoleColor.Green;
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
						unavailableOptions.Add(i);
					}
				}
				Console.WriteLine($"{i + 1}) {option.Text}");
				Console.ForegroundColor = ConsoleColor.White;
			}
			PrintSeparator();

		ask:
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write(":> ");
			Console.ForegroundColor = ConsoleColor.White;
			string userInput = Console.ReadLine();
			int intUserOption;
			if (
				!int.TryParse(userInput, out intUserOption) ||
				intUserOption < 1 ||
				unavailableOptions.Contains(intUserOption) ||
				--intUserOption >= node.Options.Length
				) 
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Неверный ввод!");
				Console.ForegroundColor = ConsoleColor.White;
				goto ask;
			}

			OptionJson userСhoice = node.Options[intUserOption];
			nodePointer = GetNodeIndexById(userСhoice.TransferId);

			if (userСhoice.ItemsRemoved && userСhoice.MandatoryItems != null)
			{
				inventory.RemoveAll(t => userСhoice.MandatoryItems.Contains(t));
			}

			Console.Clear();
			sound.Stop();
		}

		Console.ReadKey();
	}



	public static void Load()
	{
#pragma warning disable CS8601
#pragma warning disable CS8602
		//Load instance.json
		instance = JsonSerializer.Deserialize<InstanceJson>(File.ReadAllText(Constants.INSTANCE_PATH));
		instance.NodesList = instance.Nodes.ToList();
		if (instance.Title == null) throw new InvalidOperationException("[Instance] Title is null!");

		//Load script.lua
		lua = new();
		lua.State.Encoding = Encoding.UTF8;
		lua.LoadCLRPackage();

		lua.RegisterFunction("te_addItem", typeof(Program).GetMethod(nameof(LuaAddItem)));
		lua.RegisterFunction("te_removeItem", typeof(Program).GetMethod(nameof(LuaRemoveItem)));

		lua.RegisterFunction("te_setSound", typeof(Program).GetMethod(nameof(PlaySound)));

		lua.RegisterFunction("te_addOption", typeof(Program).GetMethod(nameof(LuaAddOption)));
		lua.RegisterFunction("te_removeOption", typeof(Program).GetMethod(nameof(LuaRemoveOption)));

		lua.RegisterFunction("te_clear", typeof(Program).GetMethod(nameof(LuaClear)));
		lua.RegisterFunction("te_print", typeof(Program).GetMethod(nameof(LuaPrint)));
		lua.RegisterFunction("te_printSeparator", typeof(Program).GetMethod(nameof(PrintSeparator)));

		lua.DoFile(Constants.SCRIPT_PATH);

		//Init console
		Console.Title = instance.Title;
		Console.ForegroundColor = ConsoleColor.White;
		Console.OutputEncoding = Encoding.UTF8;
#pragma warning restore CS8601
#pragma warning restore CS8602
	}

	#region Utilz
	public static int GetNodeIndexById(string id) 
	{
		return instance.NodesList.FindIndex(x => x.Id == id);
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
	public static void PrintIntro()
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
	public static void LuaRemoveItem(string name, bool lost = false)
	{
		if (!inventory.Contains(name)) return;
		inventory.Remove(name);
		Console.ForegroundColor = ConsoleColor.Magenta;
		string message = lost ? "потеряли" : "использовали";
		Console.WriteLine($"Вы {message} {name}");
	}
	public static void LuaAddOption(string id, int index,
		string f__text, 
		string f__tranfer_id, 
		string[] f__mandatory_items = null, 
		bool f__items_removed = false
	)
	{
		List<OptionJson> ol = instance.NodesList[GetNodeIndexById(id)].Options.ToList();
		ol.Insert(index, new OptionJson
		{
			Text = f__text,
			TransferId = f__tranfer_id,
			MandatoryItems = f__mandatory_items,
			ItemsRemoved = f__items_removed
		});
		instance.NodesList[GetNodeIndexById(id)].Options = ol.ToArray();

	}
	public static void LuaRemoveOption(string id, int index) 
	{
		List<OptionJson> ol = instance.NodesList[GetNodeIndexById(id)].Options.ToList();
		ol.RemoveAt(index);
		instance.NodesList[GetNodeIndexById(id)].Options = ol.ToArray();

	}
	#endregion
}