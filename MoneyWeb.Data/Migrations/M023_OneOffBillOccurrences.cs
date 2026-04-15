using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260415001)]
public class M023_OneOffBillOccurrences : Migration
{
    public override void Up()
    {
        // Allow one-off occurrences that aren't tied to a recurring bill
        Alter.Table("BillOccurrences").AddColumn("Name").AsString(200).Nullable();

        // Drop FK so BillId can be made nullable
        Execute.Sql("ALTER TABLE BillOccurrences DROP CONSTRAINT FK_BillOccurrences_Bills");
        Execute.Sql("ALTER TABLE BillOccurrences ALTER COLUMN BillId INT NULL");
    }

    public override void Down()
    {
        // Remove one-off occurrences before reversing
        Execute.Sql("DELETE FROM BillOccurrences WHERE BillId IS NULL");
        Execute.Sql("ALTER TABLE BillOccurrences ALTER COLUMN BillId INT NOT NULL");
        Execute.Sql("""
            ALTER TABLE BillOccurrences
            ADD CONSTRAINT FK_BillOccurrences_Bills
            FOREIGN KEY (BillId) REFERENCES Bills(Id)
            """);
        Delete.Column("Name").FromTable("BillOccurrences");
    }
}
