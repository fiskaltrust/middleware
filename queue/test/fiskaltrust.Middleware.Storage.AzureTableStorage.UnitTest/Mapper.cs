using FluentAssertions;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Extensions;
using System;
using System.Linq;
using Xunit;
using Azure.Data.Tables;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Tests.Mapping
{
    public class MapperTests
    {
        [Fact]
        public void SetOversized_ShouldSetProperty_WhenValueIsNotOversized()
        {
            // Arrange
            var entity = new TableEntity();
            var property = "PropertyName";
            var value = "This is a test value";

            // Act
            Mapper.SetOversized(entity, property, value);

            // Assert
            entity.Should().ContainKey(property);
            entity[property].Should().Be(value);
        }

        [Fact]
        public void SetOversized_ShouldSetMultipleProperties_WhenValueIsOversized()
        {
            // Arrange
            var entity = new TableEntity();
            var property = "PropertyName";
            var value = new string('A', 64_000); // Oversized value

            // Act
            Mapper.SetOversized(entity, property, value);

            // Assert
            var expectedChunks = value.Chunk(32_000).ToList();
            var chunkCount = expectedChunks.Count;
            for (var i = 0; i < chunkCount; i++)
            {
                var chunkProperty = $"{property}_oversize_{i}";
                entity.Should().ContainKey(chunkProperty);
                entity[chunkProperty].Should().Be(expectedChunks[i]);
            }
        }

        [Fact]
        public void GetOversized_ShouldReturnOriginalValue_WhenValueIsNotOversized()
        {
            // Arrange
            var entity = new TableEntity();
            var property = "PropertyName";
            var value = "This is a test value";
            entity[property] = value;

            // Act
            var result = Mapper.GetOversized(entity, property);

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public void GetOversized_ShouldReturnConcatenatedValue_WhenValueIsOversized()
        {
            // Arrange
            var entity = new TableEntity();
            var property = "PropertyName";
            var chunks = new[]
            {
                "This is the first chunk",
                "This is the second chunk",
                "This is the third chunk",
                "This is the fourth chunk",
                "This is the fifth chunk",
                "This is the sixth chunk",
                "This is the seventh chunk",
                "This is the eighth chunk",
                "This is the ninth chunk",
                "This is the tenth chunk",
                "This is the eleventh chunk",
                "This is the twelfth chunk",
                "This is the thirteenth chunk",
                "This is the fourteenth chunk",
                "This is the fifteenth chunk",
                "This is the sixteenth chunk",
                "This is the seventeenth chunk",
            };
            var chunkCount = chunks.Length;
            foreach (var (i, chunk) in chunks.Select((x, i) => (i, x)).Reverse())
            {
                var chunkProperty = $"{property}_oversize_{i}";
                entity[chunkProperty] = chunks[i];
            }

            // Act
            var result = Mapper.GetOversized(entity, property);

            // Assert
            result.Should().Be(string.Concat(chunks));
        }

        [Fact]
        public void GetHashString_ShouldReturnHashString()
        {
            // Arrange
            long value = 123456789;

            // Act
            var result = Mapper.GetHashString(value);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().MatchRegex("^[0-9A-F]{2}(-[0-9A-F]{2}){7}$");
        }
    }
}