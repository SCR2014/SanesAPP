namespace Sanes.Application.Common.Files;

public class StoredFileContent
{
    public Stream Content { get; set; }
        = Stream.Null;

    public long FileSize { get; set; }
}