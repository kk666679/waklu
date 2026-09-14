using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalalChain.Platform.Api.Migrations
{
    /// <inheritdoc />
    public partial class MoveBlockchainToDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 876, DateTimeKind.Unspecified).AddTicks(8948), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 877, DateTimeKind.Unspecified).AddTicks(571), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 877, DateTimeKind.Unspecified).AddTicks(577), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 877, DateTimeKind.Unspecified).AddTicks(579), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: new Guid("10000020-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 877, DateTimeKind.Unspecified).AddTicks(581), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0010000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 870, DateTimeKind.Unspecified).AddTicks(3770), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0020000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(138), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0030000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(222), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0040000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(231), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0050000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(237), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0060000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(250), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0070000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(256), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0080000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(262), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0090000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(268), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0100000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(276), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0110000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(303), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0120000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(309), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0130000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(314), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0140000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(319), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0150000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(325), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0160000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(331), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0170000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(336), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0180000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(345), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0190000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(351), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0200000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(356), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0210000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(362), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0220000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(368), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0230000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(374), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0240000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(380), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0250000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(420), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0260000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(427), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0270000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(432), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0280000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(437), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0290000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(442), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0300000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(447), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0310000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(452), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0320000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(458), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0330000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(463), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0340000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(470), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0350000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(476), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0360000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(481), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0370000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(486), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0380000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(493), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0390000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(498), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0400000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(510), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0410000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(516), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0420000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(522), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0430000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(528), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0440000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(533), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0450000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(539), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0460000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(545), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0470000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(550), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0480000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(556), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0490000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(561), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0500000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(567), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0510000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(573), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0520000-0000-0000-0000-000000000000"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 871, DateTimeKind.Unspecified).AddTicks(578), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 869, DateTimeKind.Unspecified).AddTicks(4786), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 869, DateTimeKind.Unspecified).AddTicks(7245), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 869, DateTimeKind.Unspecified).AddTicks(7255), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 869, DateTimeKind.Unspecified).AddTicks(7258), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 8, 27, 23, 28, 28, 869, DateTimeKind.Unspecified).AddTicks(7261), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
