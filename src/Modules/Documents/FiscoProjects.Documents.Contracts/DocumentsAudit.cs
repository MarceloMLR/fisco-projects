namespace FiscoProjects.Documents.Contracts;

/// <summary>The types of the items of the Documents module that audit records point to (ADR-0084).</summary>
public static class DocumentsAudit
{
    /// <summary>The type of a field that holds the id of a folder of a project, shown on the Audit screen by its name.</summary>
    public const string FolderReference = "DocumentFolder";

    /// <summary>The type of a field that holds the id of a category of links, shown on the Audit screen by its name.</summary>
    public const string LinkCategoryReference = "LinkCategory";

    /// <summary>The type of a field that holds the id of an acceptance term, shown on the Audit screen by its title.</summary>
    public const string AcceptanceTermReference = "AcceptanceTerm";
}
