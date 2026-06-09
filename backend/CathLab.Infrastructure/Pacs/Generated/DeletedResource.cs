using System;
using System.Collections.Generic;

namespace CathLab.Infrastructure.Pacs.Generated;

public partial class DeletedResource
{
    public long InternalId { get; set; }

    public int ResourceType { get; set; }

    public string PublicId { get; set; } = null!;
}
