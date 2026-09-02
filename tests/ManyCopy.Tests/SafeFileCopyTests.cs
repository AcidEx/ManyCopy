using ManyCopy.Core;
using Xunit;

namespace ManyCopy.Tests;

public sealed class SafeFileCopyTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ManyCopy.Tests-{Guid.NewGuid():N}");

    public SafeFileCopyTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void ExecuteAndUndo_NewFile_RemovesCopiedFile()
    {
        string source = WriteFile("source.txt", "new content");
        string destination = Path.Combine(_root, "destination.txt");

        CopyResult result = SafeFileCopy.Execute(source, destination, overwrite: false);

        Assert.Equal(CopyDisposition.Copied, result.Disposition);
        Assert.Equal("new content", File.ReadAllText(destination));

        SafeFileCopy.Undo(Assert.IsType<CopyReceipt>(result.Receipt));

        Assert.False(File.Exists(destination));
    }

    [Fact]
    public void ExecuteAndUndo_Overwrite_RestoresOriginalFile()
    {
        string source = WriteFile("source.txt", "replacement");
        string destination = WriteFile("destination.txt", "original");

        CopyResult result = SafeFileCopy.Execute(source, destination, overwrite: true);
        CopyReceipt receipt = Assert.IsType<CopyReceipt>(result.Receipt);

        Assert.True(receipt.ReplacedExisting);
        Assert.NotNull(receipt.BackupPath);
        Assert.True(File.Exists(receipt.BackupPath));
        Assert.Equal("replacement", File.ReadAllText(destination));

        SafeFileCopy.Undo(receipt);

        Assert.Equal("original", File.ReadAllText(destination));
        Assert.False(File.Exists(receipt.BackupPath));
    }

    [Fact]
    public void Execute_SameLengthAndTimestampButDifferentContent_DoesNotSkip()
    {
        string source = WriteFile("source.txt", "AAAA");
        string destination = WriteFile("destination.txt", "BBBB");
        DateTime timestamp = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(source, timestamp);
        File.SetLastWriteTimeUtc(destination, timestamp);

        CopyResult result = SafeFileCopy.Execute(source, destination, overwrite: true);

        Assert.Equal(CopyDisposition.Copied, result.Disposition);
        Assert.Equal("AAAA", File.ReadAllText(destination));
        SafeFileCopy.DeleteBackup(Assert.IsType<CopyReceipt>(result.Receipt));
    }

    [Fact]
    public void Execute_ExistingFileWithoutOverwrite_SkipsWithoutChangingIt()
    {
        string source = WriteFile("source.txt", "replacement");
        string destination = WriteFile("destination.txt", "original");

        CopyResult result = SafeFileCopy.Execute(source, destination, overwrite: false);

        Assert.Equal(CopyDisposition.SkippedExisting, result.Disposition);
        Assert.Null(result.Receipt);
        Assert.Equal("original", File.ReadAllText(destination));
    }

    [Fact]
    public void Execute_IdenticalContent_SkipsUsingContentHash()
    {
        string source = WriteFile("source.txt", "identical content");
        string destination = WriteFile("destination.txt", "identical content");
        File.SetLastWriteTimeUtc(source, DateTime.UtcNow.AddDays(-2));
        File.SetLastWriteTimeUtc(destination, DateTime.UtcNow);

        CopyResult result = SafeFileCopy.Execute(source, destination, overwrite: true);

        Assert.Equal(CopyDisposition.SkippedIdentical, result.Disposition);
        Assert.Null(result.Receipt);
    }

    [Fact]
    public void Undo_ChangedDestination_StopsAndPreservesBothFiles()
    {
        string source = WriteFile("source.txt", "replacement");
        string destination = WriteFile("destination.txt", "original");
        CopyReceipt receipt = Assert.IsType<CopyReceipt>(
            SafeFileCopy.Execute(source, destination, overwrite: true).Receipt);
        File.WriteAllText(destination, "newer external change");

        IOException error = Assert.Throws<IOException>(() => SafeFileCopy.Undo(receipt));

        Assert.Contains("changed", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("newer external change", File.ReadAllText(destination));
        Assert.True(File.Exists(receipt.BackupPath));
        Assert.Equal("original", File.ReadAllText(receipt.BackupPath!));
    }

    [Fact]
    public void Undo_MissingBackup_StopsWithoutDeletingDestination()
    {
        string source = WriteFile("source.txt", "replacement");
        string destination = WriteFile("destination.txt", "original");
        CopyReceipt receipt = Assert.IsType<CopyReceipt>(
            SafeFileCopy.Execute(source, destination, overwrite: true).Receipt);
        File.Delete(receipt.BackupPath!);

        Assert.Throws<IOException>(() => SafeFileCopy.Undo(receipt));

        Assert.Equal("replacement", File.ReadAllText(destination));
    }

    [Fact]
    public void Redo_ReappliesOnlyWhenSourceAndDestinationAreUnchanged()
    {
        string source = WriteFile("source.txt", "replacement");
        string destination = WriteFile("destination.txt", "original");
        CopyReceipt firstReceipt = Assert.IsType<CopyReceipt>(
            SafeFileCopy.Execute(source, destination, overwrite: true).Receipt);
        SafeFileCopy.Undo(firstReceipt);

        CopyReceipt redoReceipt = Assert.IsType<CopyReceipt>(SafeFileCopy.Redo(firstReceipt).Receipt);

        Assert.Equal("replacement", File.ReadAllText(destination));
        SafeFileCopy.Undo(redoReceipt);
        Assert.Equal("original", File.ReadAllText(destination));

        File.WriteAllText(source, "changed source");
        Assert.Throws<IOException>(() => SafeFileCopy.Redo(firstReceipt));
        Assert.Equal("original", File.ReadAllText(destination));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // Test cleanup must not hide the assertion result.
        }
    }

    private string WriteFile(string name, string content)
    {
        string path = Path.Combine(_root, name);
        File.WriteAllText(path, content);
        return path;
    }
}
