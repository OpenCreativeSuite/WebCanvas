using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Components;

/// <summary>Builds the event → handler → action map for the automation editor.</summary>
public static class AutomationFlow
{
    public static GraphCanvasModel Build(HandlerDoc handler)
    {
        var graph = new GraphCanvasModel();

        graph.Nodes.Add(new GraphNodeModel
        {
            Id = "event",
            Title = StudioSession.EventLabel(handler.Event),
            Subtitle = handler.EntityType is null ? "Project event" : handler.EntityType + " event",
            Icon = StudioSession.EventIcon(handler.Event),
            Badge = "when",
            Tone = "event",
            X = 10,
            Y = 24,
            Width = 190,
            Height = 72,
        });

        graph.Nodes.Add(new GraphNodeModel
        {
            Id = "handler",
            Title = handler.Name,
            Subtitle = handler.Conditions.Count + " conditions · " + (handler.Enabled ? "enabled" : "paused"),
            Icon = Icons.ListCheck,
            Badge = "handler",
            Tone = "handler",
            X = 294,
            Y = 24,
            Width = 250,
            Height = 72,
        });

        graph.Edges.Add(new GraphEdgeModel { From = "event", To = "handler", Label = "then" });

        double x = 646;
        string previous = "handler";

        for (int index = 0; index < handler.Actions.Count; index++)
        {
            ActionDoc action = handler.Actions[index];
            string id = "action_" + index;

            graph.Nodes.Add(new GraphNodeModel
            {
                Id = id,
                Title = ActionLabel(action.Type),
                Subtitle = ActionSummary(action),
                Icon = ActionIcon(action.Type),
                Badge = (index + 1).ToString(),
                Tone = "action",
                X = x,
                Y = 24,
                Width = 196,
                Height = 72,
            });

            graph.Edges.Add(new GraphEdgeModel { From = previous, To = id, Label = index == 0 ? "do" : "" });
            previous = id;
            x += 218;
        }

        graph.Width = Math.Max(720, x + 16);
        graph.Height = 144;
        return graph;
    }

    private static string ActionLabel(string type) => type switch
    {
        "log" => "Write to log",
        "notify" => "Notify",
        "email" => "Send email",
        "webhook" => "Call webhook",
        "set field" => "Set a field",
        "delay" => "Wait",
        _ => type,
    };

    private static string ActionSummary(ActionDoc action) => action.Type switch
    {
        "email" => action.Target ?? "Studio inbox",
        "webhook" => action.Target ?? "Configured endpoint",
        "delay" => (string.IsNullOrWhiteSpace(action.Value) ? "0" : action.Value) + " seconds",
        "set field" => string.IsNullOrWhiteSpace(action.Value) ? "field = value" : action.Value,
        _ => string.IsNullOrWhiteSpace(action.Value) ? "No message" : action.Value,
    };

    private static string ActionIcon(string type) => type switch
    {
        "email" => Icons.Mail,
        "webhook" => Icons.Globe,
        "notify" => Icons.Bell,
        "delay" => Icons.Clock,
        "set field" => Icons.Edit,
        "log" => Icons.Terminal,
        _ => Icons.Play,
    };
}
