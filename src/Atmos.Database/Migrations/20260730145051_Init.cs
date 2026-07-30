using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atmos.Database.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    create_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    update_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "media",
                columns: table => new
                {
                    media_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "text", nullable: false),
                    create_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    update_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media", x => x.media_id);
                });

            migrationBuilder.CreateTable(
                name: "page",
                columns: table => new
                {
                    page_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    create_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    update_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page", x => x.page_id);
                });

            migrationBuilder.CreateTable(
                name: "tag",
                columns: table => new
                {
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    create_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    update_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag", x => x.tag_id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_addresses = table.Column<List<string>>(type: "text[]", nullable: false),
                    nickname = table.Column<string>(type: "text", nullable: false),
                    is_site_owner = table.Column<bool>(type: "boolean", nullable: false),
                    create_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    update_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "article",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    create_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    update_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article", x => x.article_id);
                    table.ForeignKey(
                        name: "FK_article_category_category_id",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "friend_link",
                columns: table => new
                {
                    friend_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    avatar_media_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    create_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    update_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friend_link", x => x.friend_link_id);
                    table.ForeignKey(
                        name: "FK_friend_link_media_avatar_media_id",
                        column: x => x.avatar_media_id,
                        principalTable: "media",
                        principalColumn: "media_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "media_reference",
                columns: table => new
                {
                    media_reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_id = table.Column<Guid>(type: "uuid", nullable: false),
                    referrer_type = table.Column<string>(type: "text", nullable: false),
                    referrer_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_reference", x => x.media_reference_id);
                    table.ForeignKey(
                        name: "FK_media_reference_media_media_id",
                        column: x => x.media_id,
                        principalTable: "media",
                        principalColumn: "media_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "social_link",
                columns: table => new
                {
                    social_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false),
                    invert = table.Column<bool>(type: "boolean", nullable: false),
                    icon_media_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    create_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    update_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_link", x => x.social_link_id);
                    table.ForeignKey(
                        name: "FK_social_link_media_icon_media_id",
                        column: x => x.icon_media_id,
                        principalTable: "media",
                        principalColumn: "media_id",
                        onDelete: ReferentialAction.Restrict);
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
                        name: "FK_social_login_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscription",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_types = table.Column<string[]>(type: "text[]", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscription_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "webauthn",
                columns: table => new
                {
                    credential_id = table.Column<byte[]>(type: "bytea", nullable: false),
                    public_key = table.Column<byte[]>(type: "bytea", nullable: false),
                    user_handle = table.Column<byte[]>(type: "bytea", nullable: false),
                    aa_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    signature_counter = table.Column<long>(type: "bigint", nullable: false),
                    cred_type = table.Column<string>(type: "text", nullable: false),
                    create_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    update_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webauthn", x => x.credential_id);
                    table.ForeignKey(
                        name: "FK_webauthn_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "article_tag",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_tag", x => new { x.article_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_article_tag_article_article_id",
                        column: x => x.article_id,
                        principalTable: "article",
                        principalColumn: "article_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_article_tag_tag_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tag",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_article_category_id",
                table: "article",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_article_published_at",
                table: "article",
                column: "published_at");

            migrationBuilder.CreateIndex(
                name: "IX_article_slug",
                table: "article",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_article_tag_tag_id",
                table: "article_tag",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_slug",
                table: "category",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_friend_link_avatar_media_id",
                table: "friend_link",
                column: "avatar_media_id");

            migrationBuilder.CreateIndex(
                name: "IX_media_key",
                table: "media",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_sha256",
                table: "media",
                column: "sha256");

            migrationBuilder.CreateIndex(
                name: "IX_media_reference_media_id_referrer_type_referrer_id",
                table: "media_reference",
                columns: new[] { "media_id", "referrer_type", "referrer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_page_slug",
                table: "page",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_link_icon_media_id",
                table: "social_link",
                column: "icon_media_id");

            migrationBuilder.CreateIndex(
                name: "IX_social_login_platform_identifier",
                table: "social_login",
                columns: new[] { "platform", "identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_login_user_id",
                table: "social_login",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_content_types",
                table: "subscription",
                column: "content_types");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_user_id",
                table: "subscription",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tag_slug",
                table: "tag",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_email_addresses",
                table: "user",
                column: "email_addresses")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_webauthn_user_id",
                table: "webauthn",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_tag");

            migrationBuilder.DropTable(
                name: "friend_link");

            migrationBuilder.DropTable(
                name: "media_reference");

            migrationBuilder.DropTable(
                name: "page");

            migrationBuilder.DropTable(
                name: "social_link");

            migrationBuilder.DropTable(
                name: "social_login");

            migrationBuilder.DropTable(
                name: "subscription");

            migrationBuilder.DropTable(
                name: "webauthn");

            migrationBuilder.DropTable(
                name: "article");

            migrationBuilder.DropTable(
                name: "tag");

            migrationBuilder.DropTable(
                name: "media");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "category");
        }
    }
}
