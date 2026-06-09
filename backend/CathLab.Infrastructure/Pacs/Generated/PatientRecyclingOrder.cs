using System;
using System.Collections.Generic;

namespace CathLab.Infrastructure.Pacs.Generated;

public partial class PatientRecyclingOrder
{
    public long Seq { get; set; }

    public long PatientId { get; set; }

    public virtual Resource Patient { get; set; } = null!;
}
