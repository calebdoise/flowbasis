using FlowBasis.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Flows.Sql
{
    public class SqlFlowStateProvider : FlowStateProvider
    {
        private Func<SqlConnection> sqlConnectionProvider;

        public SqlFlowStateProvider(Func<SqlConnection> sqlConnectionProvider)
        {
            this.sqlConnectionProvider = sqlConnectionProvider;
        }

        public override FlowStateHandle CreateFlowState(NewFlowStateOptions options)
        {
            var connection = this.sqlConnectionProvider();

            string id = this.GetNewFlowStateId(options.FixedProperties);

            string fixedPropertiesJson = this.ToJson(options.FixedProperties);            
            string progressStateJson = this.ToJson(options.ProgressState);

            string stateJson = null;
            if (options.State != null)
            {
                stateJson = this.ToJson(options.State);
            }
            else
            {
                stateJson = options.StateJson;
            }

            var utcNow = DateTime.UtcNow;

            string lockCode = null;
            DateTime? lockExpiresAtUtc = null;
            if (options.Lock)
            {
                lockCode = Guid.NewGuid().ToString("N");

                if (options.LockDuration != null)
                {
                    lockExpiresAtUtc = utcNow.Add(options.LockDuration.Value);
                }
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("fixedPropertiesJson", fixedPropertiesJson ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("stateJson", stateJson ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("progressStateJson", progressStateJson ?? (object)DBNull.Value);                
                cmd.Parameters.AddWithValue("expiresAtUtc", options.ExpiresAtUtc ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("lockCode", lockCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("lockExpiresAtUtc", lockExpiresAtUtc ?? (object)DBNull.Value);

                // TODO: Provide more explicit error when there is a duplicate id (in cases where fixed id is used).

                cmd.CommandText = @"
INSERT INTO Flows.FlowState (Id, FixedPropertiesJson, ProgressStateJson, StateJson, ExpiresAtUtc, LockCode, LockExpiresAtUtc, ProgressStateVersion, StateVersion)
VALUES (@id, @fixedPropertiesJson, @progressStateJson, @stateJson, @expiresAtUtc, @lockCode, @lockExpiresAtUtc, 0, 0);
";

                cmd.ExecuteNonQuery();
            }

            var sqlFlowState = new SqlFlowStateData
            {
                Id = id,
                FixedPropertiesJson = fixedPropertiesJson,
                ProgressStateJson = progressStateJson,
                StateJson = stateJson,
                ExpiresAtUtc = options.ExpiresAtUtc,
                LockCode = lockCode,
                LockExpiresAtUtc = lockExpiresAtUtc,
                ProgressStateVersion = 0,
                StateVersion = 0
            };

            var handle = new SqlFlowStateHandle(this, sqlFlowState);

            return handle;
        }

        public override FlowStateHandle GetFlowState(string flowStateId, OpenFlowStateOptions options = null)
        {
            var connection = this.sqlConnectionProvider();

            using (var cmd = connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("flowStateId", flowStateId);
                cmd.Parameters.AddWithValue("lock", options?.Lock ?? false);
                cmd.Parameters.AddWithValue("lockDurationMs", options?.LockDuration?.TotalMilliseconds ?? (object)DBNull.Value);

                cmd.CommandText = @"

BEGIN TRY
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRANSACTION;

    DECLARE @id nvarchar(255);
    DECLARE @fixedPropertiesJson nvarchar(max);
    DECLARE @progressStateJson nvarchar(max);
    DECLARE @stateJson nvarchar(max);
    DECLARE @progressStateVersion bigint;
    DECLARE @stateVersion bigint;
    DECLARE @expiresAtUtc datetime;
    DECLARE @lockCode nvarchar(255);
    DECLARE @lockExpiresAtUtc datetime;

    DECLARE @utcNow datetime = GETUTCDATE();

    SELECT
        @id = Id, 
        @fixedPropertiesJson = FixedPropertiesJson,
        @progressStateJson = ProgressStateJson,
        @stateJson = StateJson,
        @progressStateVersion = ProgressStateVersion,
        @stateVersion = StateVersion,
        @expiresAtUtc = ExpiresAtUtc,
        @lockCode = LockCode,
        @lockExpiresAtUtc = LockExpiresAtUtc
    FROM Flows.FlowState WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
    WHERE Id = @flowStateId;

    DECLARE @hadError int = 0;

    IF (@lock = 1)
    BEGIN
        -- See if it is already locked.
        IF (@lockCode IS NOT NULL)
        BEGIN
            IF (@lockExpiresAtUtc IS NULL OR @lockExpiresAtUtc > @utcNow)
            BEGIN        
                SELECT 0 Success, 'AlreadyLocked' ErrorCode;
                SET @hadError = 1;
            END;
        END;    

        IF (@hadError = 0)
        BEGIN
            -- Take the lock.
            SET @lockCode = NEWID();
            SET @lockExpiresAtUtc = NULL;

            IF (@lockDurationMs IS NOT NULL)
            BEGIN            
                SET @lockExpiresAtUtc = DATEADD(millisecond, @lockDurationMs, GETUTCDATE())
            END;

            UPDATE Flows.FlowState SET LockCode = @lockCode, LockExpiresAtUtc = @lockExpiresAtUtc WHERE Id = @id;
        END;
    END;

    IF (@hadError = 0)
    BEGIN
        SELECT 1 Success, @fixedPropertiesJson, @progressStateJson, @stateJson, @progressStateVersion, @stateVersion, @expiresAtUtc, @lockCode, @lockExpiresAtUtc;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
	ROLLBACK TRANSACTION;
	THROW;
END CATCH
";

                using (var reader = cmd.ExecuteReader())
                {
                    Func<int, string> getString = (index) => !reader.IsDBNull(index) ? reader.GetString(index) : null;
                    Func<int, DateTime?> getDateTime = (index) => !reader.IsDBNull(index) ? reader.GetDateTime(index) : (DateTime?)null;

                    if (reader.Read())
                    {
                        int success = reader.GetInt32(0);
                        if (success == 1)
                        {                            
                            string fixedPropertiesJson = getString(1);
                            string progressStateJson = getString(2);
                            string stateJson = getString(3);

                            long progressStateVersion = reader.GetInt64(4);
                            long stateVersion = reader.GetInt64(5);

                            DateTime? expiresAtUtc = getDateTime(6);

                            string lockCode = getString(7);
                            DateTime? lockExpiresAtUtc = getDateTime(8);

                            var sqlFlowState = new SqlFlowStateData
                            {
                                Id = flowStateId,
                                FixedPropertiesJson = fixedPropertiesJson,
                                ProgressStateJson = progressStateJson,
                                StateJson = stateJson,
                                ExpiresAtUtc = expiresAtUtc,
                                LockCode = lockCode,
                                LockExpiresAtUtc = lockExpiresAtUtc,
                                ProgressStateVersion = progressStateVersion,
                                StateVersion = stateVersion
                            };

                            var handle = new SqlFlowStateHandle(this, sqlFlowState);

                            return handle;
                        }
                        else if (success == 0)
                        {
                            string errorCode = getString(1);

                            // TODO: Create a TryGetFlowState that returns info about the attempt to get the flow state.

                            throw new Exception("Error: " + errorCode);
                        }
                        else
                        {
                            throw new Exception("Expected 1 or 0 for success flag.");
                        }
                    }
                    else
                    {
                        throw new Exception("Expected at least one result row.");
                    }
                }
            }
        }

        private string ToJson(object value)
        {
            if (value == null)
            {
                return null;
            }

            return FlowBasis.Json.JsonSerializers.Default.Stringify(value);
        }

        private T FromJson<T>(string value)
        {
            if (value == null)
            {
                return default(T);
            }
            
            return FlowBasis.Json.JsonSerializers.Default.Parse<T>(value);            
        }


        private class SqlFlowStateHandle : FlowStateHandle
        {
            private SqlFlowStateProvider stateProvider;
            private SqlFlowStateData flowStateData;

            private IDictionary<string, string> fixedProperties;

            public SqlFlowStateHandle(SqlFlowStateProvider stateProvider, SqlFlowStateData flowStateData)
            {
                this.stateProvider = stateProvider;
                this.flowStateData = flowStateData;
            }

            public override void Dispose()
            {
                if (this.flowStateData.LockCode != null)
                {
                    this.Update(new UpdateFlowStateOptions
                    {
                        UpdateLockCommand = UpdateLockCommand.ReleaseLock
                    });
                }
            }

            public override string Id
            {
                get
                {
                    return this.flowStateData.Id;
                }
            }

            public override IDictionary<string, string> FixedProperties
            {
                get
                {
                    if (this.fixedProperties == null)
                    {
                        this.fixedProperties = this.stateProvider.FromJson<Dictionary<string, string>>(this.flowStateData.FixedPropertiesJson);
                        if (this.fixedProperties == null)
                        {
                            this.fixedProperties = new Dictionary<string, string>();
                        }
                    }

                    return this.fixedProperties;
                }
            }

            public override ProgressState ProgressState
            {
                get
                {
                    return this.stateProvider.FromJson<ProgressState>(this.flowStateData.ProgressStateJson);
                }
            }

            public override string StateJson
            {
                get
                {
                    return this.flowStateData.StateJson;
                }
            }

            public override T GetState<T>()
            {
                return this.stateProvider.FromJson<T>(this.flowStateData.StateJson);
            }

            public override DateTime? ExpiresAtUtc
            {
                get
                {
                    return this.flowStateData.ExpiresAtUtc.Value;
                }
            }


            public override void Update(UpdateFlowStateOptions options)
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                var connection = this.stateProvider.sqlConnectionProvider();

                using (var cmd = connection.CreateCommand())
                {
                    string newProgressStateJson = this.stateProvider.ToJson(options.NewProgressState);
                    string newStateJson = this.stateProvider.ToJson(options.NewState);

                    cmd.Parameters.AddWithValue("flowStateId", this.flowStateData.Id);
                    cmd.Parameters.AddWithValue("hasNewExpiresAtUtc", options.HasNewExpiresAtUtc);
                    cmd.Parameters.AddWithValue("hasNewProgressState", options.HasNewProgressState);
                    cmd.Parameters.AddWithValue("hasNewState", options.HasNewState);
                    cmd.Parameters.AddWithValue("releaseLock", options.UpdateLockCommand == UpdateLockCommand.ReleaseLock);
                    cmd.Parameters.AddWithValue("acquireOrExtendLock", options.UpdateLockCommand == UpdateLockCommand.AcquireOrExtendLock);
                    cmd.Parameters.AddWithValue("newExpiresAtUtc", options.NewExpiresAtUtc ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("newProgressStateJson", newProgressStateJson ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("newStateJson", newStateJson ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("currentLockCode", this.flowStateData.LockCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("currentProgressStateVersion", this.flowStateData.ProgressStateVersion);
                    cmd.Parameters.AddWithValue("currentStateVersion", this.flowStateData.StateVersion);

                    var paramNewLockDurationMs = cmd.Parameters.Add("newLockDurationMs", SqlDbType.Int);
                    paramNewLockDurationMs.Value = (int?)options?.NewLockDuration?.TotalMilliseconds ?? (object)DBNull.Value;

                    cmd.CommandText = @"
BEGIN TRY
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRANSACTION;
    
    DECLARE @progressStateJson nvarchar(max);
    DECLARE @stateJson nvarchar(max);
    DECLARE @progressStateVersion bigint;
    DECLARE @stateVersion bigint;
    DECLARE @expiresAtUtc datetime;
    DECLARE @lockCode nvarchar(255);
    DECLARE @lockExpiresAtUtc datetime;

    DECLARE @utcNow datetime = GETUTCDATE();

    SELECT        
        @progressStateJson = ProgressStateJson,
        @stateJson = StateJson,
        @progressStateVersion = ProgressStateVersion,
        @stateVersion = StateVersion,
        @expiresAtUtc = ExpiresAtUtc,
        @lockCode = LockCode,
        @lockExpiresAtUtc = LockExpiresAtUtc
    FROM Flows.FlowState WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
    WHERE Id = @flowStateId;

    IF (@expiresAtUtc IS NOT NULL AND @utcNow > @expiresAtUtc)
    BEGIN
        -- Already expired.
        DELETE FROM Flows.FlowState WHERE Id = @flowStateId;
        SELECT 0, 'AlreadyExpired';
    END
    ELSE
    BEGIN
        IF ((@lockCode IS NOT NULL AND @lockCode != @currentLockCode)
            AND (@lockExpiresAtUtc IS NULL OR @lockExpiresAtUtc > @utcNow))
        BEGIN
            -- Already locked elsewhere.
            SELECT 0, 'AlreadyLocked';
        END
        ELSE
        BEGIN
            DECLARE @hadError int = 0;
            DECLARE @errorCode nvarchar(255) = NULL;

            IF (@releaseLock = 1)
            BEGIN
                SET @lockCode = null;
                SET @lockExpiresAtUtc = null;
            END
            ELSE IF (@acquireOrExtendLock = 1)
            BEGIN
                IF (@currentLockCode IS NOT NULL)
                    SET @lockCode = @currentLockCode;
                ELSE
                    SET @lockCode = NEWID();
                
               IF (@newLockDurationMs IS NOT NULL)
                    SET @lockExpiresAtUtc = DATEADD(millisecond, @newLockDurationMs, @utcNow);
               ELSE
                    SET @lockExpiresAtUtc = NULL;
            END            

            IF (@hasNewProgressState = 1)
            BEGIN
                IF (@progressStateVersion = @currentProgressStateVersion)
                BEGIN
                    SET @progressStateJson = @newProgressStateJson;
                    SET @progressStateVersion = @progressStateVersion + 1;
                END
                ELSE
                BEGIN
                    SET @hadError = 1;
                    SET @errorCode = 'ProgressStateVersionMismatch';
                END
            END

            IF (@hasNewState = 1)
            BEGIN
                IF (@stateVersion = @currentStateVersion)
                BEGIN
                    SET @stateJson = @newStateJson;
                    SET @stateVersion = @stateVersion + 1;
                END
                ELSE
                BEGIN
                    SET @hadError = 1;
                    SET @errorCode = 'StateVersionMismatch';
                END
            END

            IF (@hasNewExpiresAtUtc = 1)
            BEGIN
                SET @expiresAtUtc = @newExpiresAtUtc;
            END

            IF (@hadError = 0)
            BEGIN
                UPDATE Flows.FlowState SET 
                    ProgressStateJson = @progressStateJson,
                    ProgressStateVersion = @progressStateVersion,
                    StateJson = @stateJson,
                    StateVersion = @stateVersion,
                    LockCode = @lockCode,
                    LockExpiresAtUtc = @lockExpiresAtUtc,
                    ExpiresAtUtc = @expiresAtUtc
                WHERE Id = @flowStateId;

                SELECT 1, @progressStateJson, @progressStateVersion, @stateJson, @stateVersion, @lockCode, @lockExpiresAtUtc, @expiresAtUtc
            END
            ELSE
            BEGIN
                SELECT 0, @errorCode;
            END
        END
    END

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
	ROLLBACK TRANSACTION;
	THROW;
END CATCH
";

                    using (var reader = cmd.ExecuteReader())
                    {
                        Func<int, string> getString = (index) => !reader.IsDBNull(index) ? reader.GetString(index) : null;
                        Func<int, DateTime?> getDateTime = (index) => !reader.IsDBNull(index) ? reader.GetDateTime(index) : (DateTime?)null;

                        if (reader.Read())
                        {
                            int success = reader.GetInt32(0);
                            if (success == 1)
                            {
                                string progressStateJson = getString(1);
                                long progressStateVersion = reader.GetInt64(2);
                                string stateJson = getString(3);
                                long stateVersion = reader.GetInt64(4);
                                string lockCode = getString(5);
                                DateTime? lockExpiresAtUtc = getDateTime(6);
                                DateTime? expiresAtUtc = getDateTime(7);

                                this.flowStateData.ProgressStateJson = progressStateJson;
                                this.flowStateData.ProgressStateVersion = progressStateVersion;
                                this.flowStateData.StateJson = stateJson;
                                this.flowStateData.StateVersion = stateVersion;
                                this.flowStateData.ExpiresAtUtc = expiresAtUtc;
                                this.flowStateData.LockCode = lockCode;
                                this.flowStateData.LockExpiresAtUtc = lockExpiresAtUtc;                                
                            }
                            else if (success == 0)
                            {
                                string errorCode = getString(1);

                                // TODO: Create a TryGetFlowState that returns info about the attempt to get the flow state.

                                throw new Exception("Error: " + errorCode);
                            }
                            else
                            {
                                throw new Exception("Expected 1 or 0 for success flag.");
                            }
                        }
                        else
                        {
                            throw new Exception("Expected at least one result row.");
                        }
                    }                        
                }
            }


            public override void Delete()
            {
                var connection = this.stateProvider.sqlConnectionProvider();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("flowStateId", this.flowStateData.Id);

                    cmd.CommandText = "DELETE FROM Flows.FlowState WHERE Id = @flowstateId";

                    cmd.ExecuteNonQuery();
                }

                // If we delete it, then we clear LockCode so that we don't try to release lock in Dispose method.
                this.flowStateData.LockCode = null;
            }
        }
    }


    internal class SqlFlowStateData
    {
        public string Id { get; set; }

        public string FixedPropertiesJson { get; set; }

        public string ProgressStateJson { get; set; }
        public string StateJson { get; set; }

        public long ProgressStateVersion { get; set; }
        public long StateVersion { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }

        public string LockCode { get; set; }
        public DateTime? LockExpiresAtUtc { get; set; }


        public SqlFlowStateData Clone()
        {
            return new SqlFlowStateData
            {
                Id = this.Id,
                FixedPropertiesJson = this.FixedPropertiesJson,
                ProgressStateJson = this.ProgressStateJson,
                StateJson = this.StateJson,
                ExpiresAtUtc = this.ExpiresAtUtc,
                LockCode = this.LockCode,
                LockExpiresAtUtc = this.LockExpiresAtUtc
            };
        }
    }
}
