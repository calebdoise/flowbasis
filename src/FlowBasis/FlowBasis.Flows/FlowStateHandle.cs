using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Flows
{
    public abstract class FlowStateHandle
    {
        public abstract string Id { get; }

        public abstract IDictionary<string, string> FixedProperties { get; }

        public abstract ProgressState ProgressState { get; }

        public abstract T GetState<T>();
        public abstract string StateJson { get; }

        public abstract DateTime? ExpiresAtUtc { get; }

        public abstract void Update(UpdateFlowStateOptions options);

        public abstract void Delete();
    }


    public class UpdateFlowStateOptions
    {
        private object newProgressState;
        private bool hasNewProgressState;

        private object newState;
        private bool hasNewState;

        private DateTime? newExpiresAtUtc;
        private bool hasNewExpiresAtUtc;

        public object NewProgressState
        {
            get { return this.newProgressState; }
            set
            {
                this.newProgressState = value;
                this.hasNewProgressState = true;
            }
        }

        public bool HasNewProgressState
        {
            get { return this.hasNewProgressState; }
        }

        public object NewState
        {
            get { return this.newState; }
            set
            {
                this.newState = value;
                this.hasNewState = true;
            }
        }

        public bool HasNewState
        {
            get { return this.hasNewState; }
        }

        
        public DateTime? NewExpiresAtUtc
        {
            get { return this.newExpiresAtUtc; }
            set
            {
                this.newExpiresAtUtc = value;
                this.hasNewExpiresAtUtc = true;
            }
        }

        public bool HasNewExpiresAtUtc
        {
            get { return this.hasNewExpiresAtUtc; }
        }


        public UpdateLockCommand? UpdateLockCommand { get; set; }

        /// <summary>
        /// If UpdateLockCommand is AcquireOrExtendLock, the lock expiration will be set to the current time (UTC) 
        /// plus the duration indicated below, or lock will never expire if this is set to null.
        /// </summary>
        public TimeSpan? NewLockDuration { get; set; }
    }

    public enum UpdateLockCommand
    {
        ReleaseLock,
        AcquireOrExtendLock
    }
}
