#pragma warning disable CS8618
#pragma warning disable IDE1006
using System.Text.Json.Serialization;

namespace TextEngine;

public class InstanceJson
{
	[JsonPropertyName("title")] public string Title { get; set; }
	[JsonPropertyName("nodes")] public NodeJson[] Nodes { get; set; }
	[JsonPropertyName("prescript")] public string PreScript { get; set; }
	[JsonPropertyName("postscript")] public string PostScript { get; set; }
	[JsonIgnore] public List<NodeJson> NodesList { get; set; }
}

public class NodeJson
{
	[JsonPropertyName("id")] public string Id { get; set; }
	[JsonPropertyName("text")] public string Text { get; set; }
	[JsonPropertyName("prescript")] public string PreScript { get; set; }
	[JsonPropertyName("postscript")] public string PostScript { get; set; }
	[JsonPropertyName("sound")] public string Sound { get; set; }
	[JsonPropertyName("ambient")] public string Ambient { get; set; }
	[JsonPropertyName("end")] public bool End { get; set; }
	[JsonPropertyName("options")] public OptionJson[] Options { get; set; }
}

public class OptionJson
{
	[JsonPropertyName("text")] public string Text { get; set; }
	[JsonPropertyName("transfer_id")] public string TransferId { get; set; }
	[JsonPropertyName("mandatory_items")] public string[] MandatoryItems { get; set; }
	[JsonPropertyName("items_removed")] public bool ItemsRemoved { get; set; }
}

public class I18nJson 
{

}
#pragma warning restore CS8618
#pragma warning restore IDE1006
