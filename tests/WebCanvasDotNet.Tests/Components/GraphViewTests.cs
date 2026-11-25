using WebCanvasDotNet.Components;

namespace WebCanvasDotNet.Tests.Components;

public sealed class GraphViewTests
{
    [Fact]
    public void RendersExactNodesEdgesAndSelection()
    {
        using var app = new TestApp();
        string? selected = null;
        var graph = new GraphCanvasModel();
        graph.Nodes.Add(new GraphNodeModel { Id = "a", Title = "Article", Subtitle = "3 records", X = 10, Y = 10 });
        graph.Nodes.Add(new GraphNodeModel { Id = "b", Title = "Author", Subtitle = "1 record", X = 300, Y = 10 });
        graph.Edges.Add(new GraphEdgeModel { From = "a", To = "b", Label = "written by" });

        var page = app.Render<GraphView, GraphViewProps>(new GraphViewProps
        {
            Graph = graph,
            SelectedId = "a",
            OnSelect = id => selected = id,
        });

        Assert.Equal(["Article", "Author"], page.FindAll(".graph-node-title").Select(title => title.InnerText).ToArray());
        Assert.Equal(["3 records", "1 record"], page.FindAll(".graph-node-sub").Select(sub => sub.InnerText).ToArray());
        Assert.Single(page.FindAll(".graph-edge-label"));
        Assert.Equal("written by", page.Find(".graph-edge-label").InnerText);
        Assert.Single(page.FindAll(".graph-node.is-selected"));
        Assert.Contains("Article", page.Find(".graph-node.is-selected").InnerText);

        page.Click(page.FindByText("Author"));
        Assert.Equal("b", selected);
    }

    [Fact]
    public void EmptyGraphUsesTheExactEmptyMessage()
    {
        using var app = new TestApp();
        var page = app.Render<GraphView, GraphViewProps>(new GraphViewProps
        {
            EmptyText = "No relationships",
            EmptyHint = "Create a relation field first.",
        });

        Assert.Equal("No relationships", page.Find(".empty-title").InnerText);
        Assert.Equal("Create a relation field first.", page.Find(".empty-copy").InnerText);
    }
}
