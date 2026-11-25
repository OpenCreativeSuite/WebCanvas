using WebCanvasDotNet.Components;
using WebCanvasDotNet.Core;

namespace WebCanvasDotNet.Tests.Components;

public sealed class IconsTests
{
    [Theory]
    [InlineData("h1", Icons.Heading)]
    [InlineData("p", Icons.AlignLeft)]
    [InlineData("a", Icons.Link)]
    [InlineData("button", Icons.Forward)]
    [InlineData("img", Icons.Image)]
    [InlineData("nav", Icons.Bars)]
    [InlineData("section", Icons.Layers)]
    [InlineData("blockquote", Icons.Quote)]
    [InlineData("collection", Icons.Data)]
    [InlineData("li", Icons.List)]
    [InlineData("div", Icons.Square)]
    [InlineData("custom-tag", Icons.Cube)]
    public void TagMappingsAreStable(string tag, string expected)
    {
        Assert.Equal(expected, Icons.ForTag(tag));
    }

    [Theory]
    [InlineData(FieldTypes.Text, Icons.Font)]
    [InlineData(FieldTypes.LongText, Icons.AlignLeft)]
    [InlineData(FieldTypes.Number, Icons.Hash)]
    [InlineData(FieldTypes.Boolean, Icons.Toggle)]
    [InlineData(FieldTypes.Date, Icons.Calendar)]
    [InlineData(FieldTypes.Select, Icons.List)]
    [InlineData(FieldTypes.Relation, Icons.Link)]
    [InlineData(FieldTypes.Image, Icons.Image)]
    [InlineData(FieldTypes.Url, Icons.Globe)]
    public void FieldMappingsAreStable(string type, string expected)
    {
        Assert.Equal(expected, Icons.ForField(type));
    }

    [Fact]
    public void NamedConstantsUseTheFontAwesomePrefix()
    {
        foreach (string icon in new[] { Icons.Heading, Icons.Link, Icons.Image, Icons.Bars, Icons.Delete, Icons.Eye })
        {
            Assert.StartsWith("fa-", icon);
        }
    }
}
