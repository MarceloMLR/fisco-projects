using FiscoProjects.SharedKernel;

namespace FiscoProjects.Testing.Samples;

public sealed class AlphaRecord : Entity
{
    public required string Name { get; set; }

    public DateTime OccurredAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public DateOnly Day { get; set; }
}

public sealed class BetaRecord : Entity
{
    public required string Title { get; set; }
}
