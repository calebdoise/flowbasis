using FlowBasis.Flows.Sql;
using FlowBasisFlowsUnitTests.InMem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasisFlowsUnitTests.Sql
{
    [TestClass]
    public class SqlFlowStateProviderTests
    {

        [TestMethod]
        public void Test_SqlFlowStateProvider_Basics()
        {
            var testDb = WinIntegrationTesting.SqlLocalDbDatabase.Create(
                new WinIntegrationTesting.SqlLocalDbCreateDatabaseOptions
                {
                    DatabaseName = "SqlFlowStateProviderTests",
                    DeleteAfterThisProcessExits = true,
                    DeleteExistingDatabaseAtSamePath = true,                    
                });

            using (var connection = new SqlConnection(testDb.ConnectionString))
            {
                connection.Open();

                FlowBasis.Flows.Sql.Migrations.Migration1.MigrateUp(connection);

                var stateProvider = new SqlFlowStateProvider(() => connection);

                InMemFlowStateProviderTests.Test_FlowStateProvider_Basics(stateProvider);
            }
        }

    }
}
