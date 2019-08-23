using Microsoft.EntityFrameworkCore.Migrations;

namespace HackLinks_Server.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    username = table.Column<string>(maxLength: 64, nullable: false),
                    password = table.Column<string>(maxLength: 64, nullable: true),
                    mailaddress = table.Column<string>(maxLength: 64, nullable: true),
                    netmap = table.Column<string>(nullable: false),
                    permissions = table.Column<string>(nullable: true),
                    banned = table.Column<int>(nullable: false),
                    permBanned = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.username);
                });

            migrationBuilder.CreateTable(
                name: "Binaries",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    checksum = table.Column<int>(nullable: false),
                    type = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Binaries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "File",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    OwnerId = table.Column<int>(nullable: false),
                    groupId = table.Column<int>(nullable: false),
                    content = table.Column<string>(nullable: false),
                    FilesystemId = table.Column<int>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    filePermissions = table.Column<int>(nullable: false),
                    Checksum = table.Column<int>(nullable: false),
                    ParentId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_File", x => x.id);
                    table.ForeignKey(
                        name: "FK_File_File_ParentId",
                        column: x => x.ParentId,
                        principalTable: "File",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NetMapNode",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ip = table.Column<string>(nullable: true),
                    pos = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetMapNode", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "FileSystems",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RootFileId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileSystems", x => x.id);
                    table.ForeignKey(
                        name: "FK_FileSystems_File_RootFileId",
                        column: x => x.RootFileId,
                        principalTable: "File",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Computers",
                columns: table => new
                {
                    ip = table.Column<string>(maxLength: 15, nullable: false),
                    OwnerId = table.Column<string>(nullable: true),
                    type = table.Column<int>(nullable: false),
                    FileSystemId = table.Column<int>(nullable: true),
                    ServerAccountusername = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Computers", x => x.ip);
                    table.ForeignKey(
                        name: "FK_Computers_FileSystems_FileSystemId",
                        column: x => x.FileSystemId,
                        principalTable: "FileSystems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Computers_accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "accounts",
                        principalColumn: "username",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Computers_accounts_ServerAccountusername",
                        column: x => x.ServerAccountusername,
                        principalTable: "accounts",
                        principalColumn: "username",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_mailaddress",
                table: "accounts",
                column: "mailaddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Computers_FileSystemId",
                table: "Computers",
                column: "FileSystemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Computers_OwnerId",
                table: "Computers",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Computers_ServerAccountusername",
                table: "Computers",
                column: "ServerAccountusername");

            migrationBuilder.CreateIndex(
                name: "IX_File_ParentId",
                table: "File",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_File_Name_ParentId_FilesystemId",
                table: "File",
                columns: new[] { "Name", "ParentId", "FilesystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileSystems_RootFileId",
                table: "FileSystems",
                column: "RootFileId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Binaries");

            migrationBuilder.DropTable(
                name: "Computers");

            migrationBuilder.DropTable(
                name: "NetMapNode");

            migrationBuilder.DropTable(
                name: "FileSystems");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "File");
        }
    }
}
