#pragma warning disable CS8618
#pragma warning disable IDE1006
using System.Text.Json.Serialization;

namespace TextEngine;

public class InstanceJson
{
	public string title { get; set; }
	public NodeJson[] nodes { get; set; }
	public string prescript { get; set; }
	public string postscript { get; set; }
	[JsonIgnore] public List<NodeJson> nnodes { get; set; }
}

public class NodeJson
{
	public string id { get; set; }
	public string text { get; set; }
	public string prescript { get; set; }
	public string postscript { get; set; }
	public string sound { get; set; }
	public string ambient { get; set; }
	public bool end { get; set; }
	public OptionJson[] options { get; set; }
}

public class OptionJson
{
	public string text { get; set; }
	public string transfer_id { get; set; }
	public string[] mandatory_items { get; set; }
	public bool items_removed { get; set; }
}
#pragma warning restore CS8618
#pragma warning restore IDE1006
