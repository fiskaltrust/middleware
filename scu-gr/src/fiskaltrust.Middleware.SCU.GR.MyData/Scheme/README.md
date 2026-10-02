# Updating the Schema Files

The bundled AADE myDATA schema is **v2.0.2**. The C# model `response-v2.0.2.cs` is
generated from the XSDs below with `xsd.exe`.

## Regenerating `response-v2.0.2.cs`

Use the .NET Framework **4.8** `xsd.exe` (file version `4.8.3928.0`, shipped under
`…\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\`). The tool version is stamped
into the generated file, so using a different build produces spurious diffs.

```powershell
xsd.exe expensesClassification-v2.0.2.xsd ^
       incomeClassification-v2.0.2.xsd ^
       InvoicesDoc-v2.0.2.xsd ^
       InvoicesDoc-v2.0.2_aade_detailed.xsd ^
       paymentMethods-v2.0.2.xsd ^
       SimpleTypes-v2.0.2.xsd ^
       response-v2.0.2.xsd /c /nologo /o:C:\xml
```

Notes:
- `InvoicesDoc-v2.0.2.xsd` `xs:include`s `TransportTypes-v2.0.2.xsd` and
  `SimpleTypes-v2.0.2.xsd`, so `xsd.exe` pulls those in automatically (the transport
  complex types therefore land in the same generated file / XML namespace).
- Passing both `InvoicesDoc` and its `_aade_detailed` twin makes `xsd.exe` emit
  `"… has already been declared"` validation warnings — these are expected and harmless
  (both declare the same `http://www.aade.gr/myDATA/invoice/v1.0` types; the tool
  de-duplicates and still generates correct classes).
- `xsd.exe` names the output after the concatenated input file names. Rename it to
  `response-v2.0.2.cs`.
- **Manual post-generation edit (required):** `xsd.exe` flattens the `packingsDeclarations`
  element into a jagged `PackagingDetailType[][]`, which `XmlSerializer` cannot construct — it
  throws `CodeGenError … Cannot convert PackagingDetailType[] to PackagingDetailType` and
  breaks serialization of the whole `InvoicesDoc`. After regenerating, replace that member's
  `[XmlArrayItem(...)]` with `[System.Xml.Serialization.XmlIgnoreAttribute()]` (see the comment
  on `packingsDeclarations` in the current file). This is an `xsd.exe` defect, not a schema
  problem: it collapses the `PackingsDeclaration` wrapper type into an array and annotates the
  resulting two-level array with a single-level `[XmlArrayItem]`. The element is supported via
  the hand-written `AadeBookInvoiceType.PackingsDeclarations.cs`, which adds the
  `PackingsDeclaration` wrapper class and a serializable `PackingsDeclarations` property. Keep
  that file as-is when regenerating; do not edit `response-v2.0.2.cs` beyond the
  `[XmlIgnore]` above. `XmlSerializer` writes members in compile order, so the csproj compiles
  that file explicitly last; without that the element can be emitted before `uid` and the
  document fails XSD validation.
- The e-transport endpoint schemas (`RegisterTransfer`, `RejectDeliveryNote`,
  `ConfirmDeliveryOutcome`, `ConfirmDeliveryReturn`, `GetDeliveryStatusResponse`,
  `GenerateGroupQRCode` + `Response`, `RequestGroupQRDetailsResponse`) are kept for
  reference only and are **not** compiled into the model.
