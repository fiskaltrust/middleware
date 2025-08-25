namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;

public static class PTCertificationTestCasesPhase1
{
    public static List<BusinessCase> Cases =
    [
        new BusinessCase("5_1", "A simplified invoice (Article 40 of the CIVA) for a customer who has provided their VAT number", true, PTCertificationExamples.Case_5_1()),
        new BusinessCase("5_2", "A annulled invoice (Article 36 of the CIVA) and its PDF after annulment, which visibly states that the document has been annulled, not forgetting the entry in the application database and in the relevant SAF-T(PT) field.", false, PTCertificationExamples.Case_5_2()),
        new BusinessCase("5_3", "A document that can be handed over to the customer to verify the transfer goods or the provision services", true, PTCertificationExamples.Case_5_3()),
        new BusinessCase("5_4", "An invoice based on the document issued in point 5.3. {must generate the OrderReferences element)", true, PTCertificationExamples.Case_5_4(), "5_3"),
        new BusinessCase("5_5", "A credit note based on the invoice from point 5.4 (must generate the References element) If you have not complied with the previous point, you must create a credit note on another document", true, PTCertificationExamples.Case_5_5(), "5_4"),
        new BusinessCase("5_6", "An invoice with 4 product lines, where the 1st line must contain a product at the reduced VAT rate, the 2nd line must contain a product exempt from VAT (the TaxE xemptionR eason element must be generated), the 3rd line must contain a product at the intermediate rate and the 4th line contain the product at the standard rate", true, PTCertificationExamples.Case_5_6()),
        new BusinessCase("5_7", "A document with 2 product lines with the following characteristics: the 1 line must refer to a transfer of goods or provision of services with quantity 100, unit price 0.SP and contain a line discount of 8.8%. The document must also be given an overall discount (generate the SettlementAmount element)", true, PTCertificationExamples.Case_5_7()),
        new BusinessCase("5_8", "A document in foreign currency", false, PTCertificationExamples.Case_5_8()),
        new BusinessCase("5_9", "A document, for an identified customer but who has not indicated the TIN, in which the total field (GrossTotal) is less than €1.00 and the SystemEntryDate value is recorded until 10 am", true, PTCertificationExamples.Case_5_9()),
        new BusinessCase("5_10", "A document for another identified client who has also not indicated their VAT number", true, PTCertificationExamples.Case_5_10()),
        new BusinessCase("5_11", "Two delivery or transport notes, one of which is valued and the other is not", false, PTCertificationExamples.Case_5_11()),
        new BusinessCase("5_12", "a budget or pro forma invoice", true, PTCertificationExamples.Case_5_12()),
        new BusinessCase("5_13", "", false, PTCertificationExamples.Case_5_13()),
        new BusinessCase("5_13_1", "", true, PTCertificationExamples.Case_5_13_1_Invoice()),
        new BusinessCase("5_13_2", "", true, PTCertificationExamples.Case_5_13_2_Payment(), "5_6"),
    ];
}
