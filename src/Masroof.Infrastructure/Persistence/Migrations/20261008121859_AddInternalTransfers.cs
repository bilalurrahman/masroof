using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Masroof.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Categories: flag that excludes a category from spend/income totals.
            migrationBuilder.AddColumn<bool>(
                name: "ExcludeFromTotals",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Accounts: IBAN tail (transfer SMS name the destination by IBAN) and an
            // "is this one of my own accounts" flag used to detect internal transfers.
            migrationBuilder.AddColumn<string>(
                name: "IbanTail",
                table: "Accounts",
                type: "char(4)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOwn",
                table: "Accounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // New taxonomy category for money moved between the user's own accounts.
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "Code", "Color", "ExcludeFromTotals", "Icon", "NameAr", "NameEn" },
                values: new object[] { (short)21, "transfer_internal", "#78909C", true, "swap_horiz", "تحويل داخلي", "Internal Transfer" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: (short)21);

            migrationBuilder.DropColumn(
                name: "ExcludeFromTotals",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IbanTail",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "IsOwn",
                table: "Accounts");
        }
    }
}
