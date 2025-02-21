﻿using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class RegisterCashDepositResponse
    {
        /// <summary>
        /// Cash deposit fiscalization code.
        /// </summary>
        /// <remarks>
        /// A unique code generated by the central invoice register (CIS) service for each successful cash deposit registration.
        /// </remarks>
        public string FCDC { get; set; }
    }
}
