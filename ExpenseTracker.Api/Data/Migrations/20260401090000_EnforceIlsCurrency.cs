using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Api.Data.Migrations
{
    public partial class EnforceIlsCurrency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Expenses"
                SET "Currency" = 'ILS'
                WHERE "Currency" IS DISTINCT FROM 'ILS';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Expenses",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "ILS",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Expenses_Currency_ILS",
                table: "Expenses",
                sql: "\"Currency\" = 'ILS'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Expenses_Currency_ILS",
                table: "Expenses");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Expenses",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldDefaultValue: "ILS");
        }
    }
}
