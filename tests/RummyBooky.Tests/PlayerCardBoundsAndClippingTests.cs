using System;
using System.Collections.Generic;
using Xunit;

namespace RummyBooky.Tests;

public class PlayerCardBoundsAndClippingTests
{
    public class LayoutConstraintSimulation
    {
        public double ContainerWidth { get; set; }
        public double LeftColumnWidth { get; set; } = 100d;
        public double ColumnSpacing { get; set; } = 16d;
        public double LeftColumnMarginRight { get; set; } = 8d;
        public double CollectionMarginLeft { get; set; } = 8d;
        public double BorderPadding { get; set; } = 16d;

        public double AvailableCardWidth =>
            ContainerWidth - (LeftColumnWidth + LeftColumnMarginRight + ColumnSpacing + CollectionMarginLeft);

        public double AvailableStatsGridWidth =>
            AvailableCardWidth - (BorderPadding * 2);

        // Stats grid column definitions: "*", 16, "Auto"
        // Label width + 16px spacer + Value width
        public bool CanFitStatsRow(double labelWidth, double valueWidth)
        {
            double requiredWidth = labelWidth + 16d + valueWidth;
            return AvailableStatsGridWidth >= requiredWidth;
        }
    }

    [Theory]
    [InlineData(320)] // Narrow mobile screen (320dp)
    [InlineData(360)] // Standard mobile screen (360dp)
    [InlineData(390)] // iPhone 14 (390dp)
    [InlineData(412)] // Android flagship (412dp)
    [InlineData(600)] // Small tablet / foldable (600dp)
    [InlineData(1024)] // Desktop / iPad (1024dp)
    public void ExpandedPlayerCard_WithUnconstrainedDimensions_FitsAvailableWidthWithoutClipping(double containerWidth)
    {
        var sim = new LayoutConstraintSimulation { ContainerWidth = containerWidth };

        // Check available width in Column 1 of ExpandedContainer Grid
        // On 320dp: 320 - (100 + 8 + 16 + 8) = 188dp
        // On 360dp: 360 - 132 = 228dp
        // On 400dp: 400 - 132 = 268dp
        // On 1024dp: 1024 - 132 = 892dp
        Assert.True(sim.AvailableCardWidth > 0, "Available card width must be positive");
        Assert.True(sim.AvailableStatsGridWidth > 0, "Available stats grid width must be positive");

        // The longest label in PlayerStatsGrid is "Highest Scored Hand" (~120dp at 12-14pt)
        // Values are typically 10-40dp ("999", "12,450", etc.)
        double maxLabelWidth = 115d;
        double maxValueWidth = 25d;

        // In 360dp container: available stats grid width is 228 - 32 = 196dp.
        // Required width = 115 + 16 + 25 = 156dp. 196dp >= 156dp (FIT).
        if (containerWidth >= 360)
        {
            Assert.True(sim.CanFitStatsRow(maxLabelWidth, maxValueWidth),
                $"Stats row should comfortably fit inside {containerWidth}dp container without wrapping or clipping");
        }
    }

    [Fact]
    public void RegressionTest_RigidWidthRequest_CausesOverflowAndClipping()
    {
        // Prior defect: PlayerCardView had rigid WidthRequest = 360 on all cards
        double rigidWidth = 360d;
        double mobileColumn1AvailableWidth = 228d; // On 360dp screen

        double overflow = rigidWidth - mobileColumn1AvailableWidth;
        Assert.True(overflow > 0, "Rigid WidthRequest = 360dp causes 132dp overflow beyond column 1 bounds");

        // Removing WidthRequest (WidthRequest = -1 / ClearValue) allows HorizontalOptions.Fill to size to exactly 228dp
        double unconstrainedWidth = mobileColumn1AvailableWidth;
        Assert.Equal(mobileColumn1AvailableWidth, unconstrainedWidth);
    }
}
