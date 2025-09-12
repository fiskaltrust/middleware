using fiskaltrust.Middleware.SCU.ES.Models;
using fiskaltrust.Middleware.SCU.ES.VeriFactu;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Helpers;
using fiskaltrust.Middleware.SCU.ES.VeriFactuModels;
using fiskaltrust.Middleware.SCU.ES.VeriFactuSoap;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers.ES;

class VeriFactuInMemoryClient : IClient
{
    Task<(Result<RespuestaRegFactuSistemaFacturacion, Error> result, GovernmentAPI governmentAPI)> IClient.SendAsync(Envelope<RequestBody> envelope)
    {
        return Task.FromResult<(Result<RespuestaRegFactuSistemaFacturacion, Error>, GovernmentAPI)>((
            new RespuestaRegFactuSistemaFacturacion
            {
                Cabecera = new Cabecera
                {
                    ObligadoEmision = new PersonaFisicaJuridicaES
                    {
                        NIF = "M0123456Q",
                        NombreRazon = "In Memory"
                    }
                },
                EstadoEnvio = EstadoEnvio.Correcto,
                TiempoEsperaEnvio = ""
            },
            new GovernmentAPI
            {
                Request = envelope.XmlSerialize(),
                Response = "<inmemory></inmemory>",
                Version = GovernmentAPISchemaVersion.V0
            }
        ));
    }
}