# Updating the Schema Files

The bundled AADE myDATA schema is **v2.0.1**. The C# model `response-v2.0.1.cs` is
generated from the XSDs below with `xsd.exe`.

## Regenerating `response-v2.0.1.cs`

Use the .NET Framework **4.8** `xsd.exe` (file version `4.8.3928.0`, shipped under
`…\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\`). The tool version is stamped
into the generated file, so using a different build produces spurious diffs.

```powershell
xsd.exe expensesClassification-v2.0.1.xsd ^
       incomeClassification-v2.0.1.xsd ^
       InvoicesDoc-v2.0.1.xsd ^
       InvoicesDoc-v2.0.1_aade_detailed.xsd ^
       paymentMethods-v2.0.1.xsd ^
       SimpleTypes-v2.0.1.xsd ^
       response-v2.0.1.xsd /c /nologo /o:C:\xml
```

Notes:
- `InvoicesDoc-v2.0.1.xsd` `xs:include`s `TransportTypes-v2.0.1.xsd` and
  `SimpleTypes-v2.0.1.xsd`, so `xsd.exe` pulls those in automatically (the transport
  complex types therefore land in the same generated file / XML namespace).
- Passing both `InvoicesDoc` and its `_aade_detailed` twin makes `xsd.exe` emit
  `"… has already been declared"` validation warnings — these are expected and harmless
  (both declare the same `http://www.aade.gr/myDATA/invoice/v1.0` types; the tool
  de-duplicates and still generates correct classes).
- `xsd.exe` names the output after the concatenated input file names. Rename it to
  `response-v2.0.1.cs`.
- **Manual post-generation edit (required):** `xsd.exe` flattens the `packingsDeclarations`
  element into a jagged `PackagingDetailType[][]`, which `XmlSerializer` cannot construct — it
  throws `CodeGenError … Cannot convert PackagingDetailType[] to PackagingDetailType` and
  breaks serialization of the whole `InvoicesDoc`. After regenerating, replace that member's
  `[XmlArrayItem(...)]` with `[System.Xml.Serialization.XmlIgnoreAttribute()]` (see the comment
  on `packingsDeclarations` in the current file). It is part of the out-of-scope e-transport
  surface and is not populated by this SCU.
- The e-transport endpoint schemas (`RegisterTransfer`, `RejectDeliveryNote`,
  `ConfirmDeliveryOutcome`, `GetDeliveryStatusResponse`, `GenerateGroupQRCode` +
  `Response`, `RequestGroupQRDetailsResponse`) are kept for reference only and are **not**
  compiled into the model.
