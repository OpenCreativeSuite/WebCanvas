namespace WebCanvasDotNet.Components;

/// <summary>A positioned card in a relationship or flow graph.</summary>
public sealed class GraphNodeModel
{
    public string Id { get; set; } = "";

    public string Title { get; set; } = "";

    public string Subtitle { get; set; } = "";

    public string Icon { get; set; } = "";

    public string Badge { get; set; } = "";

    public string Tone { get; set; } = "neutral";

    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; } = 224;

    public double Height { get; set; } = 84;

    public double CenterX => X + Width / 2;

    public double CenterY => Y + Height / 2;
}

public sealed class GraphEdgeModel
{
    public string From { get; set; } = "";

    public string To { get; set; } = "";

    public string Label { get; set; } = "";
}

public sealed class GraphCanvasModel
{
    public double Width { get; set; } = 640;

    public double Height { get; set; } = 360;

    public List<GraphNodeModel> Nodes { get; } = new();

    public List<GraphEdgeModel> Edges { get; } = new();
}
