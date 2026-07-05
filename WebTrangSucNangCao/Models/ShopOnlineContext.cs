using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace WebTrangSucNangCao.Models;

public partial class ShopOnlineContext : IdentityDbContext<ApplicationUser>
{
    public ShopOnlineContext()
    {
    }

    public ShopOnlineContext(DbContextOptions<ShopOnlineContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderPro> OrderPros { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<Slider> Sliders { get; set; }

    public virtual DbSet<UserCart> UserCarts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                optionsBuilder.UseSqlServer(connectionString);
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Idcate).HasName("PK__Category__43A295268D38D424");

            entity.ToTable("Category");

            entity.Property(e => e.Idcate)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("IDCate");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Idcus).HasName("PK__Customer__91A99BACEA59EC71");

            entity.ToTable("Customer");

            entity.Property(e => e.Idcus).HasColumnName("IDCus");
            entity.Property(e => e.PhoneCus).HasMaxLength(15);
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderDet__3214EC27FBB15CDF");

            entity.ToTable("OrderDetail");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Idorder).HasColumnName("IDOrder");
            entity.Property(e => e.Idproduct).HasColumnName("IDProduct");
            entity.Property(e => e.VariantId).HasColumnName("VariantID");

            entity.HasOne(d => d.IdorderNavigation).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.Idorder)
                .HasConstraintName("FK__OrderDeta__IDOrd__6B24EA82");

            entity.HasOne(d => d.IdproductNavigation).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.Idproduct)
                .HasConstraintName("FK__OrderDeta__IDPro__6C190EBB");

            entity.HasOne(d => d.Variant).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.VariantId)
                .HasConstraintName("FK_OrderDetail_ProductVariants");
        });

        modelBuilder.Entity<OrderPro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderPro__3214EC276A71800D");

            entity.ToTable("OrderPro");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Idcus).HasColumnName("IDCus");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.IdcusNavigation).WithMany(p => p.OrderPros)
                .HasForeignKey(d => d.Idcus)
                .HasConstraintName("FK__OrderPro__IDCus__6D0D32F4");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6ED537555C8");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Category)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.Material).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.CategoryNavigation).WithMany(p => p.Products)
                .HasForeignKey(d => d.Category)
                .HasConstraintName("FK_Pro_Category");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId);

            entity.Property(e => e.ImageId).HasColumnName("ImageID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductImages_Products");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.VariantId);

            entity.Property(e => e.VariantId).HasColumnName("VariantID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.VariantName).HasMaxLength(100);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductVariants_Products");
        });

        modelBuilder.Entity<Slider>(entity =>
        {
            entity.Property(e => e.SliderId).HasColumnName("SliderID");
            entity.Property(e => e.Link).HasMaxLength(250);
            entity.Property(e => e.Title).HasMaxLength(250);
        });

        modelBuilder.Entity<UserCart>(entity =>
        {
            entity.Property(e => e.UserCartId).HasColumnName("UserCartID");
            entity.Property(e => e.UserId)
                .HasMaxLength(128)
                .HasColumnName("UserID");
            entity.Property(e => e.VariantId).HasColumnName("VariantID");

            entity.HasOne(d => d.Variant).WithMany(p => p.UserCarts)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserCarts_ProductVariants");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
