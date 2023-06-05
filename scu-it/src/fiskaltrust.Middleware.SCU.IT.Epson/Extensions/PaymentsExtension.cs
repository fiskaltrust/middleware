using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Epson.Models;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Extensions
{
    public static class PaymentsExtension
    {

        public static List<TotalAndMessage> GetTotalAndMessages(this List<Payment> payments)
        {
            var TotalAndMessages = new List<TotalAndMessage>();
            if (payments != null)
            {
                foreach (var pay in payments)
                {
                    var printRecTotal = new PrintRecTotal
                    {
                        Description = pay.Description,
                        PaymentType =  pay.PaymentType.GetEpsonPaymentType().PaymentType,
                        Index = pay.PaymentType.GetEpsonPaymentType().Index,
                        Payment = pay.Amount
                    };
                    PrintRecMessage? printRecMessage = null;
                    if (!string.IsNullOrEmpty(pay.AdditionalInformation))
                    {
                        printRecMessage = new PrintRecMessage()
                        {
                            Message = pay.AdditionalInformation,
                            MessageType = 4
                        };
                    }
                    TotalAndMessages.Add(new()
                    {
                        PrintRecTotal = printRecTotal,
                        PrintRecMessage = printRecMessage
                    });
                }
            }
            if (TotalAndMessages.Count == 0)
            {
                TotalAndMessages.Add(new()
                {
                    PrintRecTotal = new PrintRecTotal()
                    {
                        Description = PaymentType.Cash.ToString(),
                        PaymentType = (int) PaymentType.Cash,
                        Payment = 0
                    },
                    PrintRecMessage = null
                });
            }
            return TotalAndMessages;
        }
    }
}
