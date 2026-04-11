using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411007)]
public class M007_AddSharePermissions : Migration
{
    public override void Up()
    {
        Create.Table("SharePermissions")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("EntityType").AsInt32().NotNullable()
            .WithColumn("EntityId").AsInt32().NotNullable()
            .WithColumn("GrantedByUserId").AsInt32().NotNullable()
            .WithColumn("SharedWithUserId").AsInt32().Nullable()
            .WithColumn("SharedWithGroupId").AsInt32().Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("FK_SharePermissions_GrantedBy")
            .FromTable("SharePermissions").ForeignColumn("GrantedByUserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.ForeignKey("FK_SharePermissions_SharedWithUser")
            .FromTable("SharePermissions").ForeignColumn("SharedWithUserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.ForeignKey("FK_SharePermissions_SharedWithGroup")
            .FromTable("SharePermissions").ForeignColumn("SharedWithGroupId")
            .ToTable("UserGroups").PrimaryColumn("Id");

        Create.Index("IX_SharePermissions_Entity")
            .OnTable("SharePermissions")
            .OnColumn("EntityType").Ascending()
            .OnColumn("EntityId").Ascending();

        // Remove old single-group sharing columns and their FKs
        Delete.ForeignKey("FK_Debts_SharingGroup").OnTable("Debts");
        Delete.ForeignKey("FK_Income_SharingGroup").OnTable("Income");
        Delete.ForeignKey("FK_BankAccounts_SharingGroup").OnTable("BankAccounts");

        Delete.Column("SharingGroupId").FromTable("Debts");
        Delete.Column("SharingGroupId").FromTable("Income");
        Delete.Column("SharingGroupId").FromTable("BankAccounts");
    }

    public override void Down()
    {
        Delete.Index("IX_SharePermissions_Entity").OnTable("SharePermissions");
        Delete.ForeignKey("FK_SharePermissions_SharedWithGroup").OnTable("SharePermissions");
        Delete.ForeignKey("FK_SharePermissions_SharedWithUser").OnTable("SharePermissions");
        Delete.ForeignKey("FK_SharePermissions_GrantedBy").OnTable("SharePermissions");
        Delete.Table("SharePermissions");

        Alter.Table("Debts").AddColumn("SharingGroupId").AsInt32().Nullable();
        Alter.Table("Income").AddColumn("SharingGroupId").AsInt32().Nullable();
        Alter.Table("BankAccounts").AddColumn("SharingGroupId").AsInt32().Nullable();
    }
}
