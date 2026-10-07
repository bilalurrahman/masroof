using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Masroof.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<short>(type: "smallint", nullable: false),
                    Code = table.Column<string>(type: "varchar(40)", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Icon = table.Column<string>(type: "varchar(40)", nullable: true),
                    Color = table.Column<string>(type: "char(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Outbox",
                columns: table => new
                {
                    OutboxId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "varchar(50)", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outbox", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Locale = table.Column<string>(type: "varchar(10)", nullable: false, defaultValue: "en"),
                    Currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "SAR"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankCode = table.Column<string>(type: "varchar(20)", nullable: true),
                    Last4 = table.Column<string>(type: "char(4)", nullable: true),
                    Nickname = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_Accounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MerchantRules",
                columns: table => new
                {
                    RuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectNorm = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<short>(type: "smallint", nullable: false),
                    RuleText = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    EmbeddingModel = table.Column<string>(type: "varchar(80)", nullable: true),
                    HitCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantRules", x => x.RuleId);
                    table.ForeignKey(
                        name: "FK_MerchantRules_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MerchantRules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    TransactionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RawTextHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    Direction = table.Column<string>(type: "varchar(6)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "char(3)", nullable: false),
                    Counterparty = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CounterpartyNorm = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Channel = table.Column<string>(type: "varchar(20)", nullable: true),
                    TxnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CategoryId = table.Column<short>(type: "smallint", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(4,3)", nullable: true),
                    Source = table.Column<string>(type: "varchar(12)", nullable: false),
                    IsCorrected = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVer = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.TransactionId);
                    table.CheckConstraint("CK_Txn_Amount", "[Amount] >= 0");
                    table.CheckConstraint("CK_Txn_Direction", "[Direction] IN ('debit','credit')");
                    table.ForeignKey(
                        name: "FK_Transactions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Transactions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    FeedbackId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionId = table.Column<long>(type: "bigint", nullable: false),
                    Field = table.Column<string>(type: "varchar(30)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.FeedbackId);
                    table.ForeignKey(
                        name: "FK_Feedback_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "TransactionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParseTraces",
                columns: table => new
                {
                    TraceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionId = table.Column<long>(type: "bigint", nullable: true),
                    Model = table.Column<string>(type: "varchar(80)", nullable: false),
                    PromptVersion = table.Column<string>(type: "varchar(20)", nullable: false),
                    HintsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LatencyMs = table.Column<int>(type: "int", nullable: false),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParseTraces", x => x.TraceId);
                    table.ForeignKey(
                        name: "FK_ParseTraces_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "TransactionId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "Code", "Color", "Icon", "NameAr", "NameEn" },
                values: new object[,]
                {
                    { (short)1, "groceries", "#4CAF50", "shopping_cart", "بقالة", "Groceries" },
                    { (short)2, "dining", "#FF7043", "restaurant", "مطاعم", "Dining" },
                    { (short)3, "transport", "#5C6BC0", "directions_bus", "مواصلات", "Transport" },
                    { (short)4, "fuel", "#8D6E63", "local_gas_station", "وقود", "Fuel" },
                    { (short)5, "utilities", "#FBC02D", "bolt", "خدمات", "Utilities" },
                    { (short)6, "telecom", "#26A69A", "sim_card", "اتصالات", "Telecom" },
                    { (short)7, "rent", "#7E57C2", "home", "إيجار", "Rent" },
                    { (short)8, "health", "#EF5350", "local_hospital", "صحة", "Health" },
                    { (short)9, "education", "#42A5F5", "school", "تعليم", "Education" },
                    { (short)10, "shopping", "#EC407A", "shopping_bag", "تسوق", "Shopping" },
                    { (short)11, "entertainment", "#AB47BC", "movie", "ترفيه", "Entertainment" },
                    { (short)12, "travel", "#29B6F6", "flight", "سفر", "Travel" },
                    { (short)13, "family_transfer", "#66BB6A", "family_restroom", "تحويل عائلي", "Family Transfer" },
                    { (short)14, "salary", "#9CCC65", "payments", "راتب", "Salary" },
                    { (short)15, "refund", "#26C6DA", "undo", "استرداد", "Refund" },
                    { (short)16, "fees_charges", "#BDBDBD", "receipt_long", "رسوم", "Fees & Charges" },
                    { (short)17, "atm_cash", "#78909C", "local_atm", "صراف / نقد", "ATM / Cash" },
                    { (short)18, "investment", "#2E7D32", "trending_up", "استثمار", "Investment" },
                    { (short)19, "government", "#546E7A", "account_balance", "حكومي", "Government" },
                    { (short)20, "other", "#90A4AE", "category", "أخرى", "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Accounts",
                table: "Accounts",
                columns: new[] { "UserId", "BankCode", "Last4" },
                unique: true,
                filter: "[BankCode] IS NOT NULL AND [Last4] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Code",
                table: "Categories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_TransactionId",
                table: "Feedback",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantRules_CategoryId",
                table: "MerchantRules",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "UQ_Rule",
                table: "MerchantRules",
                columns: new[] { "UserId", "SubjectNorm" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_Unprocessed",
                table: "Outbox",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ParseTraces_TransactionId",
                table: "ParseTraces",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CategoryId",
                table: "Transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Txn_User_Date",
                table: "Transactions",
                columns: new[] { "UserId", "TxnDate" })
                .Annotation("SqlServer:Include", new[] { "Amount", "Direction", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "UQ_Txn_Dedupe",
                table: "Transactions",
                columns: new[] { "UserId", "RawTextHash" },
                unique: true);

            // The bge-m3 embedding column uses SQL Server 2025's native VECTOR type, which EF
            // does not map. It is read/written by the rules engine (Dapper) via VECTOR_DISTANCE.
            migrationBuilder.Sql("ALTER TABLE [MerchantRules] ADD [Embedding] VECTOR(1024) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedback");

            migrationBuilder.DropTable(
                name: "MerchantRules");

            migrationBuilder.DropTable(
                name: "Outbox");

            migrationBuilder.DropTable(
                name: "ParseTraces");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
