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

public enum GammaStatus
{
    Open = 1,
    Closed = 2,
}

public sealed class GammaRecord : Entity
{
    public required string Name { get; set; }

    public int Quantity { get; set; }

    public GammaStatus Status { get; set; }

    public bool Active { get; set; }

    public DateOnly? Due { get; set; }

    public string? Password { get; set; }

    public DateTime? LastSeenAt { get; set; }
}

public sealed class GammaNote : Entity
{
    public required string Text { get; set; }
}
