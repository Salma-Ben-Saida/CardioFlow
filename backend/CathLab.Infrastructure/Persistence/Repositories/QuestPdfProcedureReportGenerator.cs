using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CathLab.Infrastructure.Repositories;

/// <summary>
/// Renders a ProcedureReport to PDF using QuestPDF and saves it to disk
/// under a per-report folder. Swap the storage part for blob storage
/// (S3/Azure) if that's where reports should live long-term.
/// </summary>
public class QuestPdfProcedureReportGenerator : IPdfGenerator
{
    private readonly string _storageRoot;

    public QuestPdfProcedureReportGenerator(string storageRoot)
    {
        _storageRoot = storageRoot;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> GenerateAsync(ProcedureReport report)
    {
        Directory.CreateDirectory(_storageRoot);
        var fileName = $"{report.Id}.pdf";
        var fullPath = Path.Combine(_storageRoot, fileName);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Procedure Report").FontSize(18).Bold();
                    col.Item().Text($"Report ID: {report.Id}");
                    col.Item().Text($"Cath Study ID: {report.CathStudyId}");
                    col.Item().Text($"Generated: {report.ReportGeneratedAt:g}");
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text("Summary").FontSize(13).Bold();
                    col.Item().Text($"Approach: {report.Approach}");
                    col.Item().Text($"Catheter Used: {report.CatheterUsed}");
                    col.Item().Text($"Conclusion: {report.Conclusion}");

                    col.Item().PaddingTop(10).Text("Lesions & Treatments").FontSize(13).Bold();

                    foreach (var lesion in report.Lesions)
                    {
                        col.Item().Border(1).Padding(8).Column(lesionCol =>
                        {
                            lesionCol.Item().Text(
                                $"{lesion.Vessel} - Segment {lesion.SegmentNumber} " +
                                $"({lesion.StenosisPercent}% stenosis, {lesion.LesionType}, TIMI {lesion.TimiFlow})"
                            ).Bold();

                            lesionCol.Item().Text($"Treated: {(lesion.IsTreated ? "Yes" : "No")}");

                            foreach (var treatment in lesion.Treatments)
                            {
                                var line = treatment switch
                                {
                                    StentImplant s =>
                                        $"Stent: {s.StentDiameterMm}mm x {s.StentLengthMm}mm, " +
                                        $"landing zone {s.LandingZoneProximal}-{s.LandingZoneDistal}, " +
                                        $"post-dilation: {(s.PostDilationPerformed ? $"yes @ {s.PostDilationPressure} atm" : "no")}, " +
                                        $"TIMI after: {s.TimiFlowAfter}",
                                    BalloonDilation b =>
                                        $"Balloon: {b.BalloonDiameterMm}mm x {b.BalloonLengthMm}mm, " +
                                        $"{b.MaxPressureAtm} atm for {b.InflationDurationSec}s, " +
                                        $"{(b.IsPreDilation ? "pre-dilation" : b.IsPostDilation ? "post-dilation" : "standalone")}, " +
                                        $"TIMI after: {b.TimiFlowAfter}",
                                    _ => $"Treatment performed at {treatment.PerformedAt:g}"
                                };

                                lesionCol.Item().PaddingLeft(10).Text(line);

                                if (!string.IsNullOrWhiteSpace(treatment.Notes))
                                    lesionCol.Item().PaddingLeft(10).Text($"Notes: {treatment.Notes}").Italic();
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        // GeneratePdf is CPU-bound/synchronous; keeping it off the calling thread.
        await Task.Run(() => document.GeneratePdf(fullPath));

        return fullPath;
    }
}