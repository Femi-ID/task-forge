using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class RenameWorkspaceInviteTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceInvite_AspNetUsers_InvitedByUserId",
                table: "WorkspaceInvite");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceInvite_Workspaces_WorkspaceId",
                table: "WorkspaceInvite");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkspaceInvite",
                table: "WorkspaceInvite");

            migrationBuilder.RenameTable(
                name: "WorkspaceInvite",
                newName: "WorkspaceInvites");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceInvite_WorkspaceId",
                table: "WorkspaceInvites",
                newName: "IX_WorkspaceInvites_WorkspaceId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceInvite_TokenHash",
                table: "WorkspaceInvites",
                newName: "IX_WorkspaceInvites_TokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceInvite_InvitedByUserId",
                table: "WorkspaceInvites",
                newName: "IX_WorkspaceInvites_InvitedByUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkspaceInvites",
                table: "WorkspaceInvites",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceInvites_AspNetUsers_InvitedByUserId",
                table: "WorkspaceInvites",
                column: "InvitedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceInvites_Workspaces_WorkspaceId",
                table: "WorkspaceInvites",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceInvites_AspNetUsers_InvitedByUserId",
                table: "WorkspaceInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceInvites_Workspaces_WorkspaceId",
                table: "WorkspaceInvites");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkspaceInvites",
                table: "WorkspaceInvites");

            migrationBuilder.RenameTable(
                name: "WorkspaceInvites",
                newName: "WorkspaceInvite");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceInvites_WorkspaceId",
                table: "WorkspaceInvite",
                newName: "IX_WorkspaceInvite_WorkspaceId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceInvites_TokenHash",
                table: "WorkspaceInvite",
                newName: "IX_WorkspaceInvite_TokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceInvites_InvitedByUserId",
                table: "WorkspaceInvite",
                newName: "IX_WorkspaceInvite_InvitedByUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkspaceInvite",
                table: "WorkspaceInvite",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceInvite_AspNetUsers_InvitedByUserId",
                table: "WorkspaceInvite",
                column: "InvitedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceInvite_Workspaces_WorkspaceId",
                table: "WorkspaceInvite",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
