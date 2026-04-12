using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411021)]
public class M021_AddSharingContacts : Migration
{
    public override void Up()
    {
        Create.Table("SharingContacts")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("OwnerUserId").AsInt32().NotNullable()
            .WithColumn("InvitedEmail").AsString(256).NotNullable()
            .WithColumn("LinkedUserId").AsInt32().Nullable()
            .WithColumn("DisplayName").AsString(200).Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("FK_SharingContacts_Owner")
            .FromTable("SharingContacts").ForeignColumn("OwnerUserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.ForeignKey("FK_SharingContacts_LinkedUser")
            .FromTable("SharingContacts").ForeignColumn("LinkedUserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.UniqueConstraint("UQ_SharingContacts_OwnerEmail")
            .OnTable("SharingContacts")
            .Columns("OwnerUserId", "InvitedEmail");
    }

    public override void Down()
    {
        Delete.UniqueConstraint("UQ_SharingContacts_OwnerEmail").FromTable("SharingContacts");
        Delete.ForeignKey("FK_SharingContacts_LinkedUser").OnTable("SharingContacts");
        Delete.ForeignKey("FK_SharingContacts_Owner").OnTable("SharingContacts");
        Delete.Table("SharingContacts");
    }
}
