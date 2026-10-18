namespace FiscoProjects.SharedKernel;

/// <summary>
/// Base type for entities. Every entity gets a version 7 GUID when it is created (ADR-0061).
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.CreateVersion7();
}
