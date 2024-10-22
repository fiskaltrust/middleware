# Open VeriFactu Questions

## `Cabecera.ObligadoEmision.NombreRazon`

> Name and company name of the person responsible for issuing the invoices.

Does this need to be in a certain format? Like e.g. "Firstname Lastname, Company Name".
Is the company name enough?


## `RegistroFacturacionAlta.RefExterna`

This field is not present in the XSD file. Do we actually need it?

## `RegistroFacturacionAlta.NombreRazonEmisor`

Is this the same as `Cabecera.ObligadoEmision.NombreRazon`?

## `RegistroFacturacionAlta.CuotaRectificada`

What's the difference between this and the `RegistroFacturacionAlta.BaseRectificada` field?

## `RegistroFacturacionAlta.CuotaTotal`

Is the sum of `ReceiptRequest.cbChargeItems.VATAmount` correct for this?
How does this differ from the `RegistroFacturacionAlta.ImporteRectificacion.BaseRectificada` field?

## `SistemaInformatico`

Should contain the data of fiskaltrust or of the PosDealer/PosCreator?

## `SistemaInformatico.NombreRazon`

> Name and company name of the producing person or entity.

Again the question of formatting and if just the company name is enough?

## `SistemaInformatico.IdSistemaInformatico`

Does this PosSystem type need to be registered with the government somehow?

