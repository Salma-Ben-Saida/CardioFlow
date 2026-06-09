using System;
using System.Collections.Generic;

namespace CathLab.Infrastructure.Pacs.Generated;

public partial class Resource
{
    public long InternalId { get; set; }

    public int ResourceType { get; set; }

    public string PublicId { get; set; } = null!;

    public long? ParentId { get; set; }

    public virtual ICollection<AttachedFile> AttachedFiles { get; set; } = new List<AttachedFile>();

    public virtual ICollection<Change> Changes { get; set; } = new List<Change>();

    public virtual ICollection<DicomIdentifier> DicomIdentifiers { get; set; } = new List<DicomIdentifier>();

    public virtual ICollection<MainDicomTag> MainDicomTags { get; set; } = new List<MainDicomTag>();

    public virtual ICollection<Metadata> Metadata { get; set; } = new List<Metadata>();

    public virtual ICollection<PatientRecyclingOrder> PatientRecyclingOrders { get; set; } = new List<PatientRecyclingOrder>();
}
