using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Flows.Sql.Migrations
{
    public static class Migration1
    {
        /// <summary>
        /// This applies the initial version of the standard flow state SQL tables.
        /// For a particular application, you may end up just cloning this code into your
        /// normal migration procedure and possibly override methods in the state provider
        /// or clone that code as well.
        /// </summary>        
        public static void MigrateUp(SqlConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE SCHEMA Flows;";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
CREATE TABLE Flows.FlowState(
    Id nvarchar(255) NOT NULL PRIMARY KEY,
    FixedPropertiesJson nvarchar(max),
    ProgressStateJson nvarchar(max),
    StateJson nvarchar(max),
    ProgressStateVersion bigint NOT NULL,
    StateVersion bigint NOT NULL,
    ExpiresAtUtc datetime,
    LockCode nvarchar(255),
    LockExpiresAtUtc datetime
);
";

                cmd.ExecuteNonQuery();
            }
        }        

        public static void MigrateDown(SqlConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DROP TABLE Flows.FlowState;";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DROP SCHEMA Flows;";
                cmd.ExecuteNonQuery();
            }
        }      
    }
}
