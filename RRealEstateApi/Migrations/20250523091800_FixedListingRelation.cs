using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateApi.Migrations
{
    public partial class FixedListingRelation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_Agents_AgentId",
                table: "Listings");

            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Listings_ListingId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_ListingId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ListingType",
                table: "Listings");

            migrationBuilder.RenameColumn(
                name: "AgentId",
                table: "Listings",
                newName: "PropertyId");

            migrationBuilder.RenameIndex(
                name: "IX_Listings_AgentId",
                table: "Listings",
                newName: "IX_Listings_PropertyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_Properties_PropertyId",
                table: "Listings",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_Properties_PropertyId",
                table: "Listings");

            migrationBuilder.RenameColumn(
                name: "PropertyId",
                table: "Listings",
                newName: "AgentId");

            migrationBuilder.RenameIndex(
                name: "IX_Listings_PropertyId",
                table: "Listings",
                newName: "IX_Listings_AgentId");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Listings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ListingType",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ListingId",
                table: "Properties",
                column: "ListingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_Agents_AgentId",
                table: "Listings",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Listings_ListingId",
                table: "Properties",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
