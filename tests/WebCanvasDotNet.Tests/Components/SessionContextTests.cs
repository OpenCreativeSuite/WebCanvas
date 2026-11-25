using Donkeh.UILab;
using WebCanvasDotNet.Components;
using static Donkeh.UILab._;

namespace WebCanvasDotNet.Tests.Components;

public sealed class SessionContextTests
{
    [Fact]
    public void ComponentsCanReadTheSessionAndInvokeItsActions()
    {
        using var app = new TestApp();
        app.Session.Preferences.Dark = false;
        var page = app.Render<ContextProbe>();

        Assert.Equal(app.Project.Name + ":1", page.Find(".context-value").InnerText);

        page.Click(".context-probe button");
        Assert.True(app.Session.Dark);
    }

    private sealed class ContextProbe : Component
    {
        public override Node Render()
        {
            var session = UseContext(SessionContext.Value);
            return H("div", new Props { ClassName = "context-probe" },
                H("span", new Props { ClassName = "context-value" },
                    Text(session.Project!.Name + ":" + session.Project.Pages.Count)),
                H("button", new Props { OnClick = session.ToggleTheme }, Text("Theme")));
        }
    }
}
