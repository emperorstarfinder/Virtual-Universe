﻿/*
 * Software license for this code will be added after the code is completed.
 * This code is in the early stages of development so should not be utilized 
 * at this time.
 * 
 * Thanks
 * Virtual Universe Development Team
 */

using System;

namespace VirtualUniverse.ClientStack
{
    /// <summary>
    ///     A hierarchical token bucket for bandwidth throttling. See
    ///     http://en.wikipedia.org/wiki/Token_bucket for more information
    /// </summary>
    public class TokenBucket
    {
        /// <summary>
        ///     Parent bucket to this bucket, or null if this is a root
        ///     bucket
        /// </summary>
        private readonly TokenBucket parent;

        /// <summary>
        ///     Number of tokens currently in the bucket
        /// </summary>
        private int content;

        /// <summary>
        ///     Time of the last drip, in system ticks
        /// </summary>
        private int lastDrip;

        /// <summary>
        ///     Size of the bucket in bytes. If zero, the bucket has
        ///     infinite capacity
        /// </summary>
        private int maxBurst;

        /// <summary>
        ///     Rate that the bucket fills, in bytes per millisecond. If
        ///     zero, the bucket always remains full
        /// </summary>
        private float tokensPerMS;

        #region Properties

        /// <summary>
        ///     The parent bucket of this bucket, or null if this bucket has no
        ///     parent. The parent bucket will limit the aggregate bandwidth of all
        ///     of its children buckets
        /// </summary>
        public TokenBucket Parent
        {
            get { return parent; }
        }

        /// <summary>
        ///     Maximum burst rate in bytes per second. This is the maximum number
        ///     of tokens that can accumulate in the bucket at any one time
        /// </summary>
        public int MaxBurst
        {
            get { return maxBurst; }
            set { maxBurst = (value >= 0 ? value : 0); }
        }

        /// <summary>
        ///     The speed limit of this bucket in bytes per second. This is the
        ///     number of tokens that are added to the bucket per second
        /// </summary>
        /// <remarks>
        ///     Tokens are added to the bucket any time
        ///     <seealso>
        ///         <cref>RemoveTokens</cref>
        ///     </seealso>
        ///     is called, at the granularity of
        ///     the system tick interval (typically around 15-22ms)
        /// </remarks>
        public float DripRate
        {
            get { return tokensPerMS * 1000; }
            set
            {
                if (value == 0)
                    tokensPerMS = 0;
                else
                {
                    float bpms = (int)(value / 1000.0f);

                    tokensPerMS = bpms <= 0.5f ? .5f : bpms;
                }
            }
        }

        /// <summary>
        ///     The speed limit of this bucket in bytes per millisecond
        /// </summary>
        public float DripPerMS
        {
            get { return tokensPerMS; }
        }

        /// <summary>
        ///     The number of bytes that can be sent at this moment. This is the
        ///     current number of tokens in the bucket
        ///     <remarks>
        ///         If this bucket has a parent bucket that does not have
        ///         enough tokens for a request,
        ///         <seealso>
        ///             <cref>RemoveTokens</cref>
        ///         </seealso>
        ///         will
        ///         return false regardless of the content of this bucket
        ///     </remarks>
        /// </summary>
        public int Content
        {
            get { return content; }
        }

        #endregion Properties

        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <param name="parent">
        ///     Parent bucket if this is a child bucket, or
        ///     null if this is a root bucket
        /// </param>
        /// <param name="maxBurst">
        ///     Maximum size of the bucket in bytes, or
        ///     zero if this bucket has no maximum capacity
        /// </param>
        /// <param name="dripRate">
        ///     Rate that the bucket fills, in bytes per
        ///     second. If zero, the bucket always remains full
        /// </param>
        public TokenBucket(TokenBucket parent, int maxBurst, int dripRate)
        {
            this.parent = parent;
            MaxBurst = maxBurst;
            DripRate = dripRate;
            lastDrip = Environment.TickCount & Int32.MaxValue;
        }

        /// <summary>
        ///     Remove a given number of tokens from the bucket
        /// </summary>
        /// <param name="amount">Number of tokens to remove from the bucket</param>
        /// <returns>
        ///     True if the requested number of tokens were removed from
        ///     the bucket, otherwise false
        /// </returns>
        public bool RemoveTokens(int amount)
        {
            bool dummy;
            return RemoveTokens(amount, out dummy);
        }

        /// <summary>
        ///     Remove a given number of tokens from the bucket
        /// </summary>
        /// <param name="amount">Number of tokens to remove from the bucket</param>
        /// <param name="dripSucceeded">
        ///     True if tokens were added to the bucket
        ///     during this call, otherwise false
        /// </param>
        /// <returns>
        ///     True if the requested number of tokens were removed from
        ///     the bucket, otherwise false
        /// </returns>
        public bool RemoveTokens(int amount, out bool dripSucceeded)
        {
            if (maxBurst == 0)
            {
                dripSucceeded = true;
                return true;
            }

            dripSucceeded = Drip();

            if (content - amount >= 0)
            {
                if (parent != null && !parent.RemoveTokens(amount))
                    return false;

                content -= amount;
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Add tokens to the bucket over time. The number of tokens added each
        ///     call depends on the length of time that has passed since the last
        ///     call to Drip
        /// </summary>
        /// <returns>True if tokens were added to the bucket, otherwise false</returns>
        public bool Drip()
        {
            if (tokensPerMS <= 0)
            {
                content = maxBurst;
                return true;
            }
            int now = Environment.TickCount & Int32.MaxValue;
            int deltaMS = now - lastDrip;

            if (deltaMS <= 0)
            {
                if (deltaMS < 0)
                    lastDrip = now;
                return false;
            }

            int dripAmount = (int)(deltaMS * tokensPerMS);

            content = Math.Min(content + dripAmount, maxBurst);
            lastDrip = now;
            /*
                            if (dripAmount < 0 || content < 0)
                                // sim has been idle for too long, integer has overflown
                                // previous calculation is meaningless, let's put it at correct max
                                content = maxBurst;
            */
            return true;
        }
    }
}