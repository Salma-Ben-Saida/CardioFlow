using CathLab.Application.DTOs.Procedures;
using CathLab.Application.Interfaces;
using CathLab.Application.Services;
using CathLab.Domain.Entities;
using CathLab.Domain.Repositories;
using Moq;
using Xunit;

public class ProcedureReportRulesTests
{
    private readonly Mock<IProcedureReportRepository> _reports = new();
    private readonly Mock<ICoronaryLesionRepository> _lesions = new();
    private readonly Mock<ILesionTreatmentRepository> _treatments = new();
    private readonly Mock<IInventoryItemRepository> _items = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPdfGenerator> _pdf = new();
    private readonly Mock<ICathStudyRepository> _studies = new();
    private readonly ProcedureService _sut;

    public ProcedureReportRulesTests()
    {
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(f => f());
        var inventory = new InventoryService(
            Mock.Of<IInventoryProductRepository>(), _items.Object,
            Mock.Of<IStockMovementRepository>(), Mock.Of<IUserRepository>());
        _sut = new ProcedureService(_reports.Object, _lesions.Object, _treatments.Object,
            inventory, _uow.Object, _pdf.Object, Mock.Of<IUserRepository>(), _studies.Object);
    }

    private static ProcedureReport Confirmed() => new() { Id = Guid.NewGuid(), IsConfirmed = true };

    [Fact]
    public async Task Doctor_cannot_edit_confirmed_report()
    {
        var r = Confirmed();
        _reports.Setup(x => x.FindByIdAsync(r.Id)).ReturnsAsync(r);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateReportNarrativeAsync(r.Id, new UpdateReportNarrativeDto { Conclusion = "x" }, isAdmin: false));
    }

    [Fact]
    public async Task Admin_edit_of_confirmed_report_regenerates_pdf()
    {
        var r = Confirmed();
        _reports.Setup(x => x.FindByIdAsync(r.Id)).ReturnsAsync(r);
        _lesions.Setup(x => x.GetByProcedureReportAsync(r.Id)).ReturnsAsync(new List<CoronaryLesion>());
        _pdf.Setup(p => p.GenerateAsync(It.IsAny<ProcedureReport>(), It.IsAny<PatientHeader?>())).ReturnsAsync("new.pdf");

        await _sut.UpdateReportNarrativeAsync(r.Id, new UpdateReportNarrativeDto { Conclusion = "corrigé" }, isAdmin: true);

        Assert.Equal("corrigé", r.Conclusion);
        Assert.Equal("new.pdf", r.PdfStoragePath);
        _pdf.Verify(p => p.GenerateAsync(r, It.IsAny<PatientHeader?>()), Times.Once);
    }

    [Fact]
    public async Task Deleting_draft_releases_reserved_devices()
    {
        var r = new ProcedureReport { Id = Guid.NewGuid(), CathStudyId = Guid.NewGuid(), IsConfirmed = false };
        var item = new InventoryItem { Id = Guid.NewGuid(), Status = ItemStatus.Reserved };
        var lesion = new CoronaryLesion
        {
            Id = Guid.NewGuid(),
            Treatments = new List<LesionTreatment> { new StentImplant { InventoryItemId = item.Id } }
        };
        _reports.Setup(x => x.FindByIdAsync(r.Id)).ReturnsAsync(r);
        _lesions.Setup(x => x.GetByProcedureReportAsync(r.Id)).ReturnsAsync(new List<CoronaryLesion> { lesion });
        _items.Setup(x => x.FindByIdAsync(item.Id)).ReturnsAsync(item);

        await _sut.DeleteReportAsync(r.Id);

        Assert.Equal(ItemStatus.InStock, item.Status);
        _reports.Verify(x => x.DeleteAsync(r.Id), Times.Once);
    }

    [Fact]
    public async Task Deleting_confirmed_report_keeps_devices_consumed()
    {
        var r = Confirmed();
        _reports.Setup(x => x.FindByIdAsync(r.Id)).ReturnsAsync(r);
        _lesions.Setup(x => x.GetByProcedureReportAsync(r.Id)).ReturnsAsync(new List<CoronaryLesion>());

        await _sut.DeleteReportAsync(r.Id);

        _items.Verify(x => x.UpdateAsync(It.IsAny<InventoryItem>()), Times.Never);
        _reports.Verify(x => x.DeleteAsync(r.Id), Times.Once);
    }
}