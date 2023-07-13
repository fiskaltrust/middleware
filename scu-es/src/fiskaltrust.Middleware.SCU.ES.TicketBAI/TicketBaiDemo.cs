using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public static class TicketBaiDemo
{
    public static TicketBaiRequest GetTicketBayRequest()
    {
        // Fixed
        var cabecera = new Cabecera
        {
            IDVersionTBAI = IDVersionTicketBaiType.Item1Period2
        };

        // Part of the config?
        var sujetos = new Sujetos
        {
            Emisor = new Emisor
            {
                NIF = "B10646545",
                ApellidosNombreRazonSocial = "CRISTIAN TECH AND CONSULTING S.L."
            },
            VariosDestinatarios = SiNoType.N,
            EmitidaPorTercerosODestinatario = EmitidaPorTercerosType.N
        };


        var huellBai = new HuellaTBAI
        {
            EncadenamientoFacturaAnterior = new EncadenamientoFacturaAnteriorType
            {
                SerieFacturaAnterior = "CTS2-2023",
                NumFacturaAnterior = "0002",
                FechaExpedicionFacturaAnterior = "26-06-2023",
                SignatureValueFirmaFacturaAnterior = "VJzuyDtdogaJ7RgGvSSqpw17xj8QUVUp/9wOlWn8W+iCMRQ1u6HC+XuRkftbec/oD0ryoyp1iB1feZuR2hzEPTZIS49rv2atWlON"
            },
            Software = new SoftwareFacturacionType
            {
                LicenciaTBAI = "TBAIGIPRE00000001035",
                EntidadDesarrolladora = new EntidadDesarrolladoraType
                {
                    NIF = "B10646545"
                },
                Nombre = "Incodebiz",
                Version = "1.0"
            },
            NumSerieDispositivo = "GP4FC5J"
        };
        var factura = new Factura
        {
            CabeceraFactura = new CabeceraFacturaType
            {
                SerieFactura = "SKE-DEMO01-2023",
                NumFactura = "0001",
                FechaExpedicionFactura = "26-06-2023",
                HoraExpedicionFactura = "17:50:13",
                FacturaSimplificada = SiNoType.S,
                FacturaEmitidaSustitucionSimplificada = SiNoType.N,
            },
            DatosFactura = new DatosFacturaType
            {
                FechaOperacion = "15-10-2021",
                DescripcionFactura = "Servicios Prueba",
                DetallesFactura = new List<IDDetalleFacturaType> {
                          new IDDetalleFacturaType
                                    {
                                        DescripcionDetalle = "test object",
                                        Cantidad = "1",
                                        ImporteUnitario = "100.00",
                                        Descuento = "0",
                                        ImporteTotal = "121.00"
                                    }
                        },
                ImporteTotalFactura = "121.00",
                Claves = new List<IDClaveType>
                        {
                            new IDClaveType
                            {
                                ClaveRegimenIvaOpTrascendencia = IdOperacionesTrascendenciaTributariaType.Item01
                            }
                        }
            },
            TipoDesglose = new TipoDesgloseType
            {
                DesgloseFactura = new DesgloseFacturaType
                {
                    Sujeta = new SujetaType
                    {
                        NoExenta = new List<DetalleNoExentaType>
                                 {
                                     new DetalleNoExentaType
                                     {
                                         TipoNoExenta = TipoOperacionSujetaNoExentaType.S1,
                                         DesgloseIVA = new List<DetalleIVAType>
                                         {
                                             new DetalleIVAType
                                             {
                                                 BaseImponible = "100.0",
                                                 TipoImpositivo = "21.0",
                                                 CuotaImpuesto = "21.0",
                                                 OperacionEnRecargoDeEquivalenciaORegimenSimplificado = SiNoType.N
                                             }
                                         }
                                     }
                                 }
                    }
                }
            }
        };

        var request = new TicketBaiRequest
        {
            Cabecera = cabecera,
            Sujetos = sujetos,
            Factura = factura,
            HuellaTBAI = huellBai
        };
        return request;
    }
}
