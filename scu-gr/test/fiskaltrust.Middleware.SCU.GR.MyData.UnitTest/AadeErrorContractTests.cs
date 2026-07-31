using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class AadeErrorContractTests
{
    [Fact]
    public void AadeErrorResponseSerialization_IsPinnedAgainstQueueGrDuplicateAaDetection()
    {
        // Mirrors the pin in QueueGR's InvoiceCounterReservationTests: the queue parses
        // the FAILURE signature Data as {"AADEError":...,"Errors":[{"message":...,
        // "code":...}]} and matches code == "233" to advance the invoice counter past
        // an aa that AADE reported as already filed ("number consumed, advance"). If
        // this serialized shape drifts — property renames, a different serializer, code
        // becoming numeric — that advance silently stops triggering and duplicate
        // rejections go back to permanently failing the receipt without moving the
        // counter.
        var json = JsonSerializer.Serialize(new MyDataSCU.AADEEErrorResponse
        {
            AADEError = "ValidationError",
            Errors = new List<ErrorType> { new ErrorType { message = "duplicate invoice", code = "233" } },
        });

        json.Should().Be("{\"AADEError\":\"ValidationError\",\"Errors\":[{\"message\":\"duplicate invoice\",\"code\":\"233\"}]}");
    }
}
