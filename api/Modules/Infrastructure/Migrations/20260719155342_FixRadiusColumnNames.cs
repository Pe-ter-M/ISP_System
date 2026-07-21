using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternetProvider.Api.Modules.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRadiusColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "radusergroup",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "GroupName",
                table: "radusergroup",
                newName: "groupname");

            migrationBuilder.RenameIndex(
                name: "IX_radusergroup_UserName",
                table: "radusergroup",
                newName: "IX_radusergroup_username");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "radreply",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "radreply",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Attribute",
                table: "radreply",
                newName: "attribute");

            migrationBuilder.RenameIndex(
                name: "IX_radreply_UserName_Attribute",
                table: "radreply",
                newName: "IX_radreply_username_attribute");

            migrationBuilder.RenameColumn(
                name: "Class",
                table: "radpostauth",
                newName: "class");

            migrationBuilder.RenameColumn(
                name: "CallingStationId",
                table: "radpostauth",
                newName: "callingstationid");

            migrationBuilder.RenameColumn(
                name: "CalledStationId",
                table: "radpostauth",
                newName: "calledstationid");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "radgroupreply",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "GroupName",
                table: "radgroupreply",
                newName: "groupname");

            migrationBuilder.RenameColumn(
                name: "Attribute",
                table: "radgroupreply",
                newName: "attribute");

            migrationBuilder.RenameIndex(
                name: "IX_radgroupreply_GroupName_Attribute",
                table: "radgroupreply",
                newName: "IX_radgroupreply_groupname_attribute");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "radgroupcheck",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "GroupName",
                table: "radgroupcheck",
                newName: "groupname");

            migrationBuilder.RenameColumn(
                name: "Attribute",
                table: "radgroupcheck",
                newName: "attribute");

            migrationBuilder.RenameIndex(
                name: "IX_radgroupcheck_GroupName_Attribute",
                table: "radgroupcheck",
                newName: "IX_radgroupcheck_groupname_attribute");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "radcheck",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "radcheck",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Attribute",
                table: "radcheck",
                newName: "attribute");

            migrationBuilder.RenameIndex(
                name: "IX_radcheck_UserName_Attribute",
                table: "radcheck",
                newName: "IX_radcheck_username_attribute");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "radacct",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "ServiceType",
                table: "radacct",
                newName: "servicetype");

            migrationBuilder.RenameColumn(
                name: "Realm",
                table: "radacct",
                newName: "realm");

            migrationBuilder.RenameColumn(
                name: "NASPortType",
                table: "radacct",
                newName: "nasporttype");

            migrationBuilder.RenameColumn(
                name: "NASPortId",
                table: "radacct",
                newName: "nasportid");

            migrationBuilder.RenameColumn(
                name: "NASIPAddress",
                table: "radacct",
                newName: "nasipaddress");

            migrationBuilder.RenameColumn(
                name: "FramedProtocol",
                table: "radacct",
                newName: "framedprotocol");

            migrationBuilder.RenameColumn(
                name: "FramedIPv6Address",
                table: "radacct",
                newName: "framedipv6address");

            migrationBuilder.RenameColumn(
                name: "FramedIPAddress",
                table: "radacct",
                newName: "framedipaddress");

            migrationBuilder.RenameColumn(
                name: "Class",
                table: "radacct",
                newName: "class");

            migrationBuilder.RenameColumn(
                name: "CallingStationId",
                table: "radacct",
                newName: "callingstationid");

            migrationBuilder.RenameColumn(
                name: "CalledStationId",
                table: "radacct",
                newName: "calledstationid");

            migrationBuilder.RenameColumn(
                name: "AcctUpdateTime",
                table: "radacct",
                newName: "acctupdatetime");

            migrationBuilder.RenameColumn(
                name: "AcctUniqueId",
                table: "radacct",
                newName: "acctuniqueid");

            migrationBuilder.RenameColumn(
                name: "AcctTerminateCause",
                table: "radacct",
                newName: "acctterminatecause");

            migrationBuilder.RenameColumn(
                name: "AcctStopTime",
                table: "radacct",
                newName: "acctstoptime");

            migrationBuilder.RenameColumn(
                name: "AcctStartTime",
                table: "radacct",
                newName: "acctstarttime");

            migrationBuilder.RenameColumn(
                name: "AcctSessionTime",
                table: "radacct",
                newName: "acctsessiontime");

            migrationBuilder.RenameColumn(
                name: "AcctSessionId",
                table: "radacct",
                newName: "acctsessionid");

            migrationBuilder.RenameColumn(
                name: "AcctOutputOctets",
                table: "radacct",
                newName: "acctoutputoctets");

            migrationBuilder.RenameColumn(
                name: "AcctInterval",
                table: "radacct",
                newName: "acctinterval");

            migrationBuilder.RenameColumn(
                name: "AcctInputOctets",
                table: "radacct",
                newName: "acctinputoctets");

            migrationBuilder.RenameColumn(
                name: "RadAcctId",
                table: "radacct",
                newName: "radacctid");

            migrationBuilder.RenameIndex(
                name: "IX_radacct_AcctUniqueId",
                table: "radacct",
                newName: "IX_radacct_acctuniqueid");

            migrationBuilder.RenameIndex(
                name: "IX_radacct_AcctStartTime_UserName",
                table: "radacct",
                newName: "IX_radacct_acctstarttime_username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "username",
                table: "radusergroup",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "groupname",
                table: "radusergroup",
                newName: "GroupName");

            migrationBuilder.RenameIndex(
                name: "IX_radusergroup_username",
                table: "radusergroup",
                newName: "IX_radusergroup_UserName");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "radreply",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "radreply",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "attribute",
                table: "radreply",
                newName: "Attribute");

            migrationBuilder.RenameIndex(
                name: "IX_radreply_username_attribute",
                table: "radreply",
                newName: "IX_radreply_UserName_Attribute");

            migrationBuilder.RenameColumn(
                name: "class",
                table: "radpostauth",
                newName: "Class");

            migrationBuilder.RenameColumn(
                name: "callingstationid",
                table: "radpostauth",
                newName: "CallingStationId");

            migrationBuilder.RenameColumn(
                name: "calledstationid",
                table: "radpostauth",
                newName: "CalledStationId");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "radgroupreply",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "groupname",
                table: "radgroupreply",
                newName: "GroupName");

            migrationBuilder.RenameColumn(
                name: "attribute",
                table: "radgroupreply",
                newName: "Attribute");

            migrationBuilder.RenameIndex(
                name: "IX_radgroupreply_groupname_attribute",
                table: "radgroupreply",
                newName: "IX_radgroupreply_GroupName_Attribute");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "radgroupcheck",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "groupname",
                table: "radgroupcheck",
                newName: "GroupName");

            migrationBuilder.RenameColumn(
                name: "attribute",
                table: "radgroupcheck",
                newName: "Attribute");

            migrationBuilder.RenameIndex(
                name: "IX_radgroupcheck_groupname_attribute",
                table: "radgroupcheck",
                newName: "IX_radgroupcheck_GroupName_Attribute");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "radcheck",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "radcheck",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "attribute",
                table: "radcheck",
                newName: "Attribute");

            migrationBuilder.RenameIndex(
                name: "IX_radcheck_username_attribute",
                table: "radcheck",
                newName: "IX_radcheck_UserName_Attribute");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "radacct",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "servicetype",
                table: "radacct",
                newName: "ServiceType");

            migrationBuilder.RenameColumn(
                name: "realm",
                table: "radacct",
                newName: "Realm");

            migrationBuilder.RenameColumn(
                name: "nasporttype",
                table: "radacct",
                newName: "NASPortType");

            migrationBuilder.RenameColumn(
                name: "nasportid",
                table: "radacct",
                newName: "NASPortId");

            migrationBuilder.RenameColumn(
                name: "nasipaddress",
                table: "radacct",
                newName: "NASIPAddress");

            migrationBuilder.RenameColumn(
                name: "framedprotocol",
                table: "radacct",
                newName: "FramedProtocol");

            migrationBuilder.RenameColumn(
                name: "framedipv6address",
                table: "radacct",
                newName: "FramedIPv6Address");

            migrationBuilder.RenameColumn(
                name: "framedipaddress",
                table: "radacct",
                newName: "FramedIPAddress");

            migrationBuilder.RenameColumn(
                name: "class",
                table: "radacct",
                newName: "Class");

            migrationBuilder.RenameColumn(
                name: "callingstationid",
                table: "radacct",
                newName: "CallingStationId");

            migrationBuilder.RenameColumn(
                name: "calledstationid",
                table: "radacct",
                newName: "CalledStationId");

            migrationBuilder.RenameColumn(
                name: "acctupdatetime",
                table: "radacct",
                newName: "AcctUpdateTime");

            migrationBuilder.RenameColumn(
                name: "acctuniqueid",
                table: "radacct",
                newName: "AcctUniqueId");

            migrationBuilder.RenameColumn(
                name: "acctterminatecause",
                table: "radacct",
                newName: "AcctTerminateCause");

            migrationBuilder.RenameColumn(
                name: "acctstoptime",
                table: "radacct",
                newName: "AcctStopTime");

            migrationBuilder.RenameColumn(
                name: "acctstarttime",
                table: "radacct",
                newName: "AcctStartTime");

            migrationBuilder.RenameColumn(
                name: "acctsessiontime",
                table: "radacct",
                newName: "AcctSessionTime");

            migrationBuilder.RenameColumn(
                name: "acctsessionid",
                table: "radacct",
                newName: "AcctSessionId");

            migrationBuilder.RenameColumn(
                name: "acctoutputoctets",
                table: "radacct",
                newName: "AcctOutputOctets");

            migrationBuilder.RenameColumn(
                name: "acctinterval",
                table: "radacct",
                newName: "AcctInterval");

            migrationBuilder.RenameColumn(
                name: "acctinputoctets",
                table: "radacct",
                newName: "AcctInputOctets");

            migrationBuilder.RenameColumn(
                name: "radacctid",
                table: "radacct",
                newName: "RadAcctId");

            migrationBuilder.RenameIndex(
                name: "IX_radacct_acctuniqueid",
                table: "radacct",
                newName: "IX_radacct_AcctUniqueId");

            migrationBuilder.RenameIndex(
                name: "IX_radacct_acctstarttime_username",
                table: "radacct",
                newName: "IX_radacct_AcctStartTime_UserName");
        }
    }
}
