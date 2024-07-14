using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atmos.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "classification",
                columns: table => new
                {
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_classification", x => x.slug);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    first_release_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_edit_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_draft = table.Column<bool>(type: "boolean", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.slug);
                    table.UniqueConstraint("AK_Notes_slug_ContentType", x => new { x.slug, x.ContentType });
                });

            migrationBuilder.CreateTable(
                name: "SinglePages",
                columns: table => new
                {
                    slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    first_release_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_edit_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    single_page_type = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinglePages", x => x.slug);
                    table.UniqueConstraint("AK_SinglePages_slug_ContentType", x => new { x.slug, x.ContentType });
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_addresses = table.Column<List<string>>(type: "text[]", nullable: false),
                    nickname = table.Column<string>(type: "text", nullable: false),
                    is_site_owner = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "article",
                columns: table => new
                {
                    slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    first_release_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_edit_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_draft = table.Column<bool>(type: "boolean", nullable: false),
                    ClassificationSlug = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article", x => x.slug);
                    table.UniqueConstraint("AK_article_slug_ContentType", x => new { x.slug, x.ContentType });
                    table.ForeignKey(
                        name: "FK_article_classification_ClassificationSlug",
                        column: x => x.ClassificationSlug,
                        principalTable: "classification",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "social_login",
                columns: table => new
                {
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "text", nullable: false),
                    identifier = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_login", x => x.connection_id);
                    table.ForeignKey(
                        name: "FK_social_login_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscription",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    region = table.Column<string[]>(type: "text[]", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscription_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commentable_entity_id = table.Column<string>(type: "text", nullable: false),
                    commentable_entity_type = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comment", x => x.id);
                    table.ForeignKey(
                        name: "FK_comment_Notes_commentable_entity_id_commentable_entity_type",
                        columns: x => new { x.commentable_entity_id, x.commentable_entity_type },
                        principalTable: "Notes",
                        principalColumns: new[] { "slug", "ContentType" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comment_SinglePages_commentable_entity_id_commentable_entit~",
                        columns: x => new { x.commentable_entity_id, x.commentable_entity_type },
                        principalTable: "SinglePages",
                        principalColumns: new[] { "slug", "ContentType" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comment_Users_author_id",
                        column: x => x.author_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comment_article_commentable_entity_id_commentable_entity_ty~",
                        columns: x => new { x.commentable_entity_id, x.commentable_entity_type },
                        principalTable: "article",
                        principalColumns: new[] { "slug", "ContentType" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comment_comment_ParentId",
                        column: x => x.ParentId,
                        principalTable: "comment",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_article_ClassificationSlug",
                table: "article",
                column: "ClassificationSlug");

            migrationBuilder.CreateIndex(
                name: "IX_comment_author_id",
                table: "comment",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_comment_commentable_entity_id_commentable_entity_type",
                table: "comment",
                columns: new[] { "commentable_entity_id", "commentable_entity_type" });

            migrationBuilder.CreateIndex(
                name: "IX_comment_ParentId",
                table: "comment",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_social_login_platform_identifier",
                table: "social_login",
                columns: new[] { "platform", "identifier" });

            migrationBuilder.CreateIndex(
                name: "IX_social_login_user_id",
                table: "social_login",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_region",
                table: "subscription",
                column: "region");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_user_id",
                table: "subscription",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_email_addresses",
                table: "Users",
                column: "email_addresses")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comment");

            migrationBuilder.DropTable(
                name: "social_login");

            migrationBuilder.DropTable(
                name: "subscription");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "SinglePages");

            migrationBuilder.DropTable(
                name: "article");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "classification");
        }
    }
}
