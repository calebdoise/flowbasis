using FlowBasis.Flows;
using FlowBasis.Flows.InMem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasisFlowsUnitTests.InMem
{
    [TestClass]
    public class InMemFlowStateProviderTests
    {
        [TestMethod]
        public void Test_InMemFlowStateProvider_Basics()
        {
            FlowStateProvider stateProvider = new InMemFlowStateProvider();

            Test_FlowStateProvider_Basics(stateProvider);
        }

        /// <summary>
        /// This is a static test method that is also called from the SqlFlowStateProvider tests.
        /// </summary>        
        internal static void Test_FlowStateProvider_Basics(FlowStateProvider stateProvider)
        {
            var stateHandle = stateProvider.CreateFlowState(new NewFlowStateOptions
            {
                FixedProperties = new Dictionary<string, string>()
                {
                    { "Task", "Something1" },
                    { "CreatedBy", "Foo" }
                },
                StateJson = "{\"hello\": \"world\"}"
            });

            var stateId = stateHandle.Id;

            var retrievedState = stateProvider.GetFlowState(stateId, new OpenFlowStateOptions
            {
                Lock = true,
                LockDuration = TimeSpan.FromDays(1)
            });

            Assert.AreEqual("Something1", retrievedState.FixedProperties["Task"]);
            Assert.AreEqual("Foo", retrievedState.FixedProperties["CreatedBy"]);

            dynamic state = retrievedState.GetState<object>();
            Assert.AreEqual("world", state.hello);

            dynamic newState = new FlowBasis.Json.JObject();
            newState.hello = "newworld";

            retrievedState.Update(new UpdateFlowStateOptions
            {
                NewState = newState,
                NewProgressState = new ProgressState
                {
                    Current = 4,
                    Total = 10,
                    Message = "msg",
                    StatusFlag = 0
                },
                UpdateLockCommand = UpdateLockCommand.ReleaseLock
            });

            // Get state a second time.
            var retrievedState2 = stateProvider.GetFlowState(stateId, new OpenFlowStateOptions
            {
                Lock = true,
                LockDuration = TimeSpan.FromDays(1)
            });

            dynamic state2 = retrievedState2.GetState<object>();
            Assert.AreEqual("newworld", state2.hello);
            Assert.AreEqual("world", state.hello);

            var progressState2 = retrievedState2.ProgressState;
            Assert.AreEqual(4, progressState2.Current);
            Assert.AreEqual(10, progressState2.Total);
            Assert.AreEqual("msg", progressState2.Message);
            Assert.AreEqual(0, progressState2.StatusFlag);
        }
    }
}