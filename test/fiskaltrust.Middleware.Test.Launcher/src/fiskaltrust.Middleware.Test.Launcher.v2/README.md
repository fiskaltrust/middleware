## Specify Configuration

Crate a file `queue-configuration.json` and a file `scu-configuration.json` in this directory.

The queue file needs to contain at least this empty configuration.
```json
{
  "Configuration": {}
}
```

The scu file should contain the package name of the scu you want to use and the needed parameters.  
E.g.:
```json
{
  "Package": "fiskaltrust.Middleware.SCU.ES.TicketBaiAraba",
  "Configuration": {
    "CertificateBase64": "",
    "CertificatePassword": "",
    "EmisorNif": "",
    "EmisorApellidosNombreRazonSocial": ""
  }
}
```