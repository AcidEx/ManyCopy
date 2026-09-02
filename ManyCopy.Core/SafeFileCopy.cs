using System.Security.Cryptography;

namespace ManyCopy.Core;

public enum CopyDisposition
{
    Copied,
    SkippedExisting,
    SkippedIdentical,
}

public sealed record CopyReceipt(
    string Source,
    string Destination,
    bool ReplacedExisting,
    string? BackupPath,
    string AppliedSha256,
    string? OriginalSha256);

public sealed record CopyResult(CopyDisposition Disposition, CopyReceipt? Receipt);

public static class SafeFileCopy
{
    public static CopyResult Execute(
        string source,
        string destination,
        bool overwrite,
        string? expectedSourceSha256 = null,
        string? knownSourceSha256 = null,
        int retries = 3,
        int retryDelayMilliseconds = 120)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        source = Path.GetFullPath(source);
        destination = Path.GetFullPath(destination);

        if (!File.Exists(source))
        {
            throw new FileNotFoundException("Source file not found.", source);
        }

        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
        {
            return new CopyResult(CopyDisposition.SkippedIdentical, null);
        }

        string destinationDirectory = Path.GetDirectoryName(destination)
            ?? throw new IOException("The destination does not have a parent directory.");
        if (!Directory.Exists(destinationDirectory))
        {
            throw new DirectoryNotFoundException($"Destination folder not found: {destinationDirectory}");
        }

        string sourceHash = knownSourceSha256 ?? ComputeSha256(source);
        if (expectedSourceSha256 is not null &&
            !string.Equals(sourceHash, expectedSourceSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("The source file changed after the original copy and cannot be redone safely.");
        }

        bool hadExisting = File.Exists(destination);
        string? originalHash = null;
        if (hadExisting)
        {
            originalHash = ComputeSha256(destination);
            if (string.Equals(sourceHash, originalHash, StringComparison.OrdinalIgnoreCase))
            {
                return new CopyResult(CopyDisposition.SkippedIdentical, null);
            }

            if (!overwrite)
            {
                return new CopyResult(CopyDisposition.SkippedExisting, null);
            }
        }

        string temporaryPath = Path.Combine(destinationDirectory, $".manycopy-{Guid.NewGuid():N}.tmp");
        string? backupPath = hadExisting
            ? Path.Combine(destinationDirectory, $".manycopy-undo-{Guid.NewGuid():N}.bak")
            : null;
        bool destinationWasReplaced = false;
        bool committed = false;

        try
        {
            CopyToTemporaryFile(source, temporaryPath, retries, retryDelayMilliseconds);
            if (!string.Equals(ComputeSha256(temporaryPath), sourceHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("The temporary copy did not match the source file.");
            }

            if (hadExisting)
            {
                File.Copy(destination, backupPath!, overwrite: false);
                if (!string.Equals(ComputeSha256(backupPath!), originalHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException("The overwrite backup did not match the original destination file.");
                }

                if (!TryFileMatchesHash(destination, originalHash!))
                {
                    throw new IOException("The destination changed while the copy was being prepared. The overwrite was stopped.");
                }
            }

            File.Move(temporaryPath, destination, overwrite: hadExisting);
            destinationWasReplaced = true;
            if (!TryFileMatchesHash(destination, sourceHash))
            {
                throw new IOException("The completed destination did not match the source file.");
            }

            committed = true;

            var receipt = new CopyReceipt(
                source,
                destination,
                hadExisting,
                backupPath,
                sourceHash,
                originalHash);
            return new CopyResult(CopyDisposition.Copied, receipt);
        }
        catch (Exception copyError)
        {
            if (destinationWasReplaced && hadExisting && backupPath is not null && File.Exists(backupPath))
            {
                if (!TryRestoreOriginal(destination, backupPath, originalHash!))
                {
                    throw new IOException(
                        $"Copy failed and the original could not be restored automatically. Recovery backup: {backupPath}",
                        copyError);
                }

                TryDeleteFile(backupPath);
            }

            throw;
        }
        finally
        {
            TryDeleteFile(temporaryPath);

            if (!committed && backupPath is not null && File.Exists(backupPath) &&
                TryFileMatchesHash(destination, originalHash!))
            {
                TryDeleteFile(backupPath);
            }
        }
    }

    public static void Undo(CopyReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        if (File.Exists(receipt.Destination) &&
            !TryFileMatchesHash(receipt.Destination, receipt.AppliedSha256))
        {
            throw new IOException("The destination changed after copying. Undo was stopped to protect the newer file.");
        }

        if (receipt.ReplacedExisting)
        {
            if (receipt.BackupPath is null || !File.Exists(receipt.BackupPath))
            {
                throw new IOException("The original-file backup is missing. Undo was stopped without deleting the destination.");
            }

            if (receipt.OriginalSha256 is null ||
                !TryFileMatchesHash(receipt.BackupPath, receipt.OriginalSha256))
            {
                throw new IOException("The original-file backup failed its integrity check. Undo was stopped.");
            }

            File.Move(receipt.BackupPath, receipt.Destination, overwrite: File.Exists(receipt.Destination));
            return;
        }

        if (File.Exists(receipt.Destination))
        {
            File.Delete(receipt.Destination);
        }
    }

    public static CopyResult Redo(CopyReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        if (receipt.ReplacedExisting)
        {
            if (receipt.OriginalSha256 is null ||
                !TryFileMatchesHash(receipt.Destination, receipt.OriginalSha256))
            {
                throw new IOException("The restored destination changed after undo. Redo was stopped to protect it.");
            }
        }
        else if (File.Exists(receipt.Destination))
        {
            throw new IOException("A new destination file appeared after undo. Redo was stopped to protect it.");
        }

        return Execute(
            receipt.Source,
            receipt.Destination,
            overwrite: receipt.ReplacedExisting,
            expectedSourceSha256: receipt.AppliedSha256);
    }

    public static void DeleteBackup(CopyReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (receipt.BackupPath is not null && File.Exists(receipt.BackupPath))
        {
            File.Delete(receipt.BackupPath);
        }
    }

    public static string ComputeSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static void CopyToTemporaryFile(string source, string temporaryPath, int retries, int retryDelayMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(retries, 1);

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                File.Copy(source, temporaryPath, overwrite: false);
                return;
            }
            catch (IOException) when (attempt < retries)
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }

                Thread.Sleep(retryDelayMilliseconds);
            }
        }
    }

    private static bool TryRestoreOriginal(string destination, string backupPath, string originalHash)
    {
        try
        {
            if (!TryFileMatchesHash(destination, originalHash))
            {
                File.Copy(backupPath, destination, overwrite: true);
            }

            return TryFileMatchesHash(destination, originalHash);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    private static bool TryFileMatchesHash(string path, string expectedHash)
    {
        try
        {
            return File.Exists(path) &&
                string.Equals(ComputeSha256(path), expectedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best-effort cleanup must not hide the original copy result.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup must not hide the original copy result.
        }
        catch (NotSupportedException)
        {
            // Best-effort cleanup must not hide the original copy result.
        }
        catch (System.Security.SecurityException)
        {
            // Best-effort cleanup must not hide the original copy result.
        }
    }
}
