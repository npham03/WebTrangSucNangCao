using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebTrangSucNangCao.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIdentityToNetCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            //migrationBuilder.CreateTable(
            //    name: "Category",
            //    columns: table => new
            //    {
            //        IDCate = table.Column<string>(type: "nchar(20)", fixedLength: true, maxLength: 20, nullable: false),
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        NameCate = table.Column<string>(type: "nvarchar(max)", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Category__43A295268D38D424", x => x.IDCate);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Customer",
            //    columns: table => new
            //    {
            //        IDCus = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        NameCus = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        PhoneCus = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
            //        EmailCus = table.Column<string>(type: "nvarchar(max)", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Customer__91A99BACEA59EC71", x => x.IDCus);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Sliders",
            //    columns: table => new
            //    {
            //        SliderID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
            //        Link = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Sliders", x => x.SliderID);
            //    });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            //migrationBuilder.CreateTable(
            //    name: "Products",
            //    columns: table => new
            //    {
            //        ProductID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        NamePro = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        DecriptionPro = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        Category = table.Column<string>(type: "nchar(20)", fixedLength: true, maxLength: 20, nullable: true),
            //        Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
            //        ImagePro = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        Material = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Products__B40CC6ED537555C8", x => x.ProductID);
            //        table.ForeignKey(
            //            name: "FK_Pro_Category",
            //            column: x => x.Category,
            //            principalTable: "Category",
            //            principalColumn: "IDCate");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "OrderPro",
            //    columns: table => new
            //    {
            //        ID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        DateOrder = table.Column<DateOnly>(type: "date", nullable: true),
            //        IDCus = table.Column<int>(type: "int", nullable: true),
            //        AddressDeliverry = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__OrderPro__3214EC276A71800D", x => x.ID);
            //        table.ForeignKey(
            //            name: "FK__OrderPro__IDCus__6D0D32F4",
            //            column: x => x.IDCus,
            //            principalTable: "Customer",
            //            principalColumn: "IDCus");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "ProductImages",
            //    columns: table => new
            //    {
            //        ImageID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        ProductID = table.Column<int>(type: "int", nullable: true),
            //        ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_ProductImages", x => x.ImageID);
            //        table.ForeignKey(
            //            name: "FK_ProductImages_Products",
            //            column: x => x.ProductID,
            //            principalTable: "Products",
            //            principalColumn: "ProductID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "ProductVariants",
            //    columns: table => new
            //    {
            //        VariantID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        ProductID = table.Column<int>(type: "int", nullable: true),
            //        VariantName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
            //        Stock = table.Column<int>(type: "int", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_ProductVariants", x => x.VariantID);
            //        table.ForeignKey(
            //            name: "FK_ProductVariants_Products",
            //            column: x => x.ProductID,
            //            principalTable: "Products",
            //            principalColumn: "ProductID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "OrderDetail",
            //    columns: table => new
            //    {
            //        ID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        IDProduct = table.Column<int>(type: "int", nullable: true),
            //        IDOrder = table.Column<int>(type: "int", nullable: true),
            //        Quantity = table.Column<int>(type: "int", nullable: true),
            //        UnitPrice = table.Column<double>(type: "float", nullable: true),
            //        VariantID = table.Column<int>(type: "int", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__OrderDet__3214EC27FBB15CDF", x => x.ID);
            //        table.ForeignKey(
            //            name: "FK_OrderDetail_ProductVariants",
            //            column: x => x.VariantID,
            //            principalTable: "ProductVariants",
            //            principalColumn: "VariantID");
            //        table.ForeignKey(
            //            name: "FK__OrderDeta__IDOrd__6B24EA82",
            //            column: x => x.IDOrder,
            //            principalTable: "OrderPro",
            //            principalColumn: "ID");
            //        table.ForeignKey(
            //            name: "FK__OrderDeta__IDPro__6C190EBB",
            //            column: x => x.IDProduct,
            //            principalTable: "Products",
            //            principalColumn: "ProductID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "UserCarts",
            //    columns: table => new
            //    {
            //        UserCartID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        UserID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
            //        Quantity = table.Column<int>(type: "int", nullable: true),
            //        VariantID = table.Column<int>(type: "int", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_UserCarts", x => x.UserCartID);
            //        table.ForeignKey(
            //            name: "FK_UserCarts_ProductVariants",
            //            column: x => x.VariantID,
            //            principalTable: "ProductVariants",
            //            principalColumn: "VariantID");
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_AspNetRoleClaims_RoleId",
            //    table: "AspNetRoleClaims",
            //    column: "RoleId");

            //migrationBuilder.CreateIndex(
            //    name: "RoleNameIndex",
            //    table: "AspNetRoles",
            //    column: "NormalizedName",
            //    unique: true,
            //    filter: "[NormalizedName] IS NOT NULL");

            //migrationBuilder.CreateIndex(
            //    name: "IX_AspNetUserClaims_UserId",
            //    table: "AspNetUserClaims",
            //    column: "UserId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_AspNetUserLogins_UserId",
            //    table: "AspNetUserLogins",
            //    column: "UserId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_AspNetUserRoles_RoleId",
            //    table: "AspNetUserRoles",
            //    column: "RoleId");

            //migrationBuilder.CreateIndex(
            //    name: "EmailIndex",
            //    table: "AspNetUsers",
            //    column: "NormalizedEmail");

            //migrationBuilder.CreateIndex(
            //    name: "UserNameIndex",
            //    table: "AspNetUsers",
            //    column: "NormalizedUserName",
            //    unique: true,
            //    filter: "[NormalizedUserName] IS NOT NULL");

            //migrationBuilder.CreateIndex(
            //    name: "IX_OrderDetail_IDOrder",
            //    table: "OrderDetail",
            //    column: "IDOrder");

            //migrationBuilder.CreateIndex(
            //    name: "IX_OrderDetail_IDProduct",
            //    table: "OrderDetail",
            //    column: "IDProduct");

            //migrationBuilder.CreateIndex(
            //    name: "IX_OrderDetail_VariantID",
            //    table: "OrderDetail",
            //    column: "VariantID");

            //migrationBuilder.CreateIndex(
            //    name: "IX_OrderPro_IDCus",
            //    table: "OrderPro",
            //    column: "IDCus");

            //migrationBuilder.CreateIndex(
            //    name: "IX_ProductImages_ProductID",
            //    table: "ProductImages",
            //    column: "ProductID");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Products_Category",
            //    table: "Products",
            //    column: "Category");

            //migrationBuilder.CreateIndex(
            //    name: "IX_ProductVariants_ProductID",
            //    table: "ProductVariants",
            //    column: "ProductID");

            //migrationBuilder.CreateIndex(
            //    name: "IX_UserCarts_VariantID",
            //    table: "UserCarts",
            //    column: "VariantID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            //migrationBuilder.DropTable(
            //    name: "OrderDetail");

            //migrationBuilder.DropTable(
            //    name: "ProductImages");

            //migrationBuilder.DropTable(
            //    name: "Sliders");

            //migrationBuilder.DropTable(
            //    name: "UserCarts");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            //migrationBuilder.DropTable(
            //    name: "OrderPro");

            //migrationBuilder.DropTable(
            //    name: "ProductVariants");

            //migrationBuilder.DropTable(
            //    name: "Customer");

            //migrationBuilder.DropTable(
            //    name: "Products");

            //migrationBuilder.DropTable(
            //    name: "Category");
        }
    }
}
