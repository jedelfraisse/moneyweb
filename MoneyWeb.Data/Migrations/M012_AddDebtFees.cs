using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411005)]
public class M012_AddDebtFees : Migration
{
    public override void Up()
    {
        Create.Table("DebtFees")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("DebtId").AsInt32().NotNullable()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Amount").AsDecimal(18, 2).NotNullable()
            .WithColumn("Category").AsInt32().NotNullable().WithDefaultValue(5) // Other
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("FK_DebtFees_Debts")
            .FromTable("DebtFees").ForeignColumn("DebtId")
            .ToTable("Debts").PrimaryColumn("Id");

        Create.ForeignKey("FK_DebtFees_Users")
            .FromTable("DebtFees").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.Index("IX_DebtFees_DebtId").OnTable("DebtFees").OnColumn("DebtId");
    }

    public override void Down()
    {
        Delete.Index("IX_DebtFees_DebtId").OnTable("DebtFees");
        Delete.ForeignKey("FK_DebtFees_Users").OnTable("DebtFees");
        Delete.ForeignKey("FK_DebtFees_Debts").OnTable("DebtFees");
        Delete.Table("DebtFees");
    }
}
