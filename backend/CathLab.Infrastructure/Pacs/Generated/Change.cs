using System;
using System.Collections.Generic;

namespace CathLab.Infrastructure.Pacs.Generated;

public partial class Change
{
    public long Seq { get; set; }

    public int? ChangeType { get; set; }

    public long InternalId { get; set; }

    public int? ResourceType { get; set; }

    public string? Date { get; set; }

    public virtual Resource Internal { get; set; } = null!;
}
