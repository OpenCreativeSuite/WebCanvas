namespace WebCanvasDotNet.Core;

public sealed partial class StudioSession
{
    public List<AutomationLogEntry> AutomationLog { get; } = new();

    public string AutomationWelcome => "Handlers run in order, after the event that triggered them.";

    public void NewHandler(string eventId)
    {
        if (Project is null)
        {
            return;
        }

        var handler = new HandlerDoc
        {
            Name = "New " + EventLabel(eventId).ToLowerInvariant() + " handler",
            Event = eventId,
            Enabled = true,
            Actions =
            {
                new ActionDoc { Type = "log", Value = "Handler ran." },
            },
        };

        try
        {
            Project.Handlers.Add(handler);
            Project.SaveHandlers();
            SelectedEvent = eventId;
            SelectedHandlerId = handler.Id;
            OpenTab(StudioMode.Automation, eventId, EventLabel(eventId), "fa-bolt");
            Notify("Created a handler.", "success");
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void DeleteHandler(HandlerDoc handler)
    {
        AskConfirm(new ConfirmRequest
        {
            Title = "Delete “" + handler.Name + "”?",
            Message = "The handler and its actions are removed from the automation file.",
            Confirm = "Delete handler",
            OnConfirm = () =>
            {
                try
                {
                    Project!.Handlers.Remove(handler);

                    if (SelectedHandlerId == handler.Id)
                    {
                        SelectedHandlerId = HandlersFor(handler.Event).FirstOrDefault()?.Id;
                    }

                    Project.SaveHandlers();
                    Notify("Handler deleted.", "success");
                }
                catch (Exception exception)
                {
                    Notify(exception.Message, "error");
                }
            },
        });
    }

    public void SetHandler(HandlerDoc handler, Action<HandlerDoc> edit)
    {
        try
        {
            edit(handler);
            Project!.SaveHandlers();
            MarkChanged();
        }
        catch (Exception exception)
        {
            Notify(exception.Message, "error");
        }
    }

    public void AddCondition(HandlerDoc handler)
    {
        SetHandler(handler, target =>
        {
            var field = handler.EntityType is not null && Project?.Schema(handler.EntityType) is { } schema
                ? schema.Fields.FirstOrDefault()?.Name ?? ""
                : "";

            target.Conditions.Add(new ConditionDoc { Field = field, Operator = "is set" });
        });
    }

    public void RemoveCondition(HandlerDoc handler, ConditionDoc condition)
    {
        SetHandler(handler, target => target.Conditions.Remove(condition));
    }

    public void AddAction(HandlerDoc handler)
    {
        SetHandler(handler, target => target.Actions.Add(new ActionDoc { Type = "log", Value = "" }));
    }

    public void RemoveAction(HandlerDoc handler, ActionDoc action)
    {
        SetHandler(handler, target => target.Actions.Remove(action));
    }

    public void MoveAction(HandlerDoc handler, ActionDoc action, int direction)
    {
        var index = handler.Actions.IndexOf(action);
        var target = index + direction;

        if (index < 0 || target < 0 || target >= handler.Actions.Count)
        {
            return;
        }

        SetHandler(handler, value =>
        {
            (value.Actions[index], value.Actions[target]) = (value.Actions[target], value.Actions[index]);
        });
    }

    public void RunHandler(HandlerDoc handler)
    {
        AutomationLog.Insert(0, new AutomationLogEntry
        {
            Handler = handler.Name,
            Message = "Started a test run for " + EventLabel(handler.Event) + ".",
            Kind = "info",
        });

        var schema = handler.EntityType is not null ? Project?.Schema(handler.EntityType) : null;
        var sample = schema is not null ? Project?.EntityList(schema).FirstOrDefault() : null;

        if (handler.Conditions.Count > 0)
        {
            if (schema is null || sample is null)
            {
                AutomationLog.Insert(0, new AutomationLogEntry
                {
                    Handler = handler.Name,
                    Message = "Conditions were skipped because there is no sample record to test against.",
                    Kind = "warning",
                });
            }
            else
            {
                foreach (var condition in handler.Conditions)
                {
                    if (!ConditionPasses(schema, sample, condition))
                    {
                        AutomationLog.Insert(0, new AutomationLogEntry
                        {
                            Handler = handler.Name,
                            Message = "Stopped: " + condition.Field + " " + condition.Operator + " “" + condition.Value + "” was not true.",
                            Kind = "warning",
                        });

                        MarkChanged();
                        return;
                    }
                }
            }
        }

        foreach (var action in handler.Actions)
        {
            var message = action.Type switch
            {
                "log" => string.IsNullOrWhiteSpace(action.Value) ? "Logged an empty message." : action.Value,
                "notify" => "Notification: " + (string.IsNullOrWhiteSpace(action.Value) ? "handler ran" : action.Value),
                "email" => "Email queued for " + (action.Target ?? "the studio") + ".",
                "webhook" => "Webhook queued for " + (action.Target ?? "the configured endpoint") + ".",
                "delay" => "Waited " + (string.IsNullOrWhiteSpace(action.Value) ? "0" : action.Value) + " seconds.",
                "set field" => ApplySetField(schema, sample, action, handler.Name),
                _ => "Ran " + action.Type + ".",
            };

            AutomationLog.Insert(0, new AutomationLogEntry
            {
                Handler = handler.Name,
                Message = message,
                Kind = action.Type == "log" ? "info" : "success",
            });

            if (action.Type == "notify")
            {
                Notify(message, "info");
            }
        }

        AutomationLog.Insert(0, new AutomationLogEntry
        {
            Handler = handler.Name,
            Message = "Finished " + handler.Actions.Count + " action" + (handler.Actions.Count == 1 ? "" : "s") + ".",
            Kind = "success",
        });

        while (AutomationLog.Count > 60)
        {
            AutomationLog.RemoveAt(AutomationLog.Count - 1);
        }

        MarkChanged();
    }

    private string ApplySetField(DataSchema? schema, EntityDoc? entity, ActionDoc action, string handlerName)
    {
        if (schema is null || entity is null)
        {
            return "Skipped “set field” because there is no sample record.";
        }

        var parts = action.Value.Split('=', 2, StringSplitOptions.TrimEntries);

        if (parts.Length != 2 || schema.Field(parts[0]) is null)
        {
            return "Skipped “set field”: use field=value with an existing field name.";
        }

        entity.Set(parts[0], parts[1]);

        try
        {
            Project!.SaveEntity(schema, entity);
        }
        catch (Exception exception)
        {
            return "Couldn't save the test change: " + exception.Message;
        }

        return "Set " + parts[0] + " to “" + parts[1] + "” on the sample record.";
    }

    private static bool ConditionPasses(DataSchema schema, EntityDoc entity, ConditionDoc condition)
    {
        if (string.IsNullOrWhiteSpace(condition.Field))
        {
            return true;
        }

        var value = entity.Get(condition.Field).Trim();
        var expected = condition.Value.Trim();

        return condition.Operator switch
        {
            "equals" => value.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "is not" => !value.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "contains" => value.Contains(expected, StringComparison.OrdinalIgnoreCase),
            "is set" => value.Length > 0,
            "is empty" => value.Length == 0,
            "greater than" => decimal.TryParse(value, out var left)
                              && decimal.TryParse(expected, out var right)
                              && left > right,
            "less than" => decimal.TryParse(value, out var low)
                           && decimal.TryParse(expected, out var high)
                           && low < high,
            _ => true,
        };
    }

    public void ClearAutomationLog()
    {
        AutomationLog.Clear();
        MarkChanged();
    }

    public List<DataField> HandlerFields(HandlerDoc handler) =>
        handler.EntityType is not null && Project?.Schema(handler.EntityType) is { } schema
            ? schema.Fields
            : new List<DataField>();
}
