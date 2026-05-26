using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTESM.DAL.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class AddMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "metric",
                columns: table => new
                {
                    id = table.Column<long>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    collected_at_utc = table.Column<DateTimeOffset>(type: "text", nullable: false),
                    hostname = table.Column<string>(type: "text", nullable: false),
                    windows_version = table.Column<string>(type: "text", nullable: false),
                    uptime_seconds = table.Column<long>(type: "integer", nullable: false),
                    cpu_usage_percent = table.Column<double>(type: "real", nullable: false),
                    ram_usage_percent = table.Column<double>(type: "real", nullable: false),
                    total_memory_bytes = table.Column<long>(type: "integer", nullable: false),
                    available_memory_bytes = table.Column<long>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "metric_disk_space",
                columns: table => new
                {
                    id = table.Column<long>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    metric_id = table.Column<long>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    volume_label = table.Column<string>(type: "text", nullable: true),
                    drive_format = table.Column<string>(type: "text", nullable: true),
                    total_space_bytes = table.Column<long>(type: "integer", nullable: false),
                    free_space_bytes = table.Column<long>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_disk_space", x => x.id);
                    table.ForeignKey(
                        name: "FK_metric_disk_space_metric_metric_id",
                        column: x => x.metric_id,
                        principalTable: "metric",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metric_ip_address",
                columns: table => new
                {
                    id = table.Column<long>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    metric_id = table.Column<long>(type: "integer", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    address_family = table.Column<string>(type: "text", nullable: true),
                    network_interface_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_ip_address", x => x.id);
                    table.ForeignKey(
                        name: "FK_metric_ip_address_metric_metric_id",
                        column: x => x.metric_id,
                        principalTable: "metric",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metric_monitored_process",
                columns: table => new
                {
                    id = table.Column<long>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    metric_id = table.Column<long>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_running = table.Column<bool>(type: "integer", nullable: false),
                    matched_process_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_monitored_process", x => x.id);
                    table.ForeignKey(
                        name: "FK_metric_monitored_process_metric_metric_id",
                        column: x => x.metric_id,
                        principalTable: "metric",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metric_outbox_message",
                columns: table => new
                {
                    id = table.Column<long>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    metric_id = table.Column<long>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "text", nullable: false),
                    sent_at_utc = table.Column<DateTimeOffset>(type: "text", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_outbox_message", x => x.id);
                    table.ForeignKey(
                        name: "FK_metric_outbox_message_metric_metric_id",
                        column: x => x.metric_id,
                        principalTable: "metric",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metric_process",
                columns: table => new
                {
                    id = table.Column<long>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    metric_id = table.Column<long>(type: "integer", nullable: false),
                    process_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "text", nullable: true),
                    working_set_bytes = table.Column<long>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_process", x => x.id);
                    table.ForeignKey(
                        name: "FK_metric_process_metric_metric_id",
                        column: x => x.metric_id,
                        principalTable: "metric",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_metric_disk_space_metric_id",
                table: "metric_disk_space",
                column: "metric_id");

            migrationBuilder.CreateIndex(
                name: "IX_metric_ip_address_metric_id",
                table: "metric_ip_address",
                column: "metric_id");

            migrationBuilder.CreateIndex(
                name: "IX_metric_monitored_process_metric_id",
                table: "metric_monitored_process",
                column: "metric_id");

            migrationBuilder.CreateIndex(
                name: "IX_metric_outbox_message_metric_id",
                table: "metric_outbox_message",
                column: "metric_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_metric_outbox_message_status_id",
                table: "metric_outbox_message",
                columns: new[] { "status", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_metric_process_metric_id",
                table: "metric_process",
                column: "metric_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metric_disk_space");

            migrationBuilder.DropTable(
                name: "metric_ip_address");

            migrationBuilder.DropTable(
                name: "metric_monitored_process");

            migrationBuilder.DropTable(
                name: "metric_outbox_message");

            migrationBuilder.DropTable(
                name: "metric_process");

            migrationBuilder.DropTable(
                name: "metric");
        }
    }
}
