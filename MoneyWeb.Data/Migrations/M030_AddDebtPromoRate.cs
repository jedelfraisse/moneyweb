using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260815010000)]
public class M030_AddDebtPromoRate : Migration
{
    public override void Up()
    {
        Alter.Table("Debts").AddColumn("PromoInterestRate").AsDecimal(9, 6).Nullable();
        Alter.Table("Debts").AddColumn("PromoExpirationDate").AsDate().Nullable();
        Alter.Table("Debts").AddColumn("PromoStartDate").AsDate().Nullable();
        Alter.Table("Debts").AddColumn("PromoOriginalBalance").AsDecimal(18, 2).Nullable();
        // 0 = RevertToStandardRate (most bank cards), 1 = DeferredInterest (store-card "no interest if
        // paid in full by X" promos — retroactively charges interest on the remaining balance at expiration)
        Alter.Table("Debts").AddColumn("PromoExpirationBehavior").AsInt32().NotNullable().WithDefaultValue(0);
    }

    public override void Down()
    {
        Delete.Column("PromoInterestRate").FromTable("Debts");
        Delete.Column("PromoExpirationDate").FromTable("Debts");
        Delete.Column("PromoStartDate").FromTable("Debts");
        Delete.Column("PromoOriginalBalance").FromTable("Debts");
        Delete.Column("PromoExpirationBehavior").FromTable("Debts");
    }
}
