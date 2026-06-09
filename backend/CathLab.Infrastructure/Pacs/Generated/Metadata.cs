using System;
using System.Collections.Generic;

namespace CathLab.Infrastructure.Pacs.Generated;

public partial class Metadata
{
    public long Id { get; set; }

    public int Type { get; set; }

    public string? Value { get; set; }

    public int? Revision { get; set; }

    public virtual Resource IdNavigation { get; set; } = null!;
}
