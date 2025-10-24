using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Copy;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Financial;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.ProForma;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Social;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.GraphQL;

public class GraphQLRequestFactory
{
    public static GraphQLRequest<OrderInput> CreateSignOrderRequest(OrderInput data, bool isTraining)
    {
        return new GraphQLRequest<OrderInput>
        {
            Query = @"mutation SignOrder($data: OrderInput!, $isTraining: Boolean!) {
  signOrder(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<CostCenterChangeInput> CreateSignCostCenterChangeRequest(CostCenterChangeInput data, bool isTraining)
    {
        return new GraphQLRequest<CostCenterChangeInput>
        {
            Query = @"mutation SignCostCenterChange(
  $data: CostCenterChangeInput!
  $isTraining: Boolean!
) {
  signCostCenterChange(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<PreBillInput> CreateSignPreliminaryBillRequest(PreBillInput data, bool isTraining)
    {
        return new GraphQLRequest<PreBillInput>
        {
            Query = @"mutation SignPreBill($data: PreBillInput!, $isTraining: Boolean!) {
  signPreBill(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<SaleInput> CreateSignSaleInputRequest(SaleInput data, bool isTraining)
    {
        return new GraphQLRequest<SaleInput>
        {
            Query = @"mutation SignSale($data: SaleInput!, $isTraining: Boolean!) {
  signSale(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<PaymentCorrectionInput> CreateSignPaymentCorrectionRequest(PaymentCorrectionInput data, bool isTraining)
    {
        return new GraphQLRequest<PaymentCorrectionInput>
        {
            Query = @"mutation SignPaymentCorrection(
  $data: PaymentCorrectionInput!
  $isTraining: Boolean!
) {
  signPaymentCorrection(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<MoneyInOutInput> CreateSignMoneyInOutRequest(MoneyInOutInput data, bool isTraining)
    {
        return new GraphQLRequest<MoneyInOutInput>
        {
            Query = @"mutation SignMoneyInOut($data: MoneyInOutInput!, $isTraining: Boolean!) {
  signMoneyInOut(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<DrawerOpenInput> CreateSignDrawerOpenRequest(DrawerOpenInput data, bool isTraining)
    {
        return new GraphQLRequest<DrawerOpenInput>
        {
            Query = @"mutation SignDrawerOpen($data: DrawerOpenInput!, $isTraining: Boolean!) {
  signDrawerOpen(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<InvoiceInput> CreateSignInvoiceRequest(InvoiceInput data, bool isTraining)
    {
        return new GraphQLRequest<InvoiceInput>
        {
            Query = @"mutation SignInvoice($data: InvoiceInput!, $isTraining: Boolean!) {
  signInvoice(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<WorkInOutInput> CreateSignWorkInRequest(WorkInOutInput data, bool isTraining)
    {
        return new GraphQLRequest<WorkInOutInput>
        {
            Query = @"mutation SignWorkIn($data: WorkInOutInput!, $isTraining: Boolean!) {
  signWorkIn(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<WorkInOutInput> CreateSignWorkOutRequest(WorkInOutInput data, bool isTraining)
    {
        return new GraphQLRequest<WorkInOutInput>
        {
            Query = @"mutation SignWorkOut($data: WorkInOutInput!, $isTraining: Boolean!) {
  signWorkOut(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<ReportTurnoverXInput> CreateSignReportTurnoverXRequest(ReportTurnoverXInput data, bool isTraining)
    {
        return new GraphQLRequest<ReportTurnoverXInput>
        {
            Query = @"mutation SignReportTurnoverX(
  $data: ReportTurnoverXInput!
  $isTraining: Boolean!
) {
  signReportTurnoverX(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<ReportTurnoverZInput> CreateSignReportTurnoverZRequest(ReportTurnoverZInput data, bool isTraining)
    {
        return new GraphQLRequest<ReportTurnoverZInput>
        {
            Query = @"mutation SignReportTurnoverZ(
  $data: ReportTurnoverZInput!
  $isTraining: Boolean!
) {
  signReportTurnoverZ(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<ReportUserXInput> CreateSignReportUserXRequest(ReportUserXInput data, bool isTraining)
    {
        return new GraphQLRequest<ReportUserXInput>
        {
            Query = @"mutation SignReportUserX($data: ReportUserXInput!, $isTraining: Boolean!) {
  signReportUserX(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<ReportUserZInput> CreateSignReportUserZRequest(ReportUserZInput data, bool isTraining)
    {
        return new GraphQLRequest<ReportUserZInput>
        {
            Query = @"mutation SignReportUserZ($data: ReportUserZInput!, $isTraining: Boolean!) {
  signReportUserZ(data: $data, isTraining: $isTraining) {
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

    public static GraphQLRequest<CopyInput> CreateSignCopyRequest(CopyInput data, bool isTraining)
    {
        return new GraphQLRequest<CopyInput>
        {
            Query = @"mutation SignCopy($data: CopyInput!, $isTraining: Boolean!) {
  signCopy(data: $data, isTraining: $isTraining) {
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
