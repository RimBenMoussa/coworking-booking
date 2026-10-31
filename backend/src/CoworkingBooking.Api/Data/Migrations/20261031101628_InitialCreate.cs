using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CoworkingBooking.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contact_messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contact_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "spaces",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_spaces", x => x.id);
                    table.CheckConstraint("ck_spaces_capacity_positive", "capacity > 0");
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    space_id = table.Column<int>(type: "integer", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    persons = table.Column<int>(type: "integer", nullable: false),
                    planned_arrival = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    planned_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_subscriber = table.Column<bool>(type: "boolean", nullable: false),
                    check_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    check_out_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    quote_amount = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    amount_due = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    payment_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    manager_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.CheckConstraint("ck_bookings_end_after_start", "planned_end > planned_arrival");
                    table.CheckConstraint("ck_bookings_persons_positive", "persons > 0");
                    table.ForeignKey(
                        name: "fk_bookings_spaces_space_id",
                        column: x => x.space_id,
                        principalTable: "spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tariffs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    space_id = table.Column<int>(type: "integer", nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tariffs", x => x.id);
                    table.CheckConstraint("ck_tariffs_amount_not_negative", "amount >= 0");
                    table.ForeignKey(
                        name: "fk_tariffs_spaces_space_id",
                        column: x => x.space_id,
                        principalTable: "spaces",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "spaces",
                columns: new[] { "id", "capacity", "description", "name", "sort_order", "type" },
                values: new object[,]
                {
                    { 1, 17, "Grand espace de travail partagé, calme et lumineux.", "Open Space", 1, "PostePartage" },
                    { 2, 8, "Postes de travail en plein air avec vue sur la ville.", "Terrasse panoramique", 2, "PostePartage" },
                    { 3, 1, "Cabine insonorisée pour un appel ou une visio.", "Phone box Solo 1", 3, "Cabine" },
                    { 4, 1, "Cabine insonorisée pour un appel ou une visio.", "Phone box Solo 2", 4, "Cabine" },
                    { 5, 2, "Cabine pour deux personnes, idéale pour un entretien.", "Phone box Duo", 5, "Cabine" },
                    { 6, 4, "Petite salle de réunion privée.", "Salle Spark", 6, "SallePrivee" },
                    { 7, 8, "Salle de réunion équipée d'un écran.", "Salle Nexus", 7, "SallePrivee" },
                    { 8, 12, "Grande salle pour ateliers et formations.", "Salle Summit", 8, "SallePrivee" }
                });

            migrationBuilder.InsertData(
                table: "tariffs",
                columns: new[] { "id", "amount", "space_id", "type" },
                values: new object[,]
                {
                    { 10, 800m, null, "AbonnementMensuel" },
                    { 1, 25m, 1, "Court" },
                    { 2, 40m, 1, "Journee" },
                    { 3, 40m, 1, "Nuit" },
                    { 4, 30m, 2, "Court" },
                    { 5, 50m, 2, "Journee" },
                    { 6, 50m, 2, "Nuit" },
                    { 7, 10m, 3, "Heure" },
                    { 8, 10m, 4, "Heure" },
                    { 9, 15m, 5, "HeureParPersonne" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_users_email",
                table: "admin_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_code",
                table: "bookings",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_phone",
                table: "bookings",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_space_id_status_planned_arrival",
                table: "bookings",
                columns: new[] { "space_id", "status", "planned_arrival" });

            migrationBuilder.CreateIndex(
                name: "ix_spaces_name",
                table: "spaces",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_phone",
                table: "subscriptions",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_tariffs_space_id_type",
                table: "tariffs",
                columns: new[] { "space_id", "type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "contact_messages");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "tariffs");

            migrationBuilder.DropTable(
                name: "spaces");
        }
    }
}
