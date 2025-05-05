using FluentAssertions;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Extensions;
using System;
using System.Linq;
using Xunit;
using Azure.Data.Tables;
using System.IO;
using System.IO.Compression;

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
        public void SetOversized_ShouldNotSetMultipleProperties_WhenValueIsOversizedButCompressable()
        {
            // Arrange
            var entity = new TableEntity();
            var property = "PropertyName";
            var value = new string('A', 128_000); // Oversized value

            // Act
            Mapper.SetOversized(entity, property, value);

            // Assert
            var chunkProperty = $"{property}_oversize_";
            entity.Should().ContainKey(chunkProperty + "0");
            entity.Should().NotContainKey(chunkProperty + "1");
        }

        [Fact]
        public void SetOversized_ShouldSetMultipleProperties_WhenValueIsOversized()
        {
            // Arrange
            var entity = new TableEntity();
            var property = "PropertyName";
            var random = new Random(42); // Seeded RNG for reproducibility
            var value = string.Concat(Enumerable.Range(0, 128_000).Select(_ =>
                random.Next(0, 2) == 0
                    ? (char) random.Next('A', 'Z' + 1)
                    : (char) random.Next('0', '9' + 1))); // Random alphanumeric oversized value

            // Act
            Mapper.SetOversized(entity, property, value);

            // Assert
            var expectedChunks = value.Chunk(32_000).ToList();
            var chunkCount = expectedChunks.Count;
            var chunkProperty = $"{property}_oversize_";
            entity.Should().ContainKey(chunkProperty + "0");
            entity.Should().ContainKey(chunkProperty + "1");
            entity.Should().NotContainKey(chunkProperty + "2");
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
            var random = new Random(42); // Seeded RNG for reproducibility
            var value = string.Concat(Enumerable.Range(0, 128_000).Select(_ =>
                random.Next(0, 2) == 0
                    ? (char) random.Next('A', 'Z' + 1)
                    : (char) random.Next('0', '9' + 1))); // Random alphanumeric oversized value

            using var memoryStream = new MemoryStream();
            using (var gzipStream = new DeflateStream(memoryStream, CompressionMode.Compress))
            using (var writer = new StreamWriter(gzipStream))
            {
                writer.Write(value);
            }
            var compressed = memoryStream.ToArray();

            foreach (var (i, chunk) in compressed.Chunk(64_000).Select((x, i) => (i, x)).Reverse())
            {
                var chunkProperty = $"{property}_oversize_{i}";
                entity[chunkProperty] = chunk.Array;
            }

            // Act
            var result = Mapper.GetOversized(entity, property);

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public void SetAndGetOversized_ShouldReturnValue_WhenValueIsNotOversized()
        {
            // Arrange
            var entity = new TableEntity();
            var property = "PropertyName";
            var random = new Random(42); // Seeded RNG for reproducibility
            var value = string.Concat(Enumerable.Range(0, 30_000).Select(_ =>
                random.Next(0, 2) == 0
                    ? (char) random.Next('A', 'Z' + 1)
                    : (char) random.Next('0', '9' + 1))); // Random alphanumeric oversized value

            // Act
            Mapper.SetOversized(entity, property, value);
            var result = Mapper.GetOversized(entity, property);

            // Assert
            result.Should().Be(value);
        }


        [Fact]
        public void SetAndGetOversized_ShouldReturnValue_WhenValueIsOversized()
        {
            // Arrange
            var entity = new TableEntity();
            var property = "PropertyName";
            var random = new Random(42); // Seeded RNG for reproducibility
            var value = string.Concat(Enumerable.Range(0, 128_000).Select(_ =>
                random.Next(0, 2) == 0
                    ? (char) random.Next('A', 'Z' + 1)
                    : (char) random.Next('0', '9' + 1))); // Random alphanumeric oversized value


            // Act
            Mapper.SetOversized(entity, property, value);
            var result = Mapper.GetOversized(entity, property);

            // Assert
            result.Should().Be(value);
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