using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Csv
{
    public class BoolTypeConverterTests
    {
        [Theory]
        [InlineData(true, "1")]
        [InlineData(false, "0")]
        public void ConvertToString_Returns_Expected_Value(object inputValue, string expectedResult)
        {
            var sut = new BoolTypeConverter();
            var result = sut.ConvertToString(inputValue, null, null);
            result.Should().Be(expectedResult);
        }
    }
}
