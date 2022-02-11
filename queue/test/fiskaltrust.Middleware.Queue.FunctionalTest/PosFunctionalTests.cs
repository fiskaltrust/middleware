using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Queue.FunctionalTest
{
    [Collection("Middleware Collection")]
    public class PosFunctionalTests
    {
        protected Guid CashBoxId { get; }
        protected IPOS WcfProxy { get; }
        protected IPOS GrpcProxy { get; }

        public PosFunctionalTests(MiddlewareFixture fixture)
        {
            CashBoxId = fixture.CashBoxId;
            WcfProxy = fixture.WcfProxy;
            GrpcProxy = fixture.GrpcProxy;
        }

        [Fact]
        public void IPOS_Echo_v0_Wcf_ShouldSucceed() => EchoV0(GrpcProxy);

        [Fact]
        public async Task IPOS_EchoAsync_v1_Wcf_ShouldSucceed() => await EchoV1Async(WcfProxy);

        [Fact]
        public void IPOS_Sign_v0_Wcf_ShouldSucceed() => SignV0(WcfProxy);

        [Fact]
        public async Task IPOS_Sign_v1_Wcf_ShouldSucceed() => await SignV1Async(WcfProxy);

        [Fact]
        public async Task IPOS_EchoAsync_v1_Grpc_ShouldSucceed() => await EchoV1Async(GrpcProxy);

        [Fact]
        public async Task IPOS_Sign_v1_Grpc_ShouldSucceed() => await SignV1Async(GrpcProxy);

        private void EchoV0(IPOS proxy)
        {
            var message = Guid.NewGuid().ToString();
            var response = proxy.Echo(message);
            response.Should().NotBeNull();
            response.Should().Be(message);
        }

        private async Task EchoV1Async(IPOS proxy)
        {
            var echoRequest = new EchoRequest
            {
                Message = Guid.NewGuid().ToString()
            };
            var response = await proxy.EchoAsync(echoRequest);
            response.Should().NotBeNull();
            response.Message.Should().Be(echoRequest.Message);
        }

        private void SignV0(IPOS proxy)
        {
            var receiptRequest = new ifPOS.v0.ReceiptRequest
            {
                ftCashBoxID = CashBoxId.ToString(),
                ftReceiptCase = 0x4445_0001_0000_0000,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems = new ifPOS.v0.ChargeItem[] {
                    new ifPOS.v0.ChargeItem {
                        ftChargeItemCase = 0x4445_0001_0000_0000,
                        VATRate = 19.0m,
                        Amount = 19.0m

                    },
                    new ifPOS.v0.ChargeItem
                    {
                        ftChargeItemCase = 0x4445_0000_0000_0000,
                        VATRate = 7.0m,
                        Amount = 7.0m

                    },
                    new ifPOS.v0.ChargeItem
                    {
                        ftChargeItemCase = 0x4445_0000_0000_0000,
                        VATRate = 10.7m,
                        Amount = 10.7m

                    },
                    new ifPOS.v0.ChargeItem
                    {
                        ftChargeItemCase = 0x4445_0000_0000_0000,
                        VATRate = 5.5m,
                        Amount = 5.5m
                    },
                    new ifPOS.v0.ChargeItem
                    {
                        ftChargeItemCase = 0x4445_0000_0000_0000,
                        VATRate = 0.0m,
                        Amount = 1.0m
                    },
                    new ifPOS.v0.ChargeItem
                    {
                        ftChargeItemCase = 0x4445_0000_0000_0000,
                        VATRate = 1.2m,
                        Amount = 1.0m
                    },
                    new ifPOS.v0.ChargeItem
                    {
                        ftChargeItemCase = 0x4445_0000_0000_0000,
                        VATRate = 20.0m,
                        Amount = 1.0m
                    }
                },
                cbPayItems = Array.Empty<ifPOS.v0.PayItem>()
            };
            var message = Guid.NewGuid().ToString();
            var response = proxy.Sign(receiptRequest);
            response.Should().NotBeNull();
            response.cbReceiptReference.Should().Contain(receiptRequest.cbReceiptReference);
        }

        private async Task SignV1Async(IPOS proxy)
        {
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = CashBoxId.ToString(),
                ftReceiptCase = 0x4445_0001_0000_0000,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems = new ChargeItem[] {
                    new ChargeItem {
                        ftChargeItemCase = 0x4445_0001_0000_0000,
                        VATRate = 19.0m,
                        Amount = 19.0m

                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x4445_0000_0000_0000,
                        VATRate = 7.0m,
                        Amount = 7.0m

                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x4445_0000_0000_0000,
                        VATRate = 10.7m,
                        Amount = 10.7m

                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x4445_0000_0000_0000,
                        VATRate = 5.5m,
                        Amount = 5.5m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x4445_0000_0000_0000,
                        VATRate = 0.0m,
                        Amount = 1.0m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x4445_0000_0000_0000,
                        VATRate = 1.2m,
                        Amount = 1.0m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x4445_0000_0000_0000,
                        VATRate = 20.0m,
                        Amount = 1.0m
                    }
                },
                cbPayItems = Array.Empty<PayItem>()
            };
            var message = Guid.NewGuid().ToString();
            var response = await proxy.SignAsync(receiptRequest);
            response.Should().NotBeNull();
            response.cbReceiptReference.Should().Contain(receiptRequest.cbReceiptReference);
        }
    }
}
