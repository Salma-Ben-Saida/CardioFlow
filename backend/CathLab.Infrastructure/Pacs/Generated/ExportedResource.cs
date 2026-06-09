using System;
using System.Collections.Generic;

namespace CathLab.Infrastructure.Pacs.Generated;

public partial class ExportedResource
{
    public long Seq { get; set; }

    public int? ResourceType { get; set; }

    public string? PublicId { get; set; }

    public string? RemoteModality { get; set; }

    public string? PatientId { get; set; }

    public string? StudyInstanceUid { get; set; }

    public string? SeriesInstanceUid { get; set; }

    public string? SopInstanceUid { get; set; }

    public string? Date { get; set; }
}
