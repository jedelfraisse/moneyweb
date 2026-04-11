using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411003)]
public class M010_AddSinkingFunds : Migration
{
    public override void Up()
    {
        Create.Table("SinkingFunds")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Description").AsString(500).Nullable()
            .WithColumn("TargetAmount").AsDecimal(18, 2).NotNullable()
            .WithColumn("Frequency").AsInt32().NotNullable()
            .WithColumn("NextDueDate").AsDate().NotNullable()
            .WithColumn("MonthlyContribution").AsDecimal(18, 2).Nullable()
            .WithColumn("CurrentSaved").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("BankAccountId").AsInt32().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("FK_SinkingFunds_Users")
            .FromTable("SinkingFunds").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id");
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_SinkingFunds_Users").OnTable("SinkingFunds");
        Delete.Table("SinkingFunds");
    }
}
