using System;
using System.Collections.Generic;

namespace CathLab.Infrastructure.Pacs.Generated;

public partial class AttachedFile
{
    public long Id { get; set; }

    public int FileType { get; set; }

    public string Uuid { get; set; } = null!;

    public long? CompressedSize { get; set; }

    public long? UncompressedSize { get; set; }

    public int? CompressionType { get; set; }

    public string? UncompressedHash { get; set; }

    public string? CompressedHash { get; set; }

    public int? Revision { get; set; }

    public virtual Resource IdNavigation { get; set; } = null!;
}
