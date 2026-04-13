using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411022)]
public class M022_AddOriginalPrincipal : Migration
{
    public override void Up()
    {
        Alter.Table("Loans").AddColumn("OriginalPrincipal").AsDecimal(18, 2).NotNullable().WithDefaultValue(0);

        // Back-fill: original principal = current principal minus any non-payment transaction amounts
        // Type 0 = Payment; all other types (Interest, Additional, Fee) were added to Principal
        Execute.Sql("""
            UPDATE l
            SET l.OriginalPrincipal = l.Principal - ISNULL(
                (SELECT SUM(t.Amount) FROM LoanTransactions t WHERE t.LoanId = l.Id AND t.Type != 0),
                0
            )
            FROM Loans l
            """);
    }

    public override void Down()
    {
        Delete.Column("OriginalPrincipal").FromTable("Loans");
    }
}
