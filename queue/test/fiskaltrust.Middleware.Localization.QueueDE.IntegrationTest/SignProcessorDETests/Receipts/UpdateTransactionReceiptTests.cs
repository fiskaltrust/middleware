﻿using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using Xunit;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class UpdateTransactionReceiptTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly ReceiptTests _receiptTests;
        private readonly SignProcessorDependenciesFixture _fixture;
        private readonly ReceiptProcessorHelper _receiptProcessorHelper;

        public UpdateTransactionReceiptTests(SignProcessorDependenciesFixture fixture)
        {
            _receiptTests = new ReceiptTests(fixture);
            _fixture = fixture;
            _receiptProcessorHelper = new ReceiptProcessorHelper(_fixture.SignProcessor);
        }
        
        [Fact]
        public async Task UpdateTransaction_IsNoImplicitFlow_ExpectErrorState()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "UpdateTransNoImplFlow", 0x4445000100000009);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }

        [Fact]
        public async Task UpdateTransaction_NoOpenTransactionRepo_ExpectArgumentException()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "UpTransNoOpenTrans", 0x4445000000000009);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }
        
        [Fact]
        public async Task UpdateTransaction_TrainingRequest_ExpectTrainingSign()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "UpdateTransaction_TrainingRequest", 0x4445000000020009);
            await _fixture.AddOpenOrders(receiptRequest.cbReceiptReference, 6);
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            receiptResponse.Should().NotBeNull();
            receiptResponse.ftSignatures.Should().NotBeNull();
            var i = receiptResponse.ftSignatures.Length - 1;
            receiptResponse.ftSignatures[i].ftSignatureType.Should().Be(4096);
            receiptResponse.ftSignatures[i].Caption.Should().Be("Trainingsbuchung");
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, string.Empty);
        }
    }
}