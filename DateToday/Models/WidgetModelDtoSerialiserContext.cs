using System.Text.Json.Serialization;

namespace DateToday.Models;

[JsonSerializable(typeof(WidgetModelDto))]
[JsonSerializable(typeof(WindowVertexIdentifier))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class WidgetModelDtoSerialiserContext : JsonSerializerContext;
