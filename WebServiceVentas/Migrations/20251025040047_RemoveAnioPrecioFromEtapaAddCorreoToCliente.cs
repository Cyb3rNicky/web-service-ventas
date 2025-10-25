using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebServiceVentas.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAnioPrecioFromEtapaAddCorreoToCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar columnas Anio y Precio de Etapas
            migrationBuilder.DropColumn(
                name: "Anio",
                table: "Etapas");

            migrationBuilder.DropColumn(
                name: "Precio",
                table: "Etapas");

            // Agregar columna CorreoElectronico a Clientes
            migrationBuilder.AddColumn<string>(
                name: "CorreoElectronico",
                table: "Clientes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir cambios
            migrationBuilder.DropColumn(
                name: "CorreoElectronico",
                table: "Clientes");

            migrationBuilder.AddColumn<int>(
                name: "Anio",
                table: "Etapas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Precio",
                table: "Etapas",
                type: "numeric(18,2)",
                nullable: true);
        }
    }
}
