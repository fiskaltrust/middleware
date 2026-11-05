using System;
using System.Reflection;
using CsvHelper.Configuration;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Csv
{
    public class DateTimeTypeConverterTests
    {
        [Fact]
        public void ConvertToString_Returns_DateTime_In_ExpectedFormat()
        {
            var datetime = new DateTime(2020, 1, 30, 12, 30, 0, 476);
            var expectedString = "2020-01-30T12:30:00Z";

            var sut = new DateTimeTypeConverter();
            var result = sut.ConvertToString(datetime, null, null);
            result.Should().Be(expectedString);
        }

        [Fact]
        public void ConvertToString_Returns_DateTime_In_ExpectedFormat_WhenMillisecondAttributIsSet()
        {
            var datetime = new DateTime(2020, 1, 30, 12, 30, 0, 476);
            var expectedString = "2020-01-30T12:30:00.476Z";
            
            var memberInfoMock = new Mock<MemberInfo>();
            memberInfoMock.Setup(x => x.GetCustomAttributes(typeof(DateTimePrecisionAttribute), false)).Returns(new object[] { new DateTimePrecisionAttribute { HasMilliseconds = true } });
            var memberMapData = new MemberMapData(memberInfoMock.Object);

            var sut = new DateTimeTypeConverter();
            var result = sut.ConvertToString(datetime, null, memberMapData);
            result.Should().Be(expectedString);
        }
    }

}
