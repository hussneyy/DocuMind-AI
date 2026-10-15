using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace DocuMind.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "DocumentChunks",
                type: "vector",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                table: "DocumentChunks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                table: "DocumentChunks");
        }
    }
}
