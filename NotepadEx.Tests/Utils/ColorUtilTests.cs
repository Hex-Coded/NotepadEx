using Xunit;
using NotepadEx.Util;
using System.Windows.Media;

namespace NotepadEx.Tests.Utils
{
    public class ColorUtilTests
    {
        [Theory]
        [InlineData("#FFFF0000", 255, 255, 0, 0)]   // Opaque Red
        [InlineData("#FF00FF00", 255, 0, 255, 0)]   // Opaque Green
        [InlineData("#800000FF", 128, 0, 0, 255)]   // Semi-transparent Blue
        [InlineData("FFFFFF", 255, 255, 255, 255)] // White without hash
        public void HexStringToColor_ShouldParseCorrectly(string hex, byte a, byte r, byte g, byte b)
        {
            // Arrange
            var expectedColor = Color.FromArgb(a, r, g, b);

            // Act
            var actualColor = ColorUtil.HexStringToColor(hex);

            // Assert
            Assert.NotNull(actualColor);
            Assert.Equal(expectedColor, actualColor.Value);
        }

        [Theory]
        [InlineData("#FFFF0000")]
        [InlineData("#FF00FF00")]
        [InlineData("#800000FF")]
        [InlineData("#FFFFFFFF")]
        public void ColorToHexString_ShouldConvertCorrectly(string expectedHex)
        {
            // Arrange
            var color = (Color)ColorConverter.ConvertFromString(expectedHex);

            // Act
            var actualHex = ColorUtil.ColorToHexString(color);

            // Assert
            Assert.Equal(expectedHex, actualHex, ignoreCase: true);
        }

        [Fact]
        public void RgbHsv_RoundTrip_ShouldPreserveColor()
        {
            // Arrange
            var originalColor = Color.FromArgb(255, 128, 64, 192);

            // Act
            var (h, s, v) = ColorUtil.RgbToHsv(originalColor);
            var resultColor = ColorUtil.HsvToRgb(h, s, v, originalColor.A);

            // Assert
            Assert.Equal(originalColor.A, resultColor.A);
            Assert.Equal(originalColor.R, resultColor.R);
            Assert.Equal(originalColor.G, resultColor.G);
            Assert.Equal(originalColor.B, resultColor.B);
        }
    }
}