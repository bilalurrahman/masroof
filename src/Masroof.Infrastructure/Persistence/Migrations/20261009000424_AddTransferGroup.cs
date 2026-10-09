using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Masroof.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TransferGroupId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Txn_TransferGroup",
                table: "Transactions",
                columns: new[] { "UserId", "TransferGroupId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Txn_TransferGroup",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransferGroupId",
                table: "Transactions");
        }
    }
}
