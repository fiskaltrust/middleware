# TicketBAI exempt-reasons reference samples

Vendored from [market-es#95](https://github.com/fiskaltrust/market-es/issues/95#issuecomment-4510628414) and used by `TicketBaiFactoryReferenceComparisonAcceptanceTests`.

| File | NN | Codes | Branch |
|---|---|---|---|
| `010_TBAI-export-transaction.xml` | [10] Exports | L9=`02`, L10=`E2` | `DesgloseTipoOperacion/Entrega/Sujeta/Exenta` |
| `007_TBAI-exempt-transaction-Art-25-intra-community-trade.xml` | [11] Intra-community delivery | L9=`01`, L10=`E5` | `DesgloseTipoOperacion/Entrega/Sujeta/Exenta` |
| `005_TBAI-exempt-transaction-Art-22.xml` | [13] Treated as exports | L9=`02`, L10=`E3` | `DesgloseTipoOperacion/Entrega/Sujeta/Exenta` |
| `006_TBAI-exempt-transaction-Art-23-24.xml` | [14] Customs / tax-regulation | L9=`02`, L10=`E4` | `DesgloseTipoOperacion/Entrega/Sujeta/Exenta` |
| `003_TBAI-not-subject-to-location-rules-transaction.xml` | [20] Not subject — localisation | L9=`01`, L13=`RL` | `DesgloseFactura/NoSujeta` |
| `002_TBAI-domestic-not-subject-transaction.xml` | [21] Not subject — Art. 7/14 | L9=`01`, L13=`OT` | `DesgloseFactura/NoSujeta` |
| `004_TBAI-exempt-domestic-transaction-Art-20.xml` | [30] Exempted domestic | L9=`01`, L10=`E1` | `DesgloseFactura/Sujeta/Exenta` |
| `008_TBAI-exempt-transaction-others.xml` | [31] Other exemptions | L9=`01`, L10=`E6` | `DesgloseFactura/Sujeta/Exenta` |
| `009_TBAI-reverse-charge-transaction.xml` | [50] Reverse charge | L9=`01`, L11=`S2` | `DesgloseTipoOperacion/PrestacionServicios/Sujeta/NoExenta` * |
| `021_TBAI-domestic-not-subject-transaction-foreign-tax.xml` | [60] Foreign tax (IPSI/IGIC) | L9=`08`, L13=`IE` | `DesgloseFactura/NoSujeta` |

The samples were stripped of their `<ds:Signature>` blocks (sample placeholder, ~35 kB each) and `<EncadenamientoFacturaAnterior>` (chain pointer, not produced by the factory for a fresh receipt). The structural test only asserts the spec-meaningful elements listed in the table above.

\* The published sample for NN [50] puts reverse charge under `DesgloseTipoOperacion/PrestacionServicios` even though the recipient is domestic. The factory follows the domestic-first rule (recipient country → `DesgloseFactura`) and places reverse charge under `DesgloseFactura/Sujeta/NoExenta/S2`; the comparison test for NN [50] therefore asserts only the L9 / L11 codes and the `TipoNoExenta=S2` value, not the branch.

NN [60] (`IE` foreign tax) currently has no value in `ChargeItemCaseNatureOfVatES`, so the factory can't produce it. The reference is kept as documentation; the corresponding test case is marked `Skip` until the upstream enum gains an `IE` variant.
