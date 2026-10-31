using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace FiscoProjects.Testing;

/// <summary>
/// Files of the MS Project in XML (MSPDI) for the tests of the import (ADR-0135), as the MS Project saves them with
/// "Save as", "XML": the summary of the whole project at level 0, then the lines with their levels, dates, percent
/// complete and links, the resources and the assignments.
/// </summary>
public static class MsProjectFiles
{
    public static readonly XNamespace Ns = "http://schemas.microsoft.com/project";

    /// <summary>A line of the file; a summary is a line followed by lines of a higher level.</summary>
    public sealed record Line(
        int Uid,
        int Level,
        string Name,
        DateOnly Start,
        DateOnly Finish,
        int Percent = 0,
        bool Milestone = false,
        bool Active = true,
        IReadOnlyList<Link>? Links = null);

    /// <summary>A link to the line it depends on: type 1 is "finish to start", 3 "start to start"; the lag in tenths of minutes.</summary>
    public sealed record Link(int Predecessor, int Type = 1, int Lag = 0);

    /// <summary>A resource: type 1 is work (a person), 0 material and 2 cost.</summary>
    public sealed record Resource(int Uid, string Name, string? Email = null, int Type = 1);

    /// <summary>The file, as bytes in UTF-8.</summary>
    public static byte[] Project(
        string? title,
        IReadOnlyList<Line> lines,
        IEnumerable<Resource>? resources = null,
        IEnumerable<(int Task, int Resource)>? assignments = null) =>
        Bytes(Document(title, lines, resources, assignments));

    /// <summary>A document as the bytes of a file, in UTF-8 with the declaration.</summary>
    public static byte[] Bytes(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        using var memory = new MemoryStream();
        document.Save(memory);
        return memory.ToArray();
    }

    /// <summary>A text as the bytes of a file, in UTF-8.</summary>
    public static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    /// <summary>The document of the file.</summary>
    public static XDocument Document(
        string? title,
        IReadOnlyList<Line> lines,
        IEnumerable<Resource>? resources = null,
        IEnumerable<(int Task, int Resource)>? assignments = null)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var start = lines.Count == 0 ? new DateOnly(2026, 10, 13) : lines.Min(line => line.Start);
        var finish = lines.Count == 0 ? start : lines.Max(line => line.Finish);
        var tasks = new List<XElement>
        {
            new(Ns + "Task",
                new XElement(Ns + "UID", 0),
                new XElement(Ns + "ID", 0),
                new XElement(Ns + "Name", title ?? "Projeto"),
                new XElement(Ns + "OutlineLevel", 0),
                new XElement(Ns + "Start", Moment(start, 8)),
                new XElement(Ns + "Finish", Moment(finish, 17)),
                new XElement(Ns + "Summary", 1)),
        };
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var summary = index + 1 < lines.Count && lines[index + 1].Level > line.Level;
            var task = new XElement(Ns + "Task",
                new XElement(Ns + "UID", line.Uid),
                new XElement(Ns + "ID", index + 1),
                new XElement(Ns + "Name", line.Name),
                new XElement(Ns + "OutlineLevel", line.Level),
                new XElement(Ns + "Start", Moment(line.Start, line.Milestone ? 17 : 8)),
                new XElement(Ns + "Finish", Moment(line.Finish, 17)),
                new XElement(Ns + "Milestone", line.Milestone ? 1 : 0),
                new XElement(Ns + "Summary", summary ? 1 : 0),
                new XElement(Ns + "PercentComplete", line.Percent));
            foreach (var link in line.Links ?? [])
            {
                task.Add(new XElement(Ns + "PredecessorLink",
                    new XElement(Ns + "PredecessorUID", link.Predecessor),
                    new XElement(Ns + "Type", link.Type),
                    new XElement(Ns + "CrossProject", 0),
                    new XElement(Ns + "LinkLag", link.Lag),
                    new XElement(Ns + "LagFormat", 7)));
            }

            task.Add(new XElement(Ns + "Active", line.Active ? 1 : 0));
            tasks.Add(task);
        }

        var project = new XElement(Ns + "Project",
            new XElement(Ns + "SaveVersion", 14),
            new XElement(Ns + "Name", "Cronograma.xml"));
        if (title is not null)
        {
            project.Add(new XElement(Ns + "Title", title));
        }

        project.Add(
            new XElement(Ns + "Tasks", tasks),
            new XElement(Ns + "Resources", (resources ?? []).Select(resource =>
            {
                var element = new XElement(Ns + "Resource",
                    new XElement(Ns + "UID", resource.Uid),
                    new XElement(Ns + "ID", resource.Uid),
                    new XElement(Ns + "Name", resource.Name),
                    new XElement(Ns + "Type", resource.Type));
                if (resource.Email is not null)
                {
                    element.Add(new XElement(Ns + "EmailAddress", resource.Email));
                }

                return element;
            })),
            new XElement(Ns + "Assignments", (assignments ?? []).Select((assignment, index) => new XElement(Ns + "Assignment",
                new XElement(Ns + "UID", index + 1),
                new XElement(Ns + "TaskUID", assignment.Task),
                new XElement(Ns + "ResourceUID", assignment.Resource),
                new XElement(Ns + "Units", 1)))));
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), project);
    }

    /// <summary>The file of the scenarios of the preview: a phase, a task from 13/10/2026 to 15/10/2026 at 40% with Carla, and a milestone after it.</summary>
    public static byte[] Configuration(string carlaEmail) => Project(
        "Implantação do ERP",
        [
            new Line(1, 1, "Configuração", new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 15)),
            new Line(2, 2, "Parametrizar regras de ICMS", new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 15), Percent: 40),
            new Line(3, 2, "Aceite da configuração", new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 15), Milestone: true, Links: [new Link(2)]),
        ],
        [new Resource(1, "Carla Mendes", carlaEmail)],
        [(2, 1)]);

    private static string Moment(DateOnly day, int hour) =>
        day.ToDateTime(new TimeOnly(hour, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
}
