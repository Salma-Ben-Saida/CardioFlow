using System;
using System.Collections.Generic;

namespace CathLab.Infrastructure.Pacs.Generated;

public partial class DicomIdentifier
{
    public long Id { get; set; }

    public int TagGroup { get; set; }

    public int TagElement { get; set; }

    public string? Value { get; set; }

    public virtual Resource IdNavigation { get; set; } = null!;
}
