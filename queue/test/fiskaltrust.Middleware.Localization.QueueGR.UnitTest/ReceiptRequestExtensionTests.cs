using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.SCU.GR.MyData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest
{
    // Move all model classes and enums above the test class so they are in scope for all tests
    public class DemoClass
    {
        public Guid Test2 { get; set; }
        public string? Test { get; set; }
    }

    public class ParentClass
    {
        public string? Name { get; set; }
        public ChildClass? Child { get; set; }
    }

    public class ChildClass
    {
        public string? ChildName { get; set; }
        public int Value { get; set; }
    }

    public class CollectionClass
    {
        public List<string> Items { get; set; } = new();
        public List<int> Numbers { get; set; } = new();
    }

    public class MultiLevelClass
    {
        public Level1Class? Level1 { get; set; }
    }

    public class Level1Class
    {
        public string? Name { get; set; }
        public Level2Class? Level2 { get; set; }
    }

    public class Level2Class
    {
        public string? Description { get; set; }
        public Level3Class? Level3 { get; set; }
    }

    public class Level3Class
    {
        public int Id { get; set; }
        public bool Active { get; set; }
    }

    public class NumericClass
    {
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
        public float FloatValue { get; set; }
    }

    public class DateTimeClass
    {
        public DateTime CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class NullableClass
    {
        public string RequiredValue { get; set; } = "";
        public string? OptionalValue { get; set; }
        public int? OptionalNumber { get; set; }
    }

    public class PeopleClass
    {
        public List<Person> People { get; set; } = new();
    }

    public class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public class StrictNumericClass
    {
        public int NumberProperty { get; set; }
    }

    public class EnumClass
    {
        public Status Status { get; set; }
        public Priority Priority { get; set; }
    }

    public enum Status
    {
        Inactive = 0,
        Active = 1,
        Suspended = 2
    }

    public enum Priority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public class ReceiptRequestExtensionTests
    {
        [Fact]
        public void TryDeserializeftReceiptCaseData_ShouldReturnFalse_WhenftReceiptCaseDataIsNull()
        {
            var receiptRequest = new ReceiptRequest();
            receiptRequest.ftReceiptCaseData = null;

            var success = receiptRequest.TryDeserializeftReceiptCaseData<DemoClass>(out var result);

            success.Should().BeFalse();
            result.Should().BeNull();
        }

        [Fact]
        public void TryDeserializeftReceiptCaseData_ShouldDeserializeSimpleObject_WhenValidData()
        {
            var testGuid = Guid.NewGuid();

            var receiptRequest = new ReceiptRequest();
            receiptRequest.ftReceiptCaseData = new
            {
                Test2 = testGuid.ToString(),
                Test = "TestValue"
            };

            var success = receiptRequest.TryDeserializeftReceiptCaseData<DemoClass>(out var result);

            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.Test.Should().Be("TestValue");
            result.Test2.Should().Be(testGuid);
        }

        [Fact]
        public void TryDeserializeftReceiptCaseData_ShouldDeserializeNestedObject_WhenComplexHierarchy()
        {
            var receiptRequest = new ReceiptRequest();
            receiptRequest.ftReceiptCaseData = new
            {
                Name = "Parent",
                Child = new
                {
                    ChildName = "Child1",
                    Value = 42
                }
            };

            var success = receiptRequest.TryDeserializeftReceiptCaseData<ParentClass>(out var result);

            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.Name.Should().Be("Parent");
            result.Child.Should().NotBeNull();
            result.Child!.ChildName.Should().Be("Child1");
            result.Child.Value.Should().Be(42);
        }

        [Fact]
        public void TryDeserializeftReceiptCaseData_ShouldDeserializeArrays_WhenContainingCollections()
        {
            var receiptRequest = new ReceiptRequest();
            receiptRequest.ftReceiptCaseData = new
            {
                Items = new[] { "Item1", "Item2", "Item3" },
                Numbers = new[] { 1, 2, 3, 4, 5 }
            };

            var success = receiptRequest.TryDeserializeftReceiptCaseData<CollectionClass>(out var result);

            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(3);
            result.Items.Should().Contain(new[] { "Item1", "Item2", "Item3" });
            result.Numbers.Should().HaveCount(5);
            result.Numbers.Should().Contain(new[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public void TryDeserializeftReceiptCaseData_ShouldDeserializeComplexNestedStructure_WhenMultipleLevels()
        {
            var receiptRequest = new ReceiptRequest();
            receiptRequest.ftReceiptCaseData = new
            {
                Level1 = new
                {
                    Name = "L1",
                    Level2 = new
                    {
                        Description = "L2",
                        Level3 = new
                        {
                            Id = 123,
                            Active = true
                        }
                    }
                }
            };

            var success = receiptRequest.TryDeserializeftReceiptCaseData<MultiLevelClass>(out var result);

            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.Level1.Should().NotBeNull();
            result.Level1!.Name.Should().Be("L1");
            result.Level1.Level2.Should().NotBeNull();
            result.Level1.Level2!.Description.Should().Be("L2");
            result.Level1.Level2.Level3.Should().NotBeNull();
            result.Level1.Level2.Level3!.Id.Should().Be(123);
            result.Level1.Level2.Level3.Active.Should().BeTrue();
        }

        [Fact]
        public void TryDeserializeftReceiptCaseData_ShouldHandleCaseInsensitiveProperties_WhenMixedCase()
        {
            var receiptRequest = new ReceiptRequest();
            receiptRequest.ftReceiptCaseData = new
            {
                test = "LowerCase",
                TEST2 = Guid.NewGuid().ToString()
            };

            var success = receiptRequest.TryDeserializeftReceiptCaseData<DemoClass>(out var result);

            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.Test.Should().Be("LowerCase");
            result.Test2.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void TryDeserializeftReceiptCaseData_ShouldDeserializeNumericTypes_WhenContainingVariousNumbers()
        {
            var receiptRequest = new ReceiptRequest();
            receiptRequest.ftReceiptCaseData = new
            {
                IntValue = 42,
                LongValue = 9223372036854775807L,
                DecimalValue = 123.456m,
                DoubleValue = 789.123d,
                FloatValue = 456.789f
            };

            var success = receiptRequest.TryDeserializeftReceiptCaseData<NumericClass>(out var result);

            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.IntValue.Should().Be(42);
            result.LongValue.Should().Be(9223372036854775807L);
            result.DecimalValue.Should().Be(123.456m);
            result.DoubleValue.Should().Be(789.123d);
            result.FloatValue.Should().BeApproximately(456.789f, 0.001f);
        }

        [Fact]
        public void TryDeserializeftReceiptCaseData_ShouldDeserializeDateTimeTypes_WhenContainingDates()
        {
            var testDate = DateTime.Now;
            var testDateOffset = DateTimeOffset.Now;

            var receiptRequest = new ReceiptRequest();
            receiptRequest.ftReceiptCaseData = new
            {
                CreatedAt = testDate,
                UpdatedAt = testDateOffset,
                IsActive = true
            };

            var success = receiptRequest.TryDeserializeftReceiptCaseData<DateTimeClass>(out var result);

            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.CreatedAt.Should().BeCloseTo(testDate, TimeSpan.FromSeconds(1));
            result.UpdatedAt.Should().BeCloseTo(testDateOffset, TimeSpan.FromSeconds(1));
            result.IsActive.Should().BeTrue();
        }

        [Fact]
        public void TryDeserializeftReceiptCaseData_ShouldHandleNullableProperties_WhenSomeValuesAreNull()
        {
            var receiptRequest = new ReceiptRequest();
            receiptRequest.ftReceiptCaseData = new
            {
                RequiredValue = "Required",
                OptionalValue = (string?) null,
                OptionalNumber = (int?) null
            };

            var success = receiptRequest.TryDeserializeftReceiptCaseData<NullableClass>(out var result);

            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.RequiredValue.Should().Be("Required");
            result.OptionalValue.Should().BeNull();
            result.OptionalNumber.Should().BeNull();
        }

        [Fact]
        public void TryDeserializeftReceiptCaseData_ShouldDeserializeListOfObjects_WhenContainingComplexCollections()
        {
            var receiptRequest = new ReceiptRequest();
            receiptRequest.ftReceiptCaseData = new
            {
                People = new[]
                {
                    new { Name = "John", Age = 30 },
                    new { Name = "Jane", Age = 25 },
                    new { Name = "Bob", Age = 35 }
                }
            };

            var success = receiptRequest.TryDeserializeftReceiptCaseData<PeopleClass>(out var result);

            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.People.Should().HaveCount(3);
            result.People[0].Name.Should().Be("John");
            result.People[0].Age.Should().Be(30);
            result.People[1].Name.Should().Be("Jane");
            result.People[1].Age.Should().Be(25);
        }

        [Fact]
        public void TryDeserializeftReceiptCaseData_ShouldReturnFalse_WhenTypesMismatch()
        {
            var receiptRequest = new ReceiptRequest();
            receiptRequest.ftReceiptCaseData = new
            {
                StringProperty = "This should be a number",
                NumberProperty = "This should be a number too"
            };

            var success = receiptRequest.TryDeserializeftReceiptCaseData<StrictNumericClass>(out var result);

            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.NumberProperty.Should().Be(0); // Default value for int
        }

        [Fact]
        public void TryDeserializeftReceiptCaseData_ShouldDeserializeEnumValues_WhenContainingEnums()
        {
            var receiptRequest = new ReceiptRequest();
            receiptRequest.ftReceiptCaseData = new
            {
                Status = "Active",
                Priority = 1
            };

            var success = receiptRequest.TryDeserializeftReceiptCaseData<EnumClass>(out var result);

            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.Status.Should().Be(Status.Active);
            result.Priority.Should().Be(Priority.Medium);
        }


        [Fact]
        public void Test()
        {
            var receiptRequest = new ReceiptResponse();
            receiptRequest.ftStateData = new MiddlewareState
            {
                cbPreviousReceiptReferences = new List<ReceiptReference>
                {
                         new ReceiptReference
                        {
                            ReceiptRequest = new ReceiptRequest
                            {
                                ftCashBoxID = Guid.NewGuid(),
                                ftReceiptCase = (ReceiptCase)0x4752_2000_0000_0000,
                                ftReceiptCaseData = new
                                {
                                    TestProperty = "TestValue"
                                }
                            },
                            ReceiptResponse = new ReceiptResponse
                            {
                                ftState = (State)0x4752_2000_0000_0000,
                                ftCashBoxIdentification = "cashBoxIdentification",
                                ftQueueID = Guid.NewGuid(),
                                ftQueueItemID = Guid.NewGuid(),
                                ftQueueRow = 1,
                                ftReceiptIdentification = "receiptIdentification",
                                ftReceiptMoment = DateTime.UtcNow,
                            }
                    }
                }
            };
            var json = JsonSerializer.Deserialize<MiddlewareSCUGRMyDataState>(JsonSerializer.Serialize(receiptRequest.ftStateData))!;
            json.GR = new MiddlewareQueueGRState
            {
                GovernmentApi = new GovernmentApiData
                {
                    Action = "TestAction",
                    Protocol = "TestProtocol",
                    ProtocolRequest = "TestRequest",
                    ProtocolResponse = "TestResponse",
                }
            };

            var data = JsonSerializer.Serialize(json);
        }

        public class MiddlewareState
        {
            public List<ReceiptReference>? cbPreviousReceiptReferences { get; set; }
        }

        public class ReceiptReference
        {
            public ReceiptRequest? ReceiptRequest { get; set; }
            public ReceiptResponse? ReceiptResponse { get; set; }
        }

    }
}
