using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.ME.InMemory
{
    internal class InMemorySCUConfiguration
    {
        /// <summary>
        /// Tax identification number (TIN) of the PosOperator.
        /// </summary>
        public string TIN { get; set; }

        /// <summary>
        /// VAT number of the PosOperator.
        /// </summary>
        public string VatNumber { get; set; }

        /// <summary>
        /// Name of the PosOperator.
        /// </summary>
        public string PosOperatorName { get; set; }

        /// <summary>
        /// Address of the PosOperator.
        /// </summary>
        public string PosOperatorAddress { get; set; }

        /// <summary>
        /// Town of the PosOperator.
        /// </summary>
        public string PosOperatorTown { get; set; }

        /// <summary>
        /// Country of the PosOperator.
        /// </summary>
        public string PosOperatorCountry { get; set; }
    }
}
