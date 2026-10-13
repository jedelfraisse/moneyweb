using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

/// <summary>Stores ASP.NET Core Data Protection keys (which encrypt the auth cookie) in the database.
/// Without this, keys live in memory on the shared IIS host and are regenerated on every app-pool
/// recycle or deploy — invalidating everyone's 30-day login cookie and logging them out.</summary>
[Migration(20260820000000)]
public class M035_AddDataProtectionKeys : Migration
{
    public override void Up()
    {
        Create.Table("DataProtectionKeys")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("FriendlyName").AsString(256).NotNullable()
            .WithColumn("Xml").AsString(int.MaxValue).NotNullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);
    }

    public override void Down()
    {
        Delete.Table("DataProtectionKeys");
    }
}
