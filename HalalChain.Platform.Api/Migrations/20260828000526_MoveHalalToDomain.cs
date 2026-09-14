using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalalChain.Platform.Api.Migrations
{
    /// <inheritdoc />
    public partial class MoveHalalToDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 176, DateTimeKind.Unspecified).AddTicks(4122), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 176, DateTimeKind.Unspecified).AddTicks(5856), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 176, DateTimeKind.Unspecified).AddTicks(5863), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 176, DateTimeKind.Unspecified).AddTicks(5865), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 176, DateTimeKind.Unspecified).AddTicks(5867), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0010000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 166, DateTimeKind.Unspecified).AddTicks(9692), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0020000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6110), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0030000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6206), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0040000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6217), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0050000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6224), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0060000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6237), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0070000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6247), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0080000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6256), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0090000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6263), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0100000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6271), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0110000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6373), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0120000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6382), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0130000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6389), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0140000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6396), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0150000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6424), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0160000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6437), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0170000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6444), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0180000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6453), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0190000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6460), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0200000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6468), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0210000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6475), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0220000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6481), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0230000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6488), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0240000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6495), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0250000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6528), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0260000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6576), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0270000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6585), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0280000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6601), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0290000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6608), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0300000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6615), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0310000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6628), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0320000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6636), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0330000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6643), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0340000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6650), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0350000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6852), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0360000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6863), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0370000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6869), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0380000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6876), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0390000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6884), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0400000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6891), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0410000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6898), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0420000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6904), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0430000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6910), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0440000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6918), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0450000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6925), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0460000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6932), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0470000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6938), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0480000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6945), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0490000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6952), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0500000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(6959), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0510000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(7051), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0520000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 167, DateTimeKind.Unspecified).AddTicks(7059), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 166, DateTimeKind.Unspecified).AddTicks(145), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 166, DateTimeKind.Unspecified).AddTicks(3106), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 166, DateTimeKind.Unspecified).AddTicks(3158), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 166, DateTimeKind.Unspecified).AddTicks(3161), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 28, 0, 5, 24, 166, DateTimeKind.Unspecified).AddTicks(3165), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 713, DateTimeKind.Unspecified).AddTicks(9863), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 714, DateTimeKind.Unspecified).AddTicks(2589), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 714, DateTimeKind.Unspecified).AddTicks(2597), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 714, DateTimeKind.Unspecified).AddTicks(2599), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 714, DateTimeKind.Unspecified).AddTicks(2602), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0010000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 707, DateTimeKind.Unspecified).AddTicks(4940), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0020000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1687), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0030000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1782), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0040000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1795), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0050000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1806), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0060000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1874), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0070000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1882), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0080000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1889), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0090000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1895), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0100000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1904), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0110000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1912), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0120000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1918), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0130000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1924), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0140000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1931), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0150000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1955), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0160000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1967), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0170000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1983), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0180000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1993), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0190000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(1999), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0200000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2017), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0210000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2024), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0220000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2031), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0230000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2038), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0240000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2044), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0250000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2079), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0260000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2087), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0270000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2094), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0280000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2111), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0290000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2118), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0300000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2125), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0310000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2144), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0320000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2151), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0330000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2158), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0340000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2178), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0350000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2186), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0360000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2193), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0370000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2200), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0380000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2207), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0390000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2213), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0400000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2220), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0410000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2227), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0420000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2233), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0430000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2239), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0440000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2245), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0450000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2251), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0460000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2258), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0470000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2264), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0480000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2271), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0490000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2290), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0500000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2298), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0510000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2305), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0520000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 708, DateTimeKind.Unspecified).AddTicks(2311), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 706, DateTimeKind.Unspecified).AddTicks(5602), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 706, DateTimeKind.Unspecified).AddTicks(8434), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 706, DateTimeKind.Unspecified).AddTicks(8446), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 706, DateTimeKind.Unspecified).AddTicks(8449), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 41, 7, 706, DateTimeKind.Unspecified).AddTicks(8452), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
