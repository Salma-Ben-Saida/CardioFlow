using System;
using System.Collections.Generic;
using CathLab.Infrastructure.Pacs.Generated;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Pacs;

public partial class OrthancPacsContext : DbContext
{
    public OrthancPacsContext(DbContextOptions<OrthancPacsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AttachedFile> AttachedFiles { get; set; }

    public virtual DbSet<Change> Changes { get; set; }

    public virtual DbSet<DeletedFile> DeletedFiles { get; set; }

    public virtual DbSet<DeletedResource> DeletedResources { get; set; }

    public virtual DbSet<DicomIdentifier> DicomIdentifiers { get; set; }

    public virtual DbSet<ExportedResource> ExportedResources { get; set; }

    public virtual DbSet<GlobalProperty> GlobalProperties { get; set; }

    public virtual DbSet<MainDicomTag> MainDicomTags { get; set; }

    public virtual DbSet<Metadata> Metadata { get; set; }

    public virtual DbSet<PatientRecyclingOrder> PatientRecyclingOrders { get; set; }

    public virtual DbSet<Resource> Resources { get; set; }

    public virtual DbSet<ServerProperty> ServerProperties { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Latin1_General_100_CI_AS_SC_UTF8");

        modelBuilder.Entity<AttachedFile>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.FileType }).HasName("PK__Attached__9D2A6B0704CB5C87");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FileType).HasColumnName("fileType");
            entity.Property(e => e.CompressedHash)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("compressedHash");
            entity.Property(e => e.CompressedSize).HasColumnName("compressedSize");
            entity.Property(e => e.CompressionType).HasColumnName("compressionType");
            entity.Property(e => e.Revision).HasColumnName("revision");
            entity.Property(e => e.UncompressedHash)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("uncompressedHash");
            entity.Property(e => e.UncompressedSize).HasColumnName("uncompressedSize");
            entity.Property(e => e.Uuid)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("uuid");

            entity.HasOne(d => d.IdNavigation).WithMany(p => p.AttachedFiles)
                .HasForeignKey(d => d.Id)
                .HasConstraintName("AttachedFiles1");
        });

        modelBuilder.Entity<Change>(entity =>
        {
            entity.HasKey(e => e.Seq).HasName("PK__Changes__DDDFBCBED8E4B2CA");

            entity.HasIndex(e => e.InternalId, "ChangesIndex");

            entity.Property(e => e.Seq).HasColumnName("seq");
            entity.Property(e => e.ChangeType).HasColumnName("changeType");
            entity.Property(e => e.Date)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("date");
            entity.Property(e => e.InternalId).HasColumnName("internalId");
            entity.Property(e => e.ResourceType).HasColumnName("resourceType");

            entity.HasOne(d => d.Internal).WithMany(p => p.Changes)
                .HasForeignKey(d => d.InternalId)
                .HasConstraintName("Changes1");
        });

        modelBuilder.Entity<DeletedFile>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.CompressedHash)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("compressedHash");
            entity.Property(e => e.CompressedSize).HasColumnName("compressedSize");
            entity.Property(e => e.CompressionType).HasColumnName("compressionType");
            entity.Property(e => e.FileType).HasColumnName("fileType");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Revision).HasColumnName("revision");
            entity.Property(e => e.UncompressedHash)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("uncompressedHash");
            entity.Property(e => e.UncompressedSize).HasColumnName("uncompressedSize");
            entity.Property(e => e.Uuid)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("uuid");
        });

        modelBuilder.Entity<DeletedResource>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("PK__DeletedR__F5DEDCBECA0E77ED");

            entity.Property(e => e.InternalId)
                .ValueGeneratedNever()
                .HasColumnName("internalId");
            entity.Property(e => e.PublicId)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("publicId");
            entity.Property(e => e.ResourceType).HasColumnName("resourceType");
        });

        modelBuilder.Entity<DicomIdentifier>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.TagGroup, e.TagElement }).HasName("PK__DicomIde__01319E85AC54B4F6");

            entity.HasIndex(e => e.Id, "DicomIdentifiersIndex1");

            entity.HasIndex(e => new { e.TagGroup, e.TagElement }, "DicomIdentifiersIndex2");

            entity.HasIndex(e => e.Value, "DicomIdentifiersIndexValues");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TagGroup).HasColumnName("tagGroup");
            entity.Property(e => e.TagElement).HasColumnName("tagElement");
            entity.Property(e => e.Value)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("value");

            entity.HasOne(d => d.IdNavigation).WithMany(p => p.DicomIdentifiers)
                .HasForeignKey(d => d.Id)
                .HasConstraintName("DicomIdentifiers1");
        });

        modelBuilder.Entity<ExportedResource>(entity =>
        {
            entity.HasKey(e => e.Seq).HasName("PK__Exported__DDDFBCBEAE4F4398");

            entity.Property(e => e.Seq).HasColumnName("seq");
            entity.Property(e => e.Date)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("date");
            entity.Property(e => e.PatientId)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("patientId");
            entity.Property(e => e.PublicId)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("publicId");
            entity.Property(e => e.RemoteModality)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("remoteModality");
            entity.Property(e => e.ResourceType).HasColumnName("resourceType");
            entity.Property(e => e.SeriesInstanceUid)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("seriesInstanceUid");
            entity.Property(e => e.SopInstanceUid)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("sopInstanceUid");
            entity.Property(e => e.StudyInstanceUid)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("studyInstanceUid");
        });

        modelBuilder.Entity<GlobalProperty>(entity =>
        {
            entity.HasKey(e => e.Property).HasName("PK__GlobalPr__0F89001B8908EFF0");

            entity.Property(e => e.Property)
                .ValueGeneratedNever()
                .HasColumnName("property");
            entity.Property(e => e.Value)
                .IsUnicode(false)
                .HasColumnName("value");
        });

        modelBuilder.Entity<MainDicomTag>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.TagGroup, e.TagElement }).HasName("PK__MainDico__01319E8576563A3F");

            entity.HasIndex(e => e.Id, "MainDicomTagsIndex");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TagGroup).HasColumnName("tagGroup");
            entity.Property(e => e.TagElement).HasColumnName("tagElement");
            entity.Property(e => e.Value)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("value");

            entity.HasOne(d => d.IdNavigation).WithMany(p => p.MainDicomTags)
                .HasForeignKey(d => d.Id)
                .HasConstraintName("MainDicomTags1");
        });

        modelBuilder.Entity<Metadata>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.Type }).HasName("PK__Metadata__BC2C6D1B2CCB98D9");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Revision).HasColumnName("revision");
            entity.Property(e => e.Value)
                .IsUnicode(false)
                .HasColumnName("value");

            entity.HasOne(d => d.IdNavigation).WithMany(p => p.Metadata)
                .HasForeignKey(d => d.Id)
                .HasConstraintName("Metadata1");
        });

        modelBuilder.Entity<PatientRecyclingOrder>(entity =>
        {
            entity.HasKey(e => e.Seq).HasName("PK__PatientR__DDDFBCBE65F97B71");

            entity.ToTable("PatientRecyclingOrder");

            entity.HasIndex(e => e.PatientId, "PatientRecyclingIndex");

            entity.Property(e => e.Seq).HasColumnName("seq");
            entity.Property(e => e.PatientId).HasColumnName("patientId");

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientRecyclingOrders)
                .HasForeignKey(d => d.PatientId)
                .HasConstraintName("PatientRecyclingOrder1");
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("PK__Resource__F5DEDCBE1EA307DF");

            entity.HasIndex(e => e.ParentId, "ChildrenIndex");

            entity.HasIndex(e => e.PublicId, "PublicIndex");

            entity.HasIndex(e => e.ResourceType, "ResourceTypeIndex");

            entity.Property(e => e.InternalId).HasColumnName("internalId");
            entity.Property(e => e.ParentId).HasColumnName("parentId");
            entity.Property(e => e.PublicId)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("publicId");
            entity.Property(e => e.ResourceType).HasColumnName("resourceType");
        });

        modelBuilder.Entity<ServerProperty>(entity =>
        {
            entity.HasKey(e => new { e.Server, e.Property }).HasName("PK__ServerPr__CC6060DC542A3B13");

            entity.Property(e => e.Server)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("server");
            entity.Property(e => e.Property).HasColumnName("property");
            entity.Property(e => e.Value)
                .IsUnicode(false)
                .HasColumnName("value");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
