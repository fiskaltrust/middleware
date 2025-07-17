using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Helpers
{
    public record Error
    {
        public record Http(HttpStatusCode statusCode, string Value) : Error();
        public record Soap(string Value) : Error();
        public record Xml(Exception Ex, string Value) : Error();

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        public override string ToString() => this switch
        {
            Http http => $"HTTP Error: ({http.statusCode}) {http.Value}",
            Soap soap => $"SOAP Error: {soap.Value}",
            Xml xml => $"XML Error: {xml.Ex.Message}\n---\n{xml.Value}",
        };
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
    }
}
