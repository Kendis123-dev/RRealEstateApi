using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateApi.Migrations
{
    public partial class init3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgentId",
                table: "Properties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Agent",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "Listings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Listings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AgentId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_AgentId",
                table: "Properties",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AgentId",
                table: "AspNetUsers",
                column: "AgentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Agents_AgentId",
                table: "AspNetUsers",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Agents_AgentId",
                table: "Properties",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Agents_AgentId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Agents_AgentId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_AgentId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AgentId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "Agent",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "AspNetUsers");
        }
    }
}
