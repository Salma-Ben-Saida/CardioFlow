using CathLab.Application.DTOs.Pacs;
using CathLab.Application.Interfaces;
using CathLab.Infrastructure.Pacs.Generated;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Pacs;

public class OrthancPacsReader : IPacsReader
{
    private readonly OrthancPacsContext _ctx;

    // DICOM tag numbers used as identifiers in Orthanc
    private const int TagGroupPatientId     = 0x0010;
    private const int TagElementPatientId   = 0x0020;
    private const int TagGroupAccession     = 0x0008;
    private const int TagElementAccession   = 0x0050;
    private const int TagGroupStudyUid      = 0x0020;
    private const int TagElementStudyUid    = 0x000D;

    // DICOM tag numbers for MainDicomTags
    private const int TagGroupPatientName   = 0x0010;
    private const int TagElementPatientName = 0x0010;
    private const int TagGroupBirthDate     = 0x0010;
    private const int TagElementBirthDate   = 0x0030;
    private const int TagGroupSex           = 0x0010;
    private const int TagElementSex         = 0x0040;
    private const int TagGroupStudyDate     = 0x0008;
    private const int TagElementStudyDate   = 0x0020;
    private const int TagGroupModality      = 0x0008;
    private const int TagElementModality    = 0x0060;

    public OrthancPacsReader(OrthancPacsContext ctx) => _ctx = ctx;

        public async Task<List<PacsStudyDto>> GetPendingStudiesAsync()
    {
        var studyResources = await _ctx.Resources
            .AsNoTracking()
            .Where(r => r.ResourceType == 1) // 1 = Study
            .ToListAsync();

        var ids = studyResources.Select(r => r.InternalId).ToList();

        var tags = (await _ctx.MainDicomTags.AsNoTracking()
                        .Where(t => ids.Contains(t.Id)).ToListAsync())
                   .ToLookup(t => t.Id);

        var identifiers = (await _ctx.DicomIdentifiers.AsNoTracking()
                        .Where(d => ids.Contains(d.Id)).ToListAsync())
                   .ToLookup(d => d.Id);

        return studyResources
            .Select(r => BuildDto(r.PublicId, tags[r.InternalId].ToList(), identifiers[r.InternalId].ToList()))
            .ToList();
    }
    private async Task<PacsStudyDto?> BuildDtoAsync(long internalId, string publicId)
    {
        var tags = await _ctx.MainDicomTags.Where(t => t.Id == internalId).ToListAsync();
        var identifiers = await _ctx.DicomIdentifiers.Where(d => d.Id == internalId).ToListAsync();
        return BuildDto(publicId, tags, identifiers);
    }

    private static PacsStudyDto BuildDto(
        string publicId, List<MainDicomTag> tags, List<DicomIdentifier> identifiers)
    {
        string? GetTag(int group, int element) =>
            tags.FirstOrDefault(t => t.TagGroup == group && t.TagElement == element)?.Value
            ?? identifiers.FirstOrDefault(d => d.TagGroup == group && d.TagElement == element)?.Value;

        var accession   = GetTag(TagGroupAccession,   TagElementAccession);
        var studyUid    = GetTag(TagGroupStudyUid,    TagElementStudyUid);
        var patientId   = GetTag(TagGroupPatientId,   TagElementPatientId);
        var patientName = GetTag(TagGroupPatientName, TagElementPatientName);
        var birthDate   = GetTag(TagGroupBirthDate,   TagElementBirthDate);
        var sex         = GetTag(TagGroupSex,         TagElementSex);
        var studyDate   = GetTag(TagGroupStudyDate,   TagElementStudyDate);
        var modality    = GetTag(TagGroupModality,    TagElementModality);

        return new PacsStudyDto
        {
            StudyInstanceUid = studyUid ?? publicId,
            AccessionNumber  = accession ?? publicId,
            PatientId        = patientId ?? string.Empty,
            PatientName      = patientName,
            PatientBirthDate = ParseDate(birthDate),
            PatientSex       = sex,
            StudyDate        = ParseDate(studyDate),
            Modality         = modality
        };
    }
    public async Task<PacsStudyDto?> GetStudyByAccessionAsync(string accessionNumber)
    {
        var identifier = await _ctx.DicomIdentifiers
            .FirstOrDefaultAsync(d =>
                d.TagGroup == TagGroupAccession &&
                d.TagElement == TagElementAccession &&
                d.Value == accessionNumber);

        if (identifier == null) return null;

        var resource = await _ctx.Resources
            .FirstOrDefaultAsync(r => r.InternalId == identifier.Id);

        if (resource == null) return null;

        return await BuildDtoAsync(resource.InternalId, resource.PublicId);
    }

    public async Task<List<PacsStudyDto>> GetPatientStudiesAsync(string patientId)
    {
        var patientIdentifier = await _ctx.DicomIdentifiers
            .FirstOrDefaultAsync(d =>
                d.TagGroup == TagGroupPatientId &&
                d.TagElement == TagElementPatientId &&
                d.Value == patientId);

        if (patientIdentifier == null) return new List<PacsStudyDto>();

        var patientResource = await _ctx.Resources
            .FirstOrDefaultAsync(r => r.InternalId == patientIdentifier.Id
                                   && r.ResourceType == 0); // 0 = Patient

        if (patientResource == null) return new List<PacsStudyDto>();

        var studyResources = await _ctx.Resources
            .Where(r => r.ParentId == patientResource.InternalId
                     && r.ResourceType == 1)
            .ToListAsync();

        var results = new List<PacsStudyDto>();
        foreach (var study in studyResources)
        {
            var dto = await BuildDtoAsync(study.InternalId, study.PublicId);
            if (dto != null) results.Add(dto);
        }
        return results;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 8) return null;
        if (DateOnly.TryParseExact(value, "yyyyMMdd", out var d)) return d;
        return null;
    }
}