using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411004)]
public class M011_AddSinkingFundFundingAccount : Migration
{
    public override void Up()
    {
        Alter.Table("SinkingFunds")
            .AddColumn("FundingAccountId").AsInt32().Nullable();
    }

    public override void Down()
    {
        Delete.Column("FundingAccountId").FromTable("SinkingFunds");
    }
}
