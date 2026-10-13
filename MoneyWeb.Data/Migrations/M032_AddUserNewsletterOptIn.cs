using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260817000000)]
public class M032_AddUserNewsletterOptIn : Migration
{
    public override void Up()
    {
        Alter.Table("Users").AddColumn("NewsletterOptIn").AsBoolean().NotNullable().WithDefaultValue(false);
    }

    public override void Down()
    {
        Delete.Column("NewsletterOptIn").FromTable("Users");
    }
}
