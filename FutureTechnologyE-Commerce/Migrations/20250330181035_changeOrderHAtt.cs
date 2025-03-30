using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutureTechnologyE_Commerce.Migrations
{
    /// <inheritdoc />
    public partial class changeOrderHAtt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "State",
                table: "orderHeaders",
                newName: "state");

            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "orderHeaders",
                newName: "street");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "orderHeaders",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "PaymentIntentId",
                table: "orderHeaders",
                newName: "floor");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "orderHeaders",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "orderHeaders",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "orderHeaders",
                newName: "building");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "AspNetUsers",
                newName: "state");

            migrationBuilder.RenameColumn(
                name: "StreetAddress",
                table: "AspNetUsers",
                newName: "street");

            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "AspNetUsers",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "AspNetUsers",
                newName: "last_name");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "AspNetUsers",
                newName: "floor");

            migrationBuilder.AddColumn<string>(
                name: "apartment",
                table: "orderHeaders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "orderHeaders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "orderHeaders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "apartment",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "building",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "apartment",
                table: "orderHeaders");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "orderHeaders");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "orderHeaders");

            migrationBuilder.DropColumn(
                name: "apartment",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "building",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "country",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "state",
                table: "orderHeaders",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "street",
                table: "orderHeaders",
                newName: "PostalCode");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "orderHeaders",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "floor",
                table: "orderHeaders",
                newName: "PaymentIntentId");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "orderHeaders",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "orderHeaders",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "building",
                table: "orderHeaders",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "state",
                table: "AspNetUsers",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "street",
                table: "AspNetUsers",
                newName: "StreetAddress");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "AspNetUsers",
                newName: "PostalCode");

            migrationBuilder.RenameColumn(
                name: "last_name",
                table: "AspNetUsers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "floor",
                table: "AspNetUsers",
                newName: "City");
        }
    }
}
