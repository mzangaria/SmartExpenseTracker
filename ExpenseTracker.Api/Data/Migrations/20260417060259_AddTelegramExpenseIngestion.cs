using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramExpenseIngestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseIngestionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OriginalText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ParserType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ParsedPayloadJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ClarificationQuestion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedExpenseId = table.Column<Guid>(type: "uuid", nullable: true),
                    TelegramUpdateId = table.Column<long>(type: "bigint", nullable: true),
                    TelegramMessageId = table.Column<long>(type: "bigint", nullable: true),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseIngestionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseIngestionLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TelegramConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramConnections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TelegramLinkTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramLinkTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramLinkTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TelegramUpdatesProcessed",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdateId = table.Column<long>(type: "bigint", nullable: false),
                    MessageId = table.Column<long>(type: "bigint", nullable: true),
                    ChatId = table.Column<long>(type: "bigint", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramUpdatesProcessed", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseIngestionLogs_TelegramUpdateId_TelegramMessageId",
                table: "ExpenseIngestionLogs",
                columns: new[] { "TelegramUpdateId", "TelegramMessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseIngestionLogs_UserId_Channel_CreatedAtUtc",
                table: "ExpenseIngestionLogs",
                columns: new[] { "UserId", "Channel", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramConnections_TelegramChatId_IsActive",
                table: "TelegramConnections",
                columns: new[] { "TelegramChatId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramConnections_TelegramUserId_IsActive",
                table: "TelegramConnections",
                columns: new[] { "TelegramUserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramConnections_UserId_IsActive",
                table: "TelegramConnections",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLinkTokens_TokenHash",
                table: "TelegramLinkTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLinkTokens_UserId_ExpiresAtUtc",
                table: "TelegramLinkTokens",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramUpdatesProcessed_ChatId_MessageId",
                table: "TelegramUpdatesProcessed",
                columns: new[] { "ChatId", "MessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramUpdatesProcessed_UpdateId",
                table: "TelegramUpdatesProcessed",
                column: "UpdateId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseIngestionLogs");

            migrationBuilder.DropTable(
                name: "TelegramConnections");

            migrationBuilder.DropTable(
                name: "TelegramLinkTokens");

            migrationBuilder.DropTable(
                name: "TelegramUpdatesProcessed");
        }
    }
}
