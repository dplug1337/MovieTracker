using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MovieTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ReleaseYear = table.Column<int>(type: "integer", nullable: false),
                    Genre = table.Column<int>(type: "integer", nullable: false),
                    PosterUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Movies",
                columns: new[] { "Id", "Description", "Genre", "PosterUrl", "ReleaseYear", "Title" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "Explorers cross the stars in search of a future home for humanity.", 3, "/images/posters/interstellar.svg", 2014, "Interstellar" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "A specialist enters layered dreams to plant an idea in a guarded mind.", 3, "/images/posters/inception.svg", 2010, "Inception" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "A linguist races to understand mysterious visitors before fear takes over.", 3, "/images/posters/arrival.svg", 2016, "Arrival" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), "A programmer discovers that the familiar world hides a startling truth.", 0, "/images/posters/matrix.svg", 1999, "The Matrix" },
                    { new Guid("00000000-0000-0000-0000-000000000005"), "A new investigation uncovers a secret that could reshape a divided society.", 3, "/images/posters/blade-runner-2049.svg", 2017, "Blade Runner 2049" },
                    { new Guid("00000000-0000-0000-0000-000000000006"), "An ambitious drummer is pushed to extremes by a demanding instructor.", 1, "/images/posters/whiplash.svg", 2014, "Whiplash" },
                    { new Guid("00000000-0000-0000-0000-000000000007"), "Two families become entangled through opportunity, deception, and class tension.", 4, "/images/posters/parasite.svg", 2019, "Parasite" },
                    { new Guid("00000000-0000-0000-0000-000000000008"), "A small fellowship begins a dangerous journey to protect their world.", 7, "/images/posters/fellowship.svg", 2001, "The Lord of the Rings: The Fellowship of the Ring" },
                    { new Guid("00000000-0000-0000-0000-000000000009"), "A young girl enters a magical realm and must find her courage to return home.", 5, "/images/posters/spirited-away.svg", 2001, "Spirited Away" },
                    { new Guid("00000000-0000-0000-0000-000000000010"), "A devoted concierge and his lobby boy are swept into a comic mystery.", 2, "/images/posters/grand-budapest.svg", 2014, "The Grand Budapest Hotel" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Title_Id",
                table: "Movies",
                columns: new[] { "Title", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movies");
        }
    }
}
