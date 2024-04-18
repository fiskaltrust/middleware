using System.Collections.Generic;
using Xunit;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;
using System;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueFR.UnitTest.Helper
{
    public class RequestValidationTests
    {
        [Fact]
        public void ValidateQueueState_QueueNotActivated_ReturnsValidationError()
        {
            // Arrange
            var request = new ReceiptRequest();
            var queue = new ftQueue();
            var queueFR = new ftQueueFR
            {
                ftQueueFRId = Guid.NewGuid()
            };

            // Act
            var result = RequestValidation.ValidateQueueState(request, queue, queueFR);
            Assert.NotNull(result);
            Assert.Single(result);

            // Assert
            Assert.Contains(result, r => r.Message.Contains($"Queue {queueFR.ftQueueFRId} is out of order, it has not been activated!"));
        }

        [Fact]
        public void ValidateQueueState_QueueDeactivated_ReturnsValidationError()
        {
            // Arrange
            var request = new ReceiptRequest();
            var queue = new ftQueue { StartMoment = new DateTime(), StopMoment = new DateTime() };
            var queueFR = new ftQueueFR
            {
                ftQueueFRId = Guid.NewGuid()
            };

            // Act
            var result = RequestValidation.ValidateQueueState(request, queue, queueFR);

            // Assert
            Assert.Single(result);
            Assert.Contains(result, r => r.Message.Contains($"Queue {queueFR.ftQueueFRId} is out of order, it is permanent de-activated!"));
        }

        [Fact]
        public void ValidateReceiptItems_ChargeItemsDoNotMatchCountryId_ReturnsValidationError()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new ChargeItem[]
                {
                    new ChargeItem { ftChargeItemCase = 0x1234000000000000, Amount = 10m },
                    new ChargeItem { ftChargeItemCase = 0x4652000000000010, Amount = 20m }
                }
            };

            // Act
            var result = RequestValidation.ValidateReceiptItems(request);

            // Assert
            Assert.Contains(result, r => r.Message.Contains("The charge item cases [0x1234000000000000] do not match the expected country id 0x4652XXXXXXXXXXXX"));
        }

        [Fact]
        public void ValidateReceiptItems_PayItemsDoNotMatchCountryId_ReturnsValidationError()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbPayItems = new PayItem[]
                {
                    new PayItem { ftPayItemCase = 0x1234000000000000, Amount = 10m },
                    new PayItem { ftPayItemCase = 0x4652000000000010, Amount = 20m }
                }
            };

            // Act
            var result = RequestValidation.ValidateReceiptItems(request);

            // Assert
            Assert.Contains(result, r => r.Message.Contains("The pay item case [0x1234000000000000] does not match the expected country id 0x4652XXXXXXXXXXXX"));
        }

        [Fact]
        public void ValidateReceiptItems_SumOfChargeItemsNotEqualToSumOfPayItems_ReturnsValidationError()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new ChargeItem[]
                {
                    new ChargeItem { ftChargeItemCase = 0x4652000000000010, Amount = 20m },
                    new ChargeItem { ftChargeItemCase = 0x4652000000000010, Amount = 20m }
                },
                cbPayItems = new PayItem[]
                {
                    new PayItem { ftPayItemCase = 0x4652000000000010, Amount = 30m }
                }
            };

            // Act
            var result = RequestValidation.ValidateReceiptItems(request);

            // Assert
            Assert.Contains(result, r => r.Message.Contains("The sum of the amounts of the charge items (40) is not equal to the sum of the amounts of the pay items (30)"));
        }
    }
}
