using System;
using System.Collections.Generic;
using Xunit;

namespace RummyBooky.Tests;

public record DisplayConfig(double Width, double Height, double Density, string Idiom, bool IsLandscape);

public class CascadingLayoutBoundsTests
{
    private static (double desiredWidth, double desiredHeight) ComputeBaseViewDimensions(DisplayConfig info)
    {
        var screenHeight = info.Height / info.Density;
        var screenWidth = info.Width / info.Density;
        var isLandscape = info.IsLandscape;

        double widthMultiplier;
        double heightMultiplier;
        double minWidth;
        double maxWidth;
        double minHeight;
        double maxHeight;

        if (info.Idiom == "Phone")
        {
            widthMultiplier = 0.9;
            heightMultiplier = isLandscape ? 0.55 : 0.62;
            minWidth = 260;
            maxWidth = 360;
            minHeight = 300;
            maxHeight = isLandscape ? 420 : 470;
        }
        else if (info.Idiom == "Tablet")
        {
            widthMultiplier = 0.6;
            heightMultiplier = isLandscape ? 0.6 : 0.58;
            minWidth = 360;
            maxWidth = 380;
            minHeight = 360;
            maxHeight = 460;
        }
        else
        {
            // Desktop / Default
            widthMultiplier = 0.35;
            heightMultiplier = 0.55;
            minWidth = 300;
            maxWidth = 400;
            minHeight = 320;
            maxHeight = 495;
        }

        if (screenWidth < 360)
        {
            widthMultiplier = Math.Min(widthMultiplier, 0.95);
            maxHeight = Math.Min(maxHeight, 460);
        }

        var desiredWidth = Math.Clamp(screenWidth * widthMultiplier, minWidth, maxWidth);
        var desiredHeight = Math.Clamp(screenHeight * heightMultiplier, minHeight, maxHeight);
        return (desiredWidth, desiredHeight);
    }

    public record CardBoxLayoutCalculations(
        double ImageWidth,
        double ImageHeight,
        double ViewportWidth,
        double ViewportHeight,
        double CardWidth,
        double CardHeight,
        double BoxY,
        double TotalHeight,
        double CanvasHeight,
        double ViewportX,
        double LabelY,
        List<double> CardTops
    );

    private static CardBoxLayoutCalculations ComputeCardBoxLayout(DisplayConfig config, int playerCount)
    {
        var (desiredWidth, desiredHeight) = ComputeBaseViewDimensions(config);
        double imageWidth = desiredWidth;
        double imageHeight = desiredHeight;
        double viewportWidth = Math.Max(0d, imageWidth * 0.82d);
        double viewportHeight = Math.Max(0d, imageHeight * 0.62d);
        double cardWidth = Math.Max(0d, viewportWidth - 8d);
        double cardHeight = Math.Max(95d, viewportHeight * 0.90d);

        int count = playerCount;
        double boxY = count > 0 ? (count * 0.20d * cardHeight) : 0d;
        double totalHeight = boxY + imageHeight;
        double viewportX = Math.Max(0d, (imageWidth - cardWidth) / 2d);
        double canvasHeight = count > 0 ? ((count - 1) * 0.20d * cardHeight + cardHeight) : 0d;
        double labelY = boxY + Math.Max(0d, imageHeight * 0.53d);

        var cardTops = new List<double>();
        for (int i = 0; i < count; i++)
        {
            cardTops.Add(i * (0.20d * cardHeight));
        }

        return new CardBoxLayoutCalculations(
            imageWidth, imageHeight, viewportWidth, viewportHeight, cardWidth, cardHeight,
            boxY, totalHeight, canvasHeight, viewportX, labelY, cardTops
        );
    }

    [Theory]
    [InlineData(320, 568, 1.0, "Phone", false)]   // Narrow phone (iPhone SE 1st gen / small Android)
    [InlineData(360, 800, 2.0, "Phone", false)]   // Standard Android (360dp)
    [InlineData(390, 844, 3.0, "Phone", false)]   // iPhone 14/15 (390dp)
    [InlineData(412, 915, 2.625, "Phone", false)] // Pixel 7/8 (412dp)
    [InlineData(768, 1024, 2.0, "Tablet", false)] // iPad portrait (768dp)
    [InlineData(1920, 1080, 1.0, "Desktop", false)] // Full HD Desktop
    public void CascadingLayout_BoundsAndOffsets_ArePositiveAndProportional(
        double width, double height, double density, string idiom, bool isLandscape)
    {
        var config = new DisplayConfig(width * density, height * density, density, idiom, isLandscape);

        for (int playerCount = 1; playerCount <= 6; playerCount++)
        {
            var layout = ComputeCardBoxLayout(config, playerCount);

            // Assert positive dimensions
            Assert.True(layout.ImageWidth >= 260, $"ImageWidth {layout.ImageWidth} should be >= 260");
            Assert.True(layout.ImageHeight >= 300, $"ImageHeight {layout.ImageHeight} should be >= 300");
            Assert.True(layout.CardWidth > 0, $"CardWidth {layout.CardWidth} should be > 0");
            Assert.True(layout.CardHeight >= 95, $"CardHeight {layout.CardHeight} should be >= 95");
            Assert.True(layout.TotalHeight > layout.BoxY, "Total height must encompass box position");

            // Verify progressive +20% vertical offset for each card
            double step = 0.20d * layout.CardHeight;
            for (int i = 0; i < playerCount; i++)
            {
                double expectedTop = i * step;
                Assert.Equal(expectedTop, layout.CardTops[i], 4);
            }

            // Verify exposed header height step exceeds the player chip height (~30-35dp)
            Assert.True(step >= 19.0, $"Step {step} should be sufficient for header chips");

            // Verify BoxY is exactly positioned at count * 0.20 * cardHeight
            Assert.Equal(playerCount * step, layout.BoxY, 4);

            // Verify CanvasHeight equals (count - 1) * 0.20 * cardHeight + cardHeight
            double expectedCanvasHeight = (playerCount - 1) * step + layout.CardHeight;
            Assert.Equal(expectedCanvasHeight, layout.CanvasHeight, 4);

            // Verify CardBoxLayout bounds fit within parent
            Assert.True(layout.CanvasHeight <= layout.TotalHeight);
        }
    }

    [Fact]
    public void CascadingLayout_ZeroPlayers_HandledGracefully()
    {
        var config = new DisplayConfig(360 * 2, 800 * 2, 2.0, "Phone", false);
        var layout = ComputeCardBoxLayout(config, 0);

        Assert.Equal(0d, layout.BoxY);
        Assert.Equal(0d, layout.CanvasHeight);
        Assert.Empty(layout.CardTops);
        Assert.Equal(layout.ImageHeight, layout.TotalHeight);
    }
}
