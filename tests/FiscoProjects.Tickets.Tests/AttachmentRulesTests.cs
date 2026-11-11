using FiscoProjects.Files.Contracts;

namespace FiscoProjects.Tickets.Tests;

// Spec: chamados, "Anexos do chamado" (ADR-0026).
public sealed class AttachmentRulesTests
{
    [Theory(DisplayName = "Anexos recusados")]
    [InlineData("nota.xml", 10, "<nfe/>", FileRefusal.None)]
    [InlineData("nota.xml", 0, "", FileRefusal.Empty)]
    [InlineData("nota.xml", 2048, "<nfe/>", FileRefusal.TooLarge)]
    [InlineData("setup.EXE", 10, "abc", FileRefusal.Dangerous)]
    [InlineData("script.ps1", 10, "abc", FileRefusal.Dangerous)]
    [InlineData("foto.png", 10, "MZ", FileRefusal.Dangerous)]
    public void Attachments_AreCheckedByNameSizeAndStart(string name, long size, string start, FileRefusal refusal) =>
        FileTypes.Check(name, size, System.Text.Encoding.ASCII.GetBytes(start), maxBytes: 1024).Should().Be(refusal);
}
