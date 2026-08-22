
using IRI.Maptor.Core.Spatial.MapIndexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Tests.Common.Mapping
{
    public class MapIndexesTest
    {
        [Theory]
        // Arrange
        [InlineData("5261", 46.0, 35.5, 46.5, 36.0)]
        [InlineData("5372", 46.5, 41.0, 47.0, 41.5)]
        [InlineData("5483", 47.0, 46.5, 47.5, 47.0)]
        [InlineData("5050", 45.0, 30.0, 45.5, 30.5)]
        [InlineData("5165", 45.5, 37.5, 46.0, 38.0)]
        [InlineData("5242", 46.0, 26.0, 46.5, 26.5)]
        [InlineData("5340", 46.5, 25.0, 47.0, 25.5)]
        [InlineData("5430", 47.0, 20.0, 47.5, 20.5)]
        [InlineData("5535", 47.5, 22.5, 48.0, 23.0)]
        [InlineData("5641", 48.0, 25.5, 48.5, 26.0)]
        [InlineData("5733", 48.5, 21.5, 49.0, 22.0)]
        [InlineData("5844", 49.0, 27.0, 49.5, 27.5)]
        [InlineData("5949", 49.5, 29.5, 50.0, 30.0)]
        public void Get100kGeodeticExtent_ValidIndex_ReturnsCorrectExtent(string index100k, double expectedXMin, double expectedYMin, double expectedXMax, double expectedYMax)
        {
            // Act
            var sheet = GeodeticIndexes.Get100kIndexSheet(index100k);
            Assert.NotNull(sheet);

            var extent = sheet.GeodeticExtent;

            // Assert
            Assert.Equal(expectedXMin, extent.XMin);
            Assert.Equal(expectedYMin, extent.YMin);
            Assert.Equal(expectedXMax, extent.XMax);
            Assert.Equal(expectedYMax, extent.YMax);
        }
    }
    }
