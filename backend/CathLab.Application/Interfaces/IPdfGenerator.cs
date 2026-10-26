using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;
public record PatientHeader(string FullName, DateOnly? BirthDate, string? Sex, DateTime StudyDate, string AccessionNumber);

/// <summary>
/// Generates the final PDF report for a confirmed ProcedureReport.
/// Implementations are responsible for laying out the report content
/// (lesions, treatments, conclusion, etc.) and persisting the resulting
/// file, returning a storage path/URI that can be saved on the report.
/// </summary>
public interface IPdfGenerator
{
    /// <summary>
    /// Renders the report to PDF and stores it (disk, blob storage, etc.).
    /// Expects <paramref name="report"/> to already include its Lesions
    /// and each lesion's Treatments (i.e. loaded via a repository call
    /// that Includes that graph), since those are what gets rendered.
    /// </summary>
    /// <returns>A storage path or URI identifying the generated file.</returns>
    Task<string> GenerateAsync(ProcedureReport report, PatientHeader? patient = null);
}