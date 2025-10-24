using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Copy;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Financial;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Invoice;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.ProForma;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Social;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.GraphQL;

public class GraphQLRequestFactory
{
    public static GraphQLQueryRequest CreateQueryDeviceRequest()
    {
        return new GraphQLQueryRequest
        {
            Query = @"{
  device {
    id
  }
}",
        };
    }

    public static GraphQLMutationRequest<OrderInput> CreateSignOrderRequest(OrderInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<OrderInput>
        {
            Query = @"mutation SignOrder($data: OrderInput!, $isTraining: Boolean!) {
  signResult: signOrder(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignOrder",
            Variables = new GraphQLVariables<OrderInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<CostCenterChangeInput> CreateSignCostCenterChangeRequest(CostCenterChangeInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<CostCenterChangeInput>
        {
            Query = @"mutation SignCostCenterChange(
  $data: CostCenterChangeInput!
  $isTraining: Boolean!
) {
signResult: signCostCenterChange(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignCostCenterChange",
            Variables = new GraphQLVariables<CostCenterChangeInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<PreBillInput> CreateSignPreBillRequest(PreBillInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<PreBillInput>
        {
            Query = @"mutation SignPreBill($data: PreBillInput!, $isTraining: Boolean!) {
 signResult: signPreBill(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignPreBill",
            Variables = new GraphQLVariables<PreBillInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<SaleInput> CreateSignSaleRequest(SaleInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<SaleInput>
        {
            Query = @"mutation SignSale($data: SaleInput!, $isTraining: Boolean!) {
  signResult: signSale(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    shortSignature
    verificationUrl
    vatCalc {
      label
      rate
      taxableAmount
      vatAmount
      totalAmount
      outOfScope
    }
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignSale",
            Variables = new GraphQLVariables<SaleInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<PaymentCorrectionInput> CreateSignPaymentCorrectionRequest(PaymentCorrectionInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<PaymentCorrectionInput>
        {
            Query = @"mutation SignPaymentCorrection(
  $data: PaymentCorrectionInput!
  $isTraining: Boolean!
) {
  signResult: signPaymentCorrection(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignPaymentCorrection",
            Variables = new GraphQLVariables<PaymentCorrectionInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<MoneyInOutInput> CreateSignMoneyInOutRequest(MoneyInOutInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<MoneyInOutInput>
        {
            Query = @"mutation SignMoneyInOut($data: MoneyInOutInput!, $isTraining: Boolean!) {
  signResult: signMoneyInOut(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignMoneyInOut",
            Variables = new GraphQLVariables<MoneyInOutInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<DrawerOpenInput> CreateSignDrawerOpenRequest(DrawerOpenInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<DrawerOpenInput>
        {
            Query = @"mutation SignDrawerOpen($data: DrawerOpenInput!, $isTraining: Boolean!) {
  signResult: signDrawerOpen(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignDrawerOpen",
            Variables = new GraphQLVariables<DrawerOpenInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<InvoiceInput> CreateSignInvoiceRequest(InvoiceInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<InvoiceInput>
        {
            Query = @"mutation SignInvoice($data: InvoiceInput!, $isTraining: Boolean!) {
  signResult: signInvoice(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignInvoice",
            Variables = new GraphQLVariables<InvoiceInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<WorkInOutInput> CreateSignWorkInRequest(WorkInOutInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<WorkInOutInput>
        {
            Query = @"mutation SignWorkIn($data: WorkInOutInput!, $isTraining: Boolean!) {
  signResult: signWorkIn(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignWorkIn",
            Variables = new GraphQLVariables<WorkInOutInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<WorkInOutInput> CreateSignWorkOutRequest(WorkInOutInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<WorkInOutInput>
        {
            Query = @"mutation SignWorkOut($data: WorkInOutInput!, $isTraining: Boolean!) {
  signResult: signWorkOut(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignWorkOut",
            Variables = new GraphQLVariables<WorkInOutInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<ReportTurnoverXInput> CreateSignReportTurnoverXRequest(ReportTurnoverXInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<ReportTurnoverXInput>
        {
            Query = @"mutation SignReportTurnoverX(
  $data: ReportTurnoverXInput!
  $isTraining: Boolean!
) {
  signResult: signReportTurnoverX(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignReportTurnoverX",
            Variables = new GraphQLVariables<ReportTurnoverXInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<ReportTurnoverZInput> CreateSignReportTurnoverZRequest(ReportTurnoverZInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<ReportTurnoverZInput>
        {
            Query = @"mutation SignReportTurnoverZ(
  $data: ReportTurnoverZInput!
  $isTraining: Boolean!
) {
  signResult: signReportTurnoverZ(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignReportTurnoverZ",
            Variables = new GraphQLVariables<ReportTurnoverZInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<ReportUserXInput> CreateSignReportUserXRequest(ReportUserXInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<ReportUserXInput>
        {
            Query = @"mutation SignReportUserX($data: ReportUserXInput!, $isTraining: Boolean!) {
  signResult: signReportUserX(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignReportUserX",
            Variables = new GraphQLVariables<ReportUserXInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<ReportUserZInput> CreateSignReportUserZRequest(ReportUserZInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<ReportUserZInput>
        {
            Query = @"mutation SignReportUserZ($data: ReportUserZInput!, $isTraining: Boolean!) {
  signResult: signReportUserZ(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignReportUserZ",
            Variables = new GraphQLVariables<ReportUserZInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }

    public static GraphQLMutationRequest<CopyInput> CreateSignCopyRequest(CopyInput data, bool isTraining)
    {
        return new GraphQLMutationRequest<CopyInput>
        {
            Query = @"mutation SignCopy($data: CopyInput!, $isTraining: Boolean!) {
  signResult: signCopy(data: $data, isTraining: $isTraining) {
    posId
    posFiscalTicketNo
    posDateTime
    terminalId
    deviceId
    eventOperation
    fdmRef {
      fdmId
      fdmDateTime
      eventLabel
      eventCounter
      totalCounter
    }
    fdmSwVersion
    digitalSignature
    bufferCapacityUsed
    warnings {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    informations {
      message
      locations {
        line
        column
      }
      extensions {
        category
        code
        data {
          name
          value
        }
        showPos
      }
    }
    footer
  }
}",
            OperationName = "SignCopy",
            Variables = new GraphQLVariables<CopyInput>
            {
                Data = data,
                IsTraining = isTraining
            }
        };
    }
}
