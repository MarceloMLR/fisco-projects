using FiscoProjects.Tickets.Application;

namespace FiscoProjects.Tickets.Tests;

// Design: baixar-anexos (Chamados-2.1), the names in the .zip of all the attachments.
public sealed class TicketArchiveTests
{
    [Fact(DisplayName = "Nome repetido no .zip ganha o número antes da extensão")]
    public void RepeatedName_GetsACopyNumber()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string[] names = [.. new[] { "erro.png", "Erro.png", "erro.png", "leia-me", "leia-me" }
            .Select(name => ListTicketArchive.UniqueName(name, used))];

        names.Should().Equal("erro.png", "Erro (2).png", "erro (3).png", "leia-me", "leia-me (2)");
    }
}
