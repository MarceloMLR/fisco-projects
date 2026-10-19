using System.Text;
using FiscoProjects.Administration.Application;

namespace FiscoProjects.Administration.Tests;

// Spec: configuracao-da-instalacao, requirement "Logo da instalação".
public sealed class LogoFileTests
{
    [Fact(DisplayName = "PNG aceito pela assinatura")]
    public void Png_IsAccepted() =>
        LogoFile.Check(Logos.Png()).Should().Be(LogoCheck.Accepted(LogoFormat.Png));

    [Fact(DisplayName = "SVG aceito")]
    public void Svg_IsAccepted() =>
        LogoFile.Check(Logos.Svg()).Should().Be(LogoCheck.Accepted(LogoFormat.Svg));

    [Fact(DisplayName = "SVG com imagem embutida aceito")]
    public void SvgWithAnEmbeddedImage_IsAccepted() =>
        LogoFile.Check(Logos.Svg("<image href=\"data:image/png;base64,iVBORw0KGgo=\" width=\"10\" height=\"10\"/>"))
            .Should().Be(LogoCheck.Accepted(LogoFormat.Svg));

    [Fact(DisplayName = "Até 1 MB")]
    public void UpTo1Megabyte_IsAccepted_AndOneByteMoreIsRefused()
    {
        LogoFile.Check(Logos.Png(LogoFile.MaxBytes)).Format.Should().Be(LogoFormat.Png);
        LogoFile.Check(Logos.Png(LogoFile.MaxBytes + 1)).Refusal.Should().Be(LogoRefusal.TooLarge);
    }

    [Fact(DisplayName = "JPG recusado")]
    public void Jpg_IsRefused() =>
        LogoFile.Check(Logos.Jpg()).Refusal.Should().Be(LogoRefusal.Format);

    [Theory(DisplayName = "Outro conteúdo recusado")]
    [InlineData("texto qualquer")]
    [InlineData("<html><body>página</body></html>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect></svg>")]
    [InlineData("")]
    public void SomethingElse_IsRefused(string content) =>
        LogoFile.Check(Encoding.UTF8.GetBytes(content)).Refusal.Should().Be(LogoRefusal.Format);

    [Theory(DisplayName = "SVG com script recusado")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<SCRIPT>alert(1)</SCRIPT>")]
    [InlineData("<rect width=\"10\" height=\"10\" onload=\"alert(1)\"/>")]
    [InlineData("<rect width=\"10\" height=\"10\" OnClick=\"alert(1)\"/>")]
    [InlineData("<a href=\"javascript:alert(1)\"><rect width=\"10\" height=\"10\"/></a>")]
    [InlineData("<a xmlns:xlink=\"http://www.w3.org/1999/xlink\" xlink:href=\" java&#9;script:alert(1)\"><rect/></a>")]
    [InlineData("<foreignObject><div xmlns=\"http://www.w3.org/1999/xhtml\">texto</div></foreignObject>")]
    public void SvgThatCouldRunCode_IsRefused(string inside) =>
        LogoFile.Check(Logos.Svg(inside)).Refusal.Should().Be(LogoRefusal.Unsafe);

    [Fact(DisplayName = "SVG com DTD recusado")]
    public void SvgWithADtd_IsRefused()
    {
        var svg = "<?xml version=\"1.0\"?><!DOCTYPE svg [<!ENTITY x SYSTEM \"file:///etc/passwd\">]>"
            + "<svg xmlns=\"http://www.w3.org/2000/svg\"><text>&x;</text></svg>";

        LogoFile.Check(Encoding.UTF8.GetBytes(svg)).Refusal.Should().Be(LogoRefusal.Unsafe);
    }
}
