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
    public class DecimalTypeConverterTests
    {
        public int PropertyWithoutAttributes { get; set; }
        
        [Precision(3)]
        public int PropertyWithPrecisionAttribute { get; set; }

        [Theory]
        [InlineData(0, "0,00")]
        [InlineData(12.01, "12,01")]
        [InlineData(-12.01, "-12,01")]
        [InlineData(12.01123, "12,01")]
        public void ConvertToString_Returns_ExpectedValue(object inputValue, string expectedResult)
        {
            var property = typeof(DecimalTypeConverterTests).GetProperties().Single(x => x.Name == nameof(PropertyWithoutAttributes));

            var sut = new DecimalTypeConverter();
            var result = sut.ConvertToString(Convert.ToDecimal(inputValue), null, new MemberMapData(property));
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0, "0,000")]
        [InlineData(12.01, "12,010")]
        [InlineData(-12.01, "-12,010")]
        [InlineData(12.01123, "12,011")]
        public void ConvertToString_Returns_ExpectedValue_With_DefinedNumberOfDecimalDigits(object inputValue, string expectedResult)
        {
            var property = typeof(DecimalTypeConverterTests).GetProperties().Single(x => x.Name == nameof(PropertyWithPrecisionAttribute));

            var sut = new DecimalTypeConverter();
            var result = sut.ConvertToString(Convert.ToDecimal(inputValue), null, new MemberMapData(property));
            result.Should().Be(expectedResult);
        }
    }
}
