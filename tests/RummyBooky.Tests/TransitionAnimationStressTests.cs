using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RummyBooky.Tests;

public class TransitionAnimationStressTests
{
    public class CardBoxStateMock
    {
        public bool IsExpanded { get; private set; }
        public bool CollapsedVisible { get; set; } = true;
        public double CollapsedOpacity { get; set; } = 1.0;
        public double CollapsedScale { get; set; } = 1.0;
        public bool ExpandedVisible { get; set; } = false;
        public double ExpandedOpacity { get; set; } = 0.0;
        public double ExpandedScale { get; set; } = 1.0;

        public int AnimationTriggerCount { get; set; }
        public int CancelCount { get; set; }
        private CancellationTokenSource? _activeCts;
        private readonly object _lock = new();

        public async Task OnCardBoxTappedAsync()
        {
            if (IsExpanded) return;
            IsExpanded = true;
            await TransitionAsync(expand: true);
        }

        public async Task OnEmptyCardBoxTappedAsync()
        {
            if (!IsExpanded) return;
            IsExpanded = false;
            await TransitionAsync(expand: false);
        }

        public async Task TransitionAsync(bool expand, int simulatedDurationMs = 50)
        {
            CancellationTokenSource cts;
            lock (_lock)
            {
                if (_activeCts != null)
                {
                    _activeCts.Cancel();
                    CancelCount++;
                }
                cts = new CancellationTokenSource();
                _activeCts = cts;
                AnimationTriggerCount++;
            }

            try
            {
                if (expand)
                {
                    ExpandedOpacity = 0.0;
                    ExpandedScale = 0.95;
                    ExpandedVisible = true;

                    // Simulate parallel animation
                    await Task.Delay(simulatedDurationMs, cts.Token);

                    CollapsedOpacity = 0.0;
                    CollapsedScale = 0.95;
                    ExpandedOpacity = 1.0;
                    ExpandedScale = 1.0;
                    CollapsedVisible = false;
                }
                else
                {
                    CollapsedOpacity = 0.0;
                    CollapsedScale = 0.95;
                    CollapsedVisible = true;

                    // Simulate parallel animation
                    await Task.Delay(simulatedDurationMs, cts.Token);

                    ExpandedOpacity = 0.0;
                    ExpandedScale = 0.95;
                    CollapsedOpacity = 1.0;
                    CollapsedScale = 1.0;
                    ExpandedVisible = false;
                }
            }
            catch (OperationCanceledException)
            {
                // Animation was cancelled by next transition
            }
        }
    }

    [Fact]
    public async Task RapidConsecutiveTaps_CardBox_OnlyTriggersSingleExpand()
    {
        var mock = new CardBoxStateMock();

        // Simulate 20 rapid taps on collapsed card box
        var tasks = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(mock.OnCardBoxTappedAsync());
        }

        await Task.WhenAll(tasks);

        // State must be expanded, animation triggered exactly once
        Assert.True(mock.IsExpanded);
        Assert.Equal(1, mock.AnimationTriggerCount);
        Assert.True(mock.ExpandedVisible);
        Assert.False(mock.CollapsedVisible);
        Assert.Equal(1.0, mock.ExpandedOpacity);
        Assert.Equal(0.0, mock.CollapsedOpacity);
    }

    [Fact]
    public async Task RapidToggling_ExpandAndCollapse_CancelsPriorAndResolvesToConsistentState()
    {
        var mock = new CardBoxStateMock();

        // Start expanding
        var expandTask = mock.OnCardBoxTappedAsync();

        // Immediately tap to collapse while expand is in flight (10ms in)
        await Task.Delay(10, TestContext.Current.CancellationToken);
        var collapseTask = mock.OnEmptyCardBoxTappedAsync();

        await Task.WhenAll(expandTask, collapseTask);

        // State must resolve to collapsed
        Assert.False(mock.IsExpanded);
        Assert.True(mock.CollapsedVisible);
        Assert.False(mock.ExpandedVisible);
        Assert.Equal(1.0, mock.CollapsedOpacity);
        Assert.Equal(0.0, mock.ExpandedOpacity);
        Assert.True(mock.CancelCount >= 1, "Prior in-flight transition should have been cancelled");
    }

    [Fact]
    public async Task ConsecutiveExpandCollapseCycles_PreserveVisualIntegrity()
    {
        var mock = new CardBoxStateMock();

        for (int cycle = 0; cycle < 10; cycle++)
        {
            await mock.OnCardBoxTappedAsync();
            Assert.True(mock.IsExpanded);
            Assert.True(mock.ExpandedVisible);
            Assert.False(mock.CollapsedVisible);

            await mock.OnEmptyCardBoxTappedAsync();
            Assert.False(mock.IsExpanded);
            Assert.True(mock.CollapsedVisible);
            Assert.False(mock.ExpandedVisible);
        }

        Assert.Equal(20, mock.AnimationTriggerCount);
        Assert.Equal(1.0, mock.CollapsedOpacity);
        Assert.Equal(0.0, mock.ExpandedOpacity);
    }
}
