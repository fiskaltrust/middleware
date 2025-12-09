# Updating the Schema Files

Latest versions can be found at https://web.araba.eus/eu/ogasuna/ticketbai/dokumentazio-teknikoa

Since the files do not match with what XSD expects we need to add the xsd script first

```powershell
& 'c:\GitHub\middleware\scu-es\scripts\Add-XsdPrefix.ps1' -InputPath 'c:\GitHub\middleware\scu-es\src\fiskaltrust.Middleware.SCU.ES.TicketBAI.Common\xsd\Anula_ticketBaiV1-2-2.xsd'

& 'c:\GitHub\middleware\scu-es\scripts\Add-XsdPrefix.ps1' -InputPath 'c:\GitHub\middleware\scu-es\src\fiskaltrust.Middleware.SCU.ES.TicketBAI.Common\xsd\ticketBaiV1-2-2.xsd'

& 'c:\GitHub\middleware\scu-es\scripts\Add-XsdPrefix.ps1' -InputPath 'c:\GitHub\middleware\scu-es\src\fiskaltrust.Middleware.SCU.ES.TicketBAI.Common\xsd\ticketBaiResponse.xsd'
```

```powershell
xsd.exe Anula_ticketBaiV1-2-2.xsd /u:https://www.w3.org/TR/2002/REC-xmldsig-core-20020212/xmldsig-core-schema.xsd /c /nologo /o:C:\xml


xsd.exe ticketBaiResponse.xsd /u:https://www.w3.org/TR/2002/REC-xmldsig-core-20020212/xmldsig-core-schema.xsd /c /nologo /o:C:\xml

xsd.exe ticketBaiV1-2-2.xsd /u:https://www.w3.org/TR/2002/REC-xmldsig-core-20020212/xmldsig-core-schema.xsd /c /nologo /o:C:\xml
```

```powershell
xsd.exe myDATA-v1.0.12.xsd /c /nologo /o:C:\xml
```