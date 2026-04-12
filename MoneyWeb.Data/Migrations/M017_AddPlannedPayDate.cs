using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411012)]
public class M017_AddPlannedPayDate : Migration
{
    public override void Up()
    {
        Alter.Table("BillOccurrences")
            .AddColumn("PlannedPayDate").AsDate().Nullable();
    }

    public override void Down()
    {
        Delete.Column("PlannedPayDate").FromTable("BillOccurrences");
    }
}
