using System;
using System.IO;
using System.ServiceModel;
#if WCF
using System.ServiceModel.Web;
#endif

namespace fiskaltrust.ifPOS.v0
{
    /// <summary>
    /// Interface to fiskaltrust.Middlewares
    /// </summary>
    [ServiceContract]
    public interface IPOS
    {
        /// <summary>
        /// Decide the receipt-case and sign the receipt
        /// </summary>
        /// <param name="data">requestdata representing a receipt</param>
        /// <returns>responsedata to be added on receipt</returns>
        [OperationContract]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v0/sign")]
#endif
        [Obsolete("This method is obsolete, use v1.SignAsync instead.")]
        ReceiptResponse Sign(ReceiptRequest data);

        /// <summary>
        /// Decide the receipt-case and sign the receipt async begin pattern
        /// </summary>
        /// <param name="data"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [OperationContract(AsyncPattern = true)]
        [Obsolete("This method is obsolete, use v1.SignAsync instead.")]
        IAsyncResult BeginSign(ReceiptRequest data, AsyncCallback callback, object state);

        /// <summary>
        /// Decide the receipt-case and sign the receipt  async end pattern
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [Obsolete("This method is obsolete, use v1.SignAsync instead.")]
        ReceiptResponse EndSign(IAsyncResult result);

        /// <summary>
        /// Stream down a journal
        /// </summary>
        /// <param name="ftJournalType">Type of the requested journal.</param>
        /// <param name="from">Timestamp in utc, starting to stream the journal.</param>
        /// <param name="to">Timestamp in utc, stopping to stream the journal.</param>
        /// <returns>Journalstream</returns>
        [OperationContract]
#if WCF
        [WebInvoke(UriTemplate = "v0/journal?type={ftJournalType}&from={from}&to={to}")]
#endif
        Stream Journal(long ftJournalType, long from, long to);

        /// <summary>
        /// Stream down a journal async begin pattern
        /// </summary>
        /// <param name="ftJournalType"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [OperationContract(AsyncPattern = true)]
        [Obsolete("This method is obsolete, use v1.JournalAsync instead.")]
        IAsyncResult BeginJournal(long ftJournalType, long from, long to, AsyncCallback callback, object state);

        /// <summary>
        /// stream down a journal  async end pattern
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [Obsolete("This method is obsolete, use v1.JournalAsync instead.")]
        Stream EndJournal(IAsyncResult result);

        /// <summary>
        /// Function to test communication
        /// </summary>
        /// <param name="message">The test message</param>
        /// <returns>The test message</returns>
        [OperationContract]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v0/echo")]
#endif
        [Obsolete("This method is obsolete, use v1.EchoAsync instead.")]
        string Echo(string message);

        /// <summary> 
        /// Function to test communication async begin pattern
        /// </summary>
        /// <param name="message"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [OperationContract(AsyncPattern = true)]
        [Obsolete("This method is obsolete, use v1.EchoAsync instead.")]
        IAsyncResult BeginEcho(string message, AsyncCallback callback, object state);

        /// <summary>
        /// Function to test communication async end pattern
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [Obsolete("This method is obsolete, use v1.EchoAsync instead.")]
        string EndEcho(IAsyncResult result);
    }
}