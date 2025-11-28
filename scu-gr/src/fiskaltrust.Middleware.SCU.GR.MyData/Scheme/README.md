# Updating the Schema Files



```powershell
xsd.exe expensesClassification-v1.0.12.xsd ^
       incomeClassification-v1.0.12.xsd ^
       InvoicesDoc-v1.0.12.xsd ^
       InvoicesDoc-v1.0.12_detailed.xsd ^
       paymentMethods-v1.0.12.xsd ^
       SimpleTypes-v1.0.12.xsd ^
       response-v1.0.12.xsd /c /nologo /o:C:\xml
```

```powershell
xsd.exe myDATA-v1.0.12.xsd /c /nologo /o:C:\xml
```