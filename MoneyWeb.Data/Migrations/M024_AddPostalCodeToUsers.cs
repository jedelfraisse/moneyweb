using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260415000001)]
public class M024_AddPostalCodeToUsers : Migration
{
    public override void Up()
    {
        Alter.Table("Users").AddColumn("PostalCode").AsString(20).NotNullable().WithDefaultValue(string.Empty);
    }

    public override void Down()
    {
        Delete.Column("PostalCode").FromTable("Users");
    }
}
