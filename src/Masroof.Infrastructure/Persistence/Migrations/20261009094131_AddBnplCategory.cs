using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Masroof.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBnplCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "Code", "Color", "Icon", "NameAr", "NameEn" },
                values: new object[] { (short)22, "bnpl", "#9575CD", "schedule", "اشترِ الآن وادفع لاحقًا", "Buy Now Pay Later" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: (short)22);
        }
    }
}
