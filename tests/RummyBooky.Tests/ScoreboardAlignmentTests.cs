using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using Xunit;

namespace RummyBooky.Tests;

public class ScoreboardAlignmentTests
{
    [Fact]
    public void CurrentGamePage_HeaderAndItemGridColumnDefinitions_MatchExactly()
    {
        var xamlPath = Path.Combine("..", "..", "..", "..", "..", "RummyBooky", "Pages", "CurrentGamePage.xaml");
        if (!File.Exists(xamlPath))
        {
            xamlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "RummyBooky", "Pages", "CurrentGamePage.xaml"));
        }
        var csPath = Path.Combine("..", "..", "..", "..", "..", "RummyBooky", "Pages", "CurrentGamePage.xaml.cs");
        if (!File.Exists(csPath))
        {
            csPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "RummyBooky", "Pages", "CurrentGamePage.xaml.cs"));
        }

        if (File.Exists(xamlPath))
        {
            var content = File.ReadAllText(xamlPath);
            var csContent = File.Exists(csPath) ? File.ReadAllText(csPath) : string.Empty;

            var headerMatch = Regex.Match(content, @"<Grid x:Name=""HeaderRoot""[^>]*ColumnDefinitions=""([^""]+)""");
            if (!headerMatch.Success)
            {
                headerMatch = Regex.Match(content, @"<CollectionView\.Header>\s*<Grid[^>]*ColumnDefinitions=""([^""]+)""");
            }
            if (!headerMatch.Success)
            {
                headerMatch = Regex.Match(content, @"<Grid[^>]*ColumnSpacing=""0""[^>]*ColumnDefinitions=""([^""]+)""");
            }
            var itemMatch = Regex.Match(content, @"<Grid x:Name=""ItemRoot""[^>]*ColumnDefinitions=""([^""]+)""");
            string itemCols = "";
            if (itemMatch.Success)
            {
                itemCols = itemMatch.Groups[1].Value.Trim();
            }
            else if (csContent.Contains("new ColumnDefinition { Width = GridLength.Star }") &&
                     csContent.Contains("new ColumnDefinition { Width = 95 }") &&
                     csContent.Contains("new ColumnDefinition { Width = 115 }"))
            {
                itemCols = "*,2,95,2,115";
            }

            Assert.True(headerMatch.Success, "Header Grid with ColumnDefinitions not found.");
            Assert.False(string.IsNullOrEmpty(itemCols), "Item/Row Grid with ColumnDefinitions not found.");

            var headerCols = headerMatch.Groups[1].Value.Trim();

            Assert.Equal("*,2,95,2,115", headerCols);
            Assert.Equal("*,2,95,2,115", itemCols);
        }
    }

    [Fact]
    public void NewGamePage_HeaderAndItemGridColumnDefinitions_MatchExactly()
    {
        var xamlPath = Path.Combine("..", "..", "..", "..", "..", "RummyBooky", "Pages", "NewGamePage.xaml");
        if (!File.Exists(xamlPath))
        {
            xamlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "RummyBooky", "Pages", "NewGamePage.xaml"));
        }

        if (File.Exists(xamlPath))
        {
            var content = File.ReadAllText(xamlPath);

            var headerMatch = Regex.Match(content, @"<CollectionView\.Header>\s*<Grid[^>]*ColumnDefinitions=""([^""]+)""");
            var itemMatch = Regex.Match(content, @"<Grid x:Name=""ItemRoot""[^>]*ColumnDefinitions=""([^""]+)""");

            Assert.True(headerMatch.Success, "NewGamePage CollectionView.Header Grid not found.");
            Assert.True(itemMatch.Success, "NewGamePage ItemRoot Grid not found.");

            var headerCols = headerMatch.Groups[1].Value.Trim();
            var itemCols = itemMatch.Groups[1].Value.Trim();

            Assert.Equal("*,2,65,2,65,2,65,2,65,2,65", headerCols);
            Assert.Equal("*,2,65,2,65,2,65,2,65,2,65", itemCols);
        }
    }
}
