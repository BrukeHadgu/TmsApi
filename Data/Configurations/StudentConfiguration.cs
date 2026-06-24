using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations;
public class StudentConfiguration : IEntityTypeConfiguration<Student>
{    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(s => s.RegistrationNumber) // unique index for RegistrationNumber
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.GPA)
            .HasPrecision(3, 2);  // e.g. 3.85 — 3 total digits, 2 after decimal

        // shadow property... exists in db but not on the c# class
        builder.Property<DateTime>("LastUpdated");

       // builder.Property(s => s.Version)   // concurrency token... detects simultaneous edits
        // .IsRowVersion();

        builder.Property<uint>("xmin")
        .HasColumnName("xmin")
        .ValueGeneratedOnAddOrUpdate()
        .IsConcurrencyToken();

        //soft delete filter.. students hidden from normal queries
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}

