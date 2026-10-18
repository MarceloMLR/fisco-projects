namespace FiscoProjects.Persistence;

/// <summary>A module context registered with <see cref="PersistenceServiceCollectionExtensions.AddModuleDbContext{TContext}"/>.</summary>
public sealed record ModuleDbContextRegistration(Type ContextType, string Schema);
