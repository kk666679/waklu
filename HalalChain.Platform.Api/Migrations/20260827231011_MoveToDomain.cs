using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalalChain.Platform.Api.Migrations
{
    /// <inheritdoc />
    public partial class MoveToDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Pending",
                table: "OutboxMessages");

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 235, DateTimeKind.Unspecified).AddTicks(4643), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 235, DateTimeKind.Unspecified).AddTicks(5614), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 235, DateTimeKind.Unspecified).AddTicks(5618), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 235, DateTimeKind.Unspecified).AddTicks(5620), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 235, DateTimeKind.Unspecified).AddTicks(5622), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0010000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(5376), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0020000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9338), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0030000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9407), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0040000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9413), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0050000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9417), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0060000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9432), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0070000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9446), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0080000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9471), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0090000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9476), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0100000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9480), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0110000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9489), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0120000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9493), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0130000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9497), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0140000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9500), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0150000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9511), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0160000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9527), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0170000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9531), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0180000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9536), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0190000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9540), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0200000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9544), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0210000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9548), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0220000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9563), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0230000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9567), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0240000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9570), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0250000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9594), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0260000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9598), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0270000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9602), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0280000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9612), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0290000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9616), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0300000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9619), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0310000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9627), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0320000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9630), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0330000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9634), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0340000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9638), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0350000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9643), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0360000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9647), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0370000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9660), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0380000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9664), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0390000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9668), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0400000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9671), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0410000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9675), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0420000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9678), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0430000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9682), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0440000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9685), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0450000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9689), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0460000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9693), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0470000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9696), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0480000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9700), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0490000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9704), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0500000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9707), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0510000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9711), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0520000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(9720), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 230, DateTimeKind.Unspecified).AddTicks(9446), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(876), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(883), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(885), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 10, 8, 231, DateTimeKind.Unspecified).AddTicks(887), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Pending",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Pending",
                table: "OutboxMessages");

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 701, DateTimeKind.Unspecified).AddTicks(6436), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 701, DateTimeKind.Unspecified).AddTicks(7852), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 701, DateTimeKind.Unspecified).AddTicks(7857), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 701, DateTimeKind.Unspecified).AddTicks(7859), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 701, DateTimeKind.Unspecified).AddTicks(7861), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0010000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 695, DateTimeKind.Unspecified).AddTicks(8703), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0020000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4305), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0030000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4384), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0040000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4392), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0050000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4397), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0060000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4409), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0070000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4416), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0080000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4421), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0090000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4426), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0100000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4434), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0110000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4441), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0120000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4446), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0130000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4451), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0140000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4474), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0150000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4480), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0160000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4485), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0170000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4491), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0180000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4499), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0190000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4505), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0200000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4510), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0210000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4516), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0220000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4521), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0230000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4526), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0240000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4531), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0250000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4558), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0260000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4564), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0270000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4569), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0280000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4574), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0290000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4590), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0300000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4596), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0310000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4601), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0320000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4606), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0330000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4612), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0340000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4619), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0350000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4625), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0360000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4631), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0370000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4636), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0380000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4641), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0390000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4647), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0400000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4652), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0410000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4657), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0420000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4663), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0430000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4675), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0440000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4681), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0450000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4686), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0460000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4692), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0470000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4698), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0480000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4703), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0490000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4708), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0500000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4713), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0510000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4718), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0520000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4723), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 694, DateTimeKind.Unspecified).AddTicks(9403), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 695, DateTimeKind.Unspecified).AddTicks(2550), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 695, DateTimeKind.Unspecified).AddTicks(2559), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 695, DateTimeKind.Unspecified).AddTicks(2593), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 695, DateTimeKind.Unspecified).AddTicks(2596), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Pending",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "CreatedAt" });
        }
    }
}
