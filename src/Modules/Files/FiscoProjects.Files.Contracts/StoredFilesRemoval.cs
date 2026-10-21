namespace FiscoProjects.Files.Contracts;

/// <summary>
/// Removes what a module stored, as when a project is deleted (ADR-0041, ADR-0111): the files, with their versions, and
/// then the folders left empty, one by one up to the root, never the root nor a folder with anything in it, so what the
/// system did not create stays. A file or folder that is already gone is skipped, so running it again finishes it.
/// </summary>
public static class StoredFilesRemoval
{
    /// <summary>Removes the files and then the folders, theirs and the ones given, that were left empty.</summary>
    /// <param name="files">The storage.</param>
    /// <param name="fileIds">The files the module registered.</param>
    /// <param name="folderIds">The folders the module registered, such as the folder of a ticket.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    /// <returns>How many files were removed.</returns>
    public static async Task<int> RemoveAsync(
        this IFileStorage files,
        IEnumerable<string> fileIds,
        IEnumerable<string> folderIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(fileIds);
        ArgumentNullException.ThrowIfNull(folderIds);
        var folders = new HashSet<string>(folderIds, StringComparer.Ordinal);
        var removed = 0;
        foreach (var fileId in fileIds.Distinct(StringComparer.Ordinal))
        {
            if (await files.FolderOfAsync(fileId, cancellationToken) is { } folder)
            {
                folders.Add(folder);
            }

            if (await files.DeleteFileAsync(fileId, cancellationToken))
            {
                removed++;
            }
        }

        foreach (var folder in folders)
        {
            await RemoveEmptyAsync(files, folder, cancellationToken);
        }

        return removed;
    }

    /// <summary>Removes the folder while it is empty, and then the one above it, up to the root.</summary>
    private static async Task RemoveEmptyAsync(IFileStorage files, string folderId, CancellationToken cancellationToken)
    {
        for (string? current = folderId; current is not null;)
        {
            var above = await files.FolderOfAsync(current, cancellationToken);
            if (!await files.DeleteFolderIfEmptyAsync(current, cancellationToken))
            {
                return;
            }

            current = above;
        }
    }
}
