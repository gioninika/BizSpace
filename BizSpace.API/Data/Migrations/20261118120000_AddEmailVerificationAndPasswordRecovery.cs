using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizSpace.Data.Migrations;

public partial class AddEmailVerificationAndPasswordRecovery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EmailConfirmed",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<int>(
            name: "EmailCodeFailedAttempts",
            table: "Users",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTime>(
            name: "EmailCodeExpiresAtUtc",
            table: "Users",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "EmailCodeHash",
            table: "Users",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "EmailCodePurpose",
            table: "Users",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "EmailCodeSentAtUtc",
            table: "Users",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "EmailConfirmed", table: "Users");
        migrationBuilder.DropColumn(name: "EmailCodeFailedAttempts", table: "Users");
        migrationBuilder.DropColumn(name: "EmailCodeExpiresAtUtc", table: "Users");
        migrationBuilder.DropColumn(name: "EmailCodeHash", table: "Users");
        migrationBuilder.DropColumn(name: "EmailCodePurpose", table: "Users");
        migrationBuilder.DropColumn(name: "EmailCodeSentAtUtc", table: "Users");
    }
}
