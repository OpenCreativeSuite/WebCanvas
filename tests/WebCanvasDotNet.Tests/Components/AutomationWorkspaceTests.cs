using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class AutomationWorkspaceTests
{
    [Fact]
    public void EmptyEventShowsExactEmptyState()
    {
        using var app = new TestApp();
        var page = app.Render<AutomationWorkspace>();

        Assert.Equal(StudioSession.EventLabel(app.Session.SelectedEvent), page.Find(".handler-list-title").InnerText);
        Assert.Equal("0 handlers", page.Find(".handler-list-count").InnerText);
        Assert.StartsWith("Nothing listens to this event yet.", page.Find(".handler-list-empty").InnerText);
    }

    [Fact]
    public void NewHandlerCreatesTheExpectedHandlerAndEditor()
    {
        using var app = new TestApp();
        var page = app.Render<AutomationWorkspace>();

        page.Click(page.FindAll(".btn").First(button => button.InnerText.Trim() == "New handler"));

        var handler = Assert.Single(app.Project.Handlers);
        Assert.Equal("New entity created handler", handler.Name);
        Assert.Equal("entity.created", handler.Event);
        Assert.True(handler.Enabled);
        Assert.Equal(handler.Id, app.Session.SelectedHandlerId);
        Assert.Single(handler.Actions);
        Assert.Equal("log", handler.Actions[0].Type);
        Assert.Equal("Handler ran.", handler.Actions[0].Value);

        page = app.Render<AutomationWorkspace>();
        Assert.Equal(handler.Name, page.Find(".handler-row.is-selected .handler-row-title").InnerText);
        Assert.Equal("1 action", page.Find(".handler-row.is-selected .handler-row-sub").InnerText);
        Assert.Equal(handler.Name, page.Find(".handler-name-input").Value);
        Assert.Contains("Enabled", page.Find(".handler-header-meta .toggle").InnerText);
    }

    [Fact]
    public void ActionsConditionsAndTestRunMutateState()
    {
        using var app = new TestApp();
        app.Session.NewHandler(app.Session.SelectedEvent);
        var handler = app.Project.Handlers.Single();
        var page = app.Render<AutomationWorkspace>();

        page.Click(page.FindAll(".btn").First(button => button.InnerText.Trim() == "Add action"));
        Assert.Equal(2, handler.Actions.Count);

        page.Click(page.FindAll(".btn").First(button => button.InnerText.Trim() == "Add condition"));
        Assert.Single(handler.Conditions);

        page.Click(page.FindAll(".btn").First(button => button.InnerText.Trim() == "Test run"));
        Assert.Equal("Finished 2 actions.", app.Session.AutomationLog[0].Message);
        Assert.Equal(handler.Name, app.Session.AutomationLog[0].Handler);
        Assert.Contains(app.Session.AutomationLog, entry => entry.Message == "Handler ran.");
    }
}

