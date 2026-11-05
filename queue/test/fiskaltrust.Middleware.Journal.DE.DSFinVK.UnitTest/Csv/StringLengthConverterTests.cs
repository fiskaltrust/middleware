using System;
using System.Linq;
using System.Reflection;
using CsvHelper.Configuration;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Csv
{
    public class StringLengthConverterTests
    {
        public int PropertyWithoutAttributes { get; set; }

        [MaxLength(10)]
        public int PropertyWithLengthAttribute { get; set; }

        [Theory]
        [InlineData("I am a string with more than 10 characters", "I am a string with more than 10 characters")]
        [InlineData("Short", "Short")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void ConvertToString_Returns_ExpectedValue(object inputValue, string expectedResult)
        {
            var property = typeof(StringLengthConverterTests).GetProperties().Single(x => x.Name == nameof(PropertyWithoutAttributes));

            var sut = new StringLengthConverter();
            var result = sut.ConvertToString(inputValue, null, new MemberMapData(property));
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("I am a string with more than 10 characters", "I am a str")]
        [InlineData("Short", "Short")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void ConvertToString_Returns_ExpectedValue_With_DefinedNumberOfDecimalDigits(object inputValue, string expectedResult)
        {
            var property = typeof(StringLengthConverterTests).GetProperties().Single(x => x.Name == nameof(PropertyWithLengthAttribute));

            var sut = new StringLengthConverter();
            var result = sut.ConvertToString(inputValue, null, new MemberMapData(property));
            result.Should().Be(expectedResult);
        }
    }
}
