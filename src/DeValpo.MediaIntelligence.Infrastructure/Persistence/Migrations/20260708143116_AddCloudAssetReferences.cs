using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudAssetReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BucketName",
                table: "VideoSearchAssets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "VideoSearchAssets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Generation",
                table: "VideoSearchAssets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InputMode",
                table: "VideoSearchAssets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "MultipartUpload");

            migrationBuilder.AddColumn<bool>(
                name: "IsStaged",
                table: "VideoSearchAssets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "VideoSearchAssets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                table: "VideoSearchAssets",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "VideoSearchAssets",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "VideoSearchAssets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StagingCleanupStatus",
                table: "VideoSearchAssets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StagingExpiresAt",
                table: "VideoSearchAssets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageProvider",
                table: "VideoSearchAssets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionId",
                table: "VideoSearchAssets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BucketName",
                table: "ImageBatchSearchAssets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "ImageBatchSearchAssets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Generation",
                table: "ImageBatchSearchAssets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InputMode",
                table: "ImageBatchSearchAssets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "MultipartUpload");

            migrationBuilder.AddColumn<bool>(
                name: "IsStaged",
                table: "ImageBatchSearchAssets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "ImageBatchSearchAssets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                table: "ImageBatchSearchAssets",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "ImageBatchSearchAssets",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "ImageBatchSearchAssets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StagingCleanupStatus",
                table: "ImageBatchSearchAssets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StagingExpiresAt",
                table: "ImageBatchSearchAssets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageProvider",
                table: "ImageBatchSearchAssets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionId",
                table: "ImageBatchSearchAssets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BucketName",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "Generation",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "InputMode",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "IsStaged",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "ObjectKey",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "StagingCleanupStatus",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "StagingExpiresAt",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "StorageProvider",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "VersionId",
                table: "VideoSearchAssets");

            migrationBuilder.DropColumn(
                name: "BucketName",
                table: "ImageBatchSearchAssets");

            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "ImageBatchSearchAssets");

            migrationBuilder.DropColumn(
                name: "Generation",
                table: "ImageBatchSearchAssets");

            migrationBuilder.DropColumn(
                name: "InputMode",
                table: "ImageBatchSearchAssets");

            migrationBuilder.DropColumn(
                name: "IsStaged",
                table: "ImageBatchSearchAssets");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "ImageBatchSearchAssets");

            migrationBuilder.DropColumn(
                name: "ObjectKey",
                table: "ImageBatchSearchAssets");

            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "ImageBatchSearchAssets");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "ImageBatchSearchAssets");

            migrationBuilder.DropColumn(
                name: "StagingCleanupStatus",
                table: "ImageBatchSearchAssets");

            migrationBuilder.DropColumn(
                name: "StagingExpiresAt",
                table: "ImageBatchSearchAssets");

            migrationBuilder.DropColumn(
                name: "StorageProvider",
                table: "ImageBatchSearchAssets");

            migrationBuilder.DropColumn(
                name: "VersionId",
                table: "ImageBatchSearchAssets");
        }
    }
}
