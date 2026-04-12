using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411015)]
public class M020_AddFamilyKids : Migration
{
    public override void Up()
    {
        Create.Table("Kids")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable().ForeignKey("Users", "Id")
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("ColorHex").AsString(7).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Table("Chores")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("KidId").AsInt32().NotNullable().ForeignKey("Kids", "Id")
            .WithColumn("UserId").AsInt32().NotNullable().ForeignKey("Users", "Id")
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Description").AsString(500).Nullable()
            .WithColumn("RewardAmount").AsDecimal(18, 2).NotNullable()
            .WithColumn("Frequency").AsInt32().NotNullable().WithDefaultValue(2) // 2=Weekly
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Table("ChoreCompletions")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("ChoreId").AsInt32().NotNullable().ForeignKey("Chores", "Id")
            .WithColumn("KidId").AsInt32().NotNullable().ForeignKey("Kids", "Id")
            .WithColumn("UserId").AsInt32().NotNullable().ForeignKey("Users", "Id")
            .WithColumn("CompletedDate").AsDate().NotNullable()
            .WithColumn("Amount").AsDecimal(18, 2).NotNullable()
            .WithColumn("Notes").AsString(500).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Table("KidTransactions")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("KidId").AsInt32().NotNullable().ForeignKey("Kids", "Id")
            .WithColumn("UserId").AsInt32().NotNullable().ForeignKey("Users", "Id")
            .WithColumn("TransactionDate").AsDate().NotNullable()
            .WithColumn("Amount").AsDecimal(18, 2).NotNullable()
            .WithColumn("Description").AsString(300).NotNullable()
            .WithColumn("ContributorName").AsString(100).Nullable()
            .WithColumn("Source").AsInt32().NotNullable().WithDefaultValue(0) // 0=Deposit
            .WithColumn("ChoreCompletionId").AsInt32().Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);
    }

    public override void Down()
    {
        Delete.Table("KidTransactions");
        Delete.Table("ChoreCompletions");
        Delete.Table("Chores");
        Delete.Table("Kids");
    }
}
