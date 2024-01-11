using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibVHTapStrap
{
    internal class MultitapPendingInputArgs(int mask, uint tapCnt)
    {
        public int Mask
        {
            get;
        } = mask;

        public uint TapCount
        {
            get;
        } = tapCnt;
    }

    internal class MultitapInputArgs(int mask, uint tapCnt)
    {
        public int Mask
        {
            get;
        } = mask;

        public uint TapCount
        {
            get;
        } = tapCnt;
    }

    interface IMutitapPendingDecider
    {
        bool MultiTapShouldBePending(int mask, uint tapCnt);
    }

    internal class MultitapComposer
    {
        public TimeSpan DoubleTapTimeout
        {
            get;
            set;
        } = TimeSpan.FromMilliseconds(300);

        internal event EventHandler<MultitapPendingInputArgs>? OnPendingInput;
        internal event EventHandler<MultitapInputArgs>? OnCommitInput;

        public IMutitapPendingDecider? PendingDecider
        {
            get;
            set;
        }

        int lastTapMask = 0;
        long lastTapTick = 0;
        uint multiTapCount = 0;
        bool hasPending = false;

        public void Reset()
        {
        }

        private CancellationTokenSource delayCancellationTokenSource = new CancellationTokenSource();
        public void OnTap(int tapMask)
        {
            delayCancellationTokenSource.Cancel();
            delayCancellationTokenSource = new CancellationTokenSource();

            var now = DateTime.Now.Ticks;

            if (tapMask == lastTapMask && now - lastTapTick <= DoubleTapTimeout.Ticks)
            {
                ++multiTapCount;
            }
            else
            {
                FlushPending();

                multiTapCount = 0;
            }

            lastTapMask = tapMask;
            lastTapTick = now;
            hasPending = true;

            if (!(PendingDecider?.MultiTapShouldBePending(tapMask, multiTapCount) ?? true))
            {
                FlushPending();
                return;
            }

            OnPendingInput?.Invoke(this, new MultitapPendingInputArgs(tapMask, multiTapCount));

            Task.Delay(DoubleTapTimeout, delayCancellationTokenSource.Token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    FlushPending();
                }
            }, TaskScheduler.Default);
        }

        private void FlushPending()
        {
            if (hasPending)
            {
                hasPending = false;
                OnCommitInput?.Invoke(this, new MultitapInputArgs(lastTapMask, multiTapCount));
                lastTapMask = 0;
                multiTapCount = 0;
            }
        }
    }
}
