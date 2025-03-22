﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExpenseTracker.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Category", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("MccCodes")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Categories");

                    b.HasData(
                        new
                        {
                            Id = new Guid("c1b15d7e-0b6f-4d19-9d8c-b0c8722277d0"),
                            MccCodes = "5814",
                            Name = "Кофейні"
                        },
                        new
                        {
                            Id = new Guid("b7d45c1b-19b4-4770-bcf4-8c2f5e4d3424"),
                            MccCodes = "5812",
                            Name = "Ресторани"
                        },
                        new
                        {
                            Id = new Guid("a25a42f4-88b3-4006-b0c3-2c7a15a358e7"),
                            MccCodes = "5411,5499",
                            Name = "Супермаркети"
                        },
                        new
                        {
                            Id = new Guid("ad42b743-ef9a-43e5-b71f-97a742ae1a85"),
                            MccCodes = "5651",
                            Name = "Магазини одягу"
                        },
                        new
                        {
                            Id = new Guid("61e1f6c7-7b85-47d1-bb9a-d78f911e8cd3"),
                            MccCodes = "5541",
                            Name = "АЗС"
                        },
                        new
                        {
                            Id = new Guid("db60d0b9-89e6-4694-9295-56b688254a2f"),
                            MccCodes = "6011",
                            Name = "Банки"
                        },
                        new
                        {
                            Id = new Guid("1a12c08c-f9eb-4f29-8480-ef05137e0cf5"),
                            MccCodes = "5912",
                            Name = "Аптеки"
                        },
                        new
                        {
                            Id = new Guid("d177f3d7-d5d2-4d97-bd9e-45f54e2e268f"),
                            MccCodes = "7011",
                            Name = "Готелі"
                        },
                        new
                        {
                            Id = new Guid("b50ee9f0-3480-40d8-bb48-e15bf9e2fc03"),
                            MccCodes = "5811,8999",
                            Name = "Доставка"
                        },
                        new
                        {
                            Id = new Guid("2afd336a-03b8-4f17-8cfe-2c0e93cb4c49"),
                            MccCodes = "4829",
                            Name = "Знаття/Відправка"
                        },
                        new
                        {
                            Id = new Guid("cf668835-71a2-4868-87b8-3938ac9dabf9"),
                            MccCodes = "4900",
                            Name = "Комуналка"
                        },
                        new
                        {
                            Id = new Guid("86cdeb09-ae28-4a8f-9006-24ce4c44419f"),
                            MccCodes = "6012",
                            Name = "Надходження"
                        },
                        new
                        {
                            Id = new Guid("74d258ea-bf82-4934-bb9a-8a343d9da1ea"),
                            MccCodes = "7832",
                            Name = "Кінотеатри"
                        });
                });

            modelBuilder.Entity("ExpenseTracker.Data.Model.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Token")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("MerchantAlias", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("NormalizedName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("OriginalName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("MerchantAliases");
                });

            modelBuilder.Entity("Transaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("Amount")
                        .HasColumnType("numeric");

                    b.Property<Guid?>("CategoryId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int?>("MccCode")
                        .HasColumnType("integer");

                    b.Property<string>("MerchantName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.HasIndex("UserId");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("Transaction", b =>
                {
                    b.HasOne("Category", "Category")
                        .WithMany()
                        .HasForeignKey("CategoryId");

                    b.HasOne("ExpenseTracker.Data.Model.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Category");

                    b.Navigation("User");
                });
#pragma warning restore 612, 618
        }
    }
}
