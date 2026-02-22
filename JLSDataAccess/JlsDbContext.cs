using System;
using JLSDataModel.Models;
using JLSDataModel.Models.Adress;
using JLSDataModel.Models.Analytics;
using JLSDataModel.Models.Message;
using JLSDataModel.Models.Order;
using JLSDataModel.Models.Product;
using JLSDataModel.Models.User;
using JLSDataModel.Models.Website;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JLSDataAccess;

public class JlsDbContext(DbContextOptions<JlsDbContext> options)
    : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    public virtual DbSet<ReferenceCategory> ReferenceCategory { get; set; }

    public virtual DbSet<ReferenceItem> ReferenceItem { get; set; }

    public virtual DbSet<ReferenceLabel> ReferenceLabel { get; set; }

    public virtual DbSet<OrderInfo> OrderInfo { get; set; }
    public virtual DbSet<Product> Product { get; set; }

    public virtual DbSet<ProductPhotoPath> ProductPhotoPath { get; set; }

    public virtual DbSet<Adress> Adress { get; set; }

    public virtual DbSet<CustomerInfo> CustomerInfo { get; set; }

    public virtual DbSet<UserShippingAdress> UserShippingAdress { get; set; }
    public virtual DbSet<OrderProduct> OrderProduct { get; set; }

    public virtual DbSet<ProductComment> ProductComment { get; set; }

    public virtual DbSet<TokenModel> TokenModel { get; set; }

    public virtual DbSet<OrderInfoStatusLog> OrderInfoStatusLog { get; set; }

    public virtual DbSet<Message> Message { get; set; }

    public virtual DbSet<MessageDestination> MessageDestination { get; set; }
    public virtual DbSet<Remark> Remark { get; set; }
    public virtual DbSet<ShipmentInfo> ShipmentInfo { get; set; }
    public virtual DbSet<ProductFavorite> ProductFavorite { get; set; }

    public virtual DbSet<ExportConfiguration> ExportConfiguration { get; set; }
    public virtual DbSet<EmailTemplate> EmailTemplate { get; set; }
    public virtual DbSet<EmailToSend> EmailToSend { get; set; }
    public virtual DbSet<WebsiteSlide> WebsiteSlide { get; set; }
    public virtual DbSet<SubscribeEmail> SubscribeEmail { get; set; }
    public virtual DbSet<Dialog> Dialog { get; set; }
    public virtual DbSet<VisitorCounter> VisitorCounter { get; set; }
    public virtual DbSet<BestClientWidget> BestClientWidget { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BestClientWidget>().HasNoKey();
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            modelBuilder.Entity(entityType.ClrType).ToTable(tb => tb.UseSqlOutputClause(false));
        }

        base.OnModelCreating(modelBuilder);
    }

    // sql function 
    [DbFunction("fn_CheckNewProduct", "dbo")]
    public bool CheckNewProduct(long ProductId)
    {
        throw new NotSupportedException();
    }
}

