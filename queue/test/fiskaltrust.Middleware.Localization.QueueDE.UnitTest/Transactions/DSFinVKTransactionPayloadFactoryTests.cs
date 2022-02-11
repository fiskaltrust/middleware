using System;
using System.Collections.Generic;
using System.Globalization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest.Transactions
{
    public class DSFinVKTransactionPayloadFactoryTests
    {
        public static IEnumerable<object[]> ReceiptPayloadData()
        {
            yield return new object[]
{
                "Kassenbeleg-V1",
                "Beleg^0.00_0.00_0.00_0.00_0.00^-8541.83:Bar:EEE",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 4919338172267102209,
                    cbPayItems = new PayItem[]
                        {
                        new PayItem()
                        {
                            Amount = 7321.0m,
                            Description = "cash",
                            Quantity = -1.0m,
                            ftPayItemCase = 0x4445000000000002,
                            ftPayItemCaseData="{ \"CurrencyCode\":\"EEE\", \"ForeignCurrencyAmount\":8541.83 }"

                            }
                        }
                })
};
            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^0.00_0.00_0.00_0.00_0.00^-73214.83:Unbar:EEE",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 4919338172267102209,
                    cbPayItems = new PayItem[]
                        {
                        new PayItem()
                        {
                            Amount = 7321.0m,
                            Description = "cash",
                            Quantity = -1.0m,
                            ftPayItemCase = 0x4445000000000009,
                            ftPayItemCaseData="{ \"CurrencyCode\":\"EEE\", \"ForeignCurrencyAmount\":73214.83 }"

                            }
                        }
                })
            };
            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^0.00_0.00_0.00_0.00_0.00^-6324.83:Bar:EEE",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 4919338172267102209,
                    cbPayItems = new PayItem[]
                        {
                        new PayItem()
                        {
                            Amount = 7321.0m,
                            Description = "cash",
                            Quantity = -1.0m,
                            ftPayItemCase = 0x444500000000000C,
                            ftPayItemCaseData="{ \"CurrencyCode\":\"EEE\", \"ForeignCurrencyAmount\":6324.83 }"

                            }
                        }
                })
            };
            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^0.00_0.00_0.00_0.00_0.00^9998.83:Bar",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 4919338172267102209,
                    cbPayItems = new PayItem[]
                        {
                            new PayItem()
                            {
                                Amount = 9998.83m,
                                Description = "Cash",
                                Quantity = 1,
                                ftPayItemCase = 0x4445_0000_0000_0001,

                            }
                        }
                })
            };
            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^0.00_0.00_0.00_0.00_0.00^-9998.83:Bar",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 4919338172267102209,
                    cbPayItems = new PayItem[]
                        {
                            new PayItem()
                            {
                                Amount = -9998.83m,
                                Description = "Cash",
                                Quantity = 1,
                                ftPayItemCase = 0x4445_0000_0000_0001,

                            }
                        }
                })
            };
            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^0.00_0.00_0.00_0.00_0.00^-9998.83:Bar",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 4919338172267102209,
                    cbPayItems = new PayItem[]
                        {
                            new PayItem()
                            {
                                Amount = 9998.83m,
                                Description = "Cash",
                                Quantity = -1,
                                ftPayItemCase = 0x4445_0000_0000_0001,

                            }
                        }
                })
            };
            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^-1999.76519_-1999.76519_-1999.76519_-1999.76519_-1999.76519^-9998.83:Bar",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 4919338172267102209,
                    cbChargeItems = new ChargeItem[]
                        {
                            new ChargeItem()
                            {
                                Amount = 1000.234812m,
                                Description = "normal_Vat19",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000011,
                                VATRate = 19
                            },
                            new ChargeItem()
                            {
                                Amount = -3000,
                                Description = "loyality points19",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000031,
                                VATRate = 19
                            },
                           new ChargeItem()
                            {
                                Amount = 1000.234812m,
                                Description = "discounted-1_Vat7",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000010012,
                                VATRate = 7
                            },
                            new ChargeItem()
                            {
                                Amount = -3000,
                                Description = "loyality points7",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000032,
                                VATRate = 7
                            },
                            new ChargeItem()
                            {
                                Amount = 1000.234812m,
                                Description = "special-1_Vat10.7",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000010013,
                                VATRate = 10.7m
                            },
                            new ChargeItem()
                            {
                                Amount = -3000,
                                Description = "loyality points10.7",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000033,
                                VATRate = 7
                            },
                            new ChargeItem()
                            {
                                Amount = 1000.234812m,
                                Description = "special-2_Vat5.5",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000014,
                                VATRate = 5.5m
                            },
                            new ChargeItem()
                            {
                                Amount = -3000,
                                Description = "loyality points5.5",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000034,
                                VATRate = 5.5m
                            },
                            new ChargeItem()
                            {
                                Amount = 1000.234812m,
                                Description = "zero_Vat",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000015,
                                VATRate = 0
                            },
                            new ChargeItem()
                            {
                                Amount = -3000m,
                                Description = "loyality pointszero",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000035,
                                VATRate = 5.5m
                            }
                        },
                    cbPayItems = new PayItem[]
                        {
                            new PayItem()
                            {
                                Amount = -9998.83m,
                                Description = "Cash",
                                Quantity = 1,
                                ftPayItemCase = 4919338167972134913,

                            }
                        }
                })
            };
            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^2000.23481_2000.23481_2000.23481_2000.23481_2000.23481^10001.17:Bar",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 4919338172267102209,
                    cbChargeItems = new ChargeItem[]
                        {
                            new ChargeItem()
                            {
                                Amount = 3000.234812m,
                                Description = "normal_Vat19",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000011,
                                VATRate = 19
                            },
                            new ChargeItem()
                            {
                                Amount = -1000,
                                Description = "loyality points19",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000031,
                                VATRate = 19
                            },
                           new ChargeItem()
                            {
                                Amount = 3000.234812m,
                                Description = "discounted-1_Vat7",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000010012,
                                VATRate = 7
                            },
                            new ChargeItem()
                            {
                                Amount = -1000,
                                Description = "loyality points7",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000032,
                                VATRate = 7
                            },
                            new ChargeItem()
                            {
                                Amount = 3000.234812m,
                                Description = "special-1_Vat10.7",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000010013,
                                VATRate = 10.7m
                            },
                            new ChargeItem()
                            {
                                Amount = -1000,
                                Description = "loyality points10.7",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000033,
                                VATRate = 7
                            },
                            new ChargeItem()
                            {
                                Amount = 3000.234812m,
                                Description = "special-2_Vat5.5",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000014,
                                VATRate = 5.5m
                            },
                            new ChargeItem()
                            {
                                Amount = -1000,
                                Description = "loyality points5.5",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000034,
                                VATRate = 5.5m
                            },
                            new ChargeItem()
                            {
                                Amount = 3000.234812m,
                                Description = "zero_Vat",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000015,
                                VATRate = 0
                            },
                            new ChargeItem()
                            {
                                Amount = -1000m,
                                Description = "loyality pointszero",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000035,
                                VATRate = 5.5m
                            }
                        },
                    cbPayItems = new PayItem[]
                        {
                            new PayItem()
                            {
                                Amount = 10001.17406m,
                                Description = "Cash",
                                Quantity = 1,
                                ftPayItemCase = 4919338167972134913,

                            }
                        }
                })
            };

            yield return new object[]
                {
                "Kassenbeleg-V1",
                "Beleg^2000.13_3000.23481_5483.12452_1000.9632_6371.12^17855.57:Bar",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 4919338172267102209,
                    cbChargeItems = new ChargeItem[]
                        {
                            new ChargeItem()
                            {
                                Amount = 2000.13m,
                                Description = "normal_Vat19",
                                Quantity = 2,
                                ftChargeItemCase = 0x4445000000000011,
                                VATRate = 19
                            },
                            new ChargeItem()
                            {
                                Amount = 3000.234812m,
                                Description = "discounted-1_Vat7",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000010012,
                                VATRate = 7
                            },
                            new ChargeItem()
                            {
                                Amount = 5483.124518m,
                                Description = "special-1_Vat10.7",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000010013,
                                VATRate = 10.7m
                            },
                            new ChargeItem()
                            {
                                Amount = 1000.9632m,
                                Description = "special-2_Vat5.5",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000014,
                                VATRate = 5.5m
                            },
                            new ChargeItem()
                            {
                                Amount = 6371.12m,
                                Description = "zero_Vat",
                                Quantity = 1,
                                ftChargeItemCase = 0x4445000000000015,
                                VATRate = 0
                            }
                        },
                    cbPayItems = new PayItem[]
                        {
                            new PayItem()
                            {
                                Amount = 17855.568m,
                                Description = "Cash",
                                Quantity = 1,
                                ftPayItemCase = 4919338167972134913,

                            }
                        }
                })
            };

            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^5.00_7.00_0.00_0.00_0.00^12.00:Bar",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0001,
                    cbChargeItems = new ChargeItem[]
                    {
                        new ChargeItem()
                        {
                            Amount = 5.0m,
                            Description = "item1",
                            Quantity = 2.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0001
                        },
                        new ChargeItem()
                        {
                            Amount = 7.0m,
                            Description = "item2",
                            Quantity = 1.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0002
                        }
                    },
                    cbPayItems = new PayItem[]
                    {
                        new PayItem()
                        {
                            Amount = 12.0m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0001
                        }
                    }
                })
            };

            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^10.00_0.00_5.00_0.00_0.00^15.00:Bar",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0001,
                    cbChargeItems = new ChargeItem[]
                    {
                        new ChargeItem()
                        {
                            Amount = 10.0m,
                            Description = "item1",
                            Quantity = 2.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0001
                        },
                        new ChargeItem()
                        {
                            Amount = 5.0m,
                            Description = "item2",
                            Quantity = 1.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0003
                        }
                    },
                    cbPayItems = new PayItem[]
                    {
                        new PayItem()
                        {
                            Amount = 15.0m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0001
                        }
                    }
                })
            };

            yield return new object[]
{
                "Kassenbeleg-V1",
                "Beleg^10.00_0.00_5.00_0.00_0.00^15.00:Unbar",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0001,
                    cbChargeItems = new ChargeItem[]
                    {
                        new ChargeItem()
                        {
                            Amount = 10.0m,
                            Description = "item1",
                            Quantity = 2.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0001
                        },
                        new ChargeItem()
                        {
                            Amount = 5.0m,
                            Description = "item2",
                            Quantity = 1.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0003
                        }
                    },
                    cbPayItems = new PayItem[]
                    {
                        new PayItem()
                        {
                            Amount = 15.0m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0003
                        }
                    }
                })
};

            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^5.00_7.00_0.00_0.00_0.00^12.00:Bar",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0001,
                    cbChargeItems = new ChargeItem[]
                    {
                        new ChargeItem()
                        {
                            Amount = 5.0m,
                            Description = "item1",
                            Quantity = 2.0m,
                             VATRate=19.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0000
                        },
                        new ChargeItem()
                        {
                            Amount = 7.0m,
                            Description = "item2",
                            Quantity = 1.0m,
                            VATRate=7.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0000
                        }
                    },
                    cbPayItems = new PayItem[]
                    {
                        new PayItem()
                        {
                            Amount = 12.0m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0000
                        }
                    }
                })
            };

            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^5.00_7.00_0.00_0.00_0.00^12.00:Bar",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0000,
                    cbChargeItems = new ChargeItem[]
                    {
                        new ChargeItem()
                        {
                            Amount = 5.0m,
                            Description = "item1",
                            Quantity = 2.0m,
                             VATRate=19.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0000
                        },
                        new ChargeItem()
                        {
                            Amount = 7.0m,
                            Description = "item2",
                            Quantity = 1.0m,
                            VATRate=7.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0000
                        }
                    },
                    cbPayItems = new PayItem[]
                    {
                        new PayItem()
                        {
                            Amount = 12.0m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0000
                        }
                    }
                })
            };

            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^5.00_7.00_0.00_0.00_0.00^12.00:Bar",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0000,
                    cbChargeItems = new ChargeItem[]
                    {
                        new ChargeItem()
                        {
                            Amount = 5.0m,
                            Description = "item1",
                            Quantity = 2.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0001
                        },
                        new ChargeItem()
                        {
                            Amount = 7.0m,
                            Description = "item2",
                            Quantity = 1.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0002
                        }
                    },
                    cbPayItems = new PayItem[]
                    {
                        new PayItem()
                        {
                            Amount = 12.0m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0001
                        }
                    }
                })
            };

            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^0.00_0.00_0.00_0.00_0.00^",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0000,
                    cbChargeItems = Array.Empty<ChargeItem>(),
                    cbPayItems = Array.Empty<PayItem>(),
                })
            };

            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^0.00_0.00_0.00_0.00_0.00^",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0000,
                    cbChargeItems = null,
                    cbPayItems = null,
                })
            };

            yield return new object[]
            {
                "Bestellung-V1",
                "",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0010,
                    cbChargeItems = Array.Empty<ChargeItem>(),
                    cbPayItems = Array.Empty<PayItem>(),
                })
            };

            yield return new object[]
            {
                "Bestellung-V1",
                "",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0010,
                    cbChargeItems = null,
                    cbPayItems = null,
                })
            };

            yield return new object[]
            {
                "Bestellung-V1",
                "0;\"Beer\";0.00",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0010,
                    cbChargeItems = new ChargeItem[]
                    {
                        new ChargeItem
                        {
                            Amount = 0,
                            Quantity = 0,
                            Description = "Beer"
                        }
                    }
                })
            };

            yield return new object[]
            {
                "Bestellung-V1",
                "1;\"Beer\";10.21\r0.5;\"Schnitzel\";24.40",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0010,
                    cbChargeItems = new ChargeItem[]
                    {
                        new ChargeItem
                        {
                            Amount = 10.21311M,
                            Quantity = 1.0M,
                            Description = "Beer"
                        },
                        new ChargeItem
                        {
                            Amount = 12.2M,
                            Quantity = 0.5M,
                            Description = "Schnitzel"
                        }
                    }
                })
            };

            yield return new object[]
            {
                "SonstigerVorgang",
                "info-internal",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0013,
                    cbChargeItems = Array.Empty<ChargeItem>(),
                    cbPayItems = Array.Empty<PayItem>(),
                })
            };

            yield return new object[]
            {
                "SonstigerVorgang",
                "info-internal",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0013,
                    cbChargeItems = null,
                    cbPayItems = null,
                })
            };

            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^5.00_7.00_0.00_0.00_0.00^-71.18:Bar_40.00:Bar:CHF_50.00:Bar:USD",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0000,
                    cbChargeItems = new ChargeItem[]
                    {
                        new ChargeItem()
                        {
                            Amount = 5.0m,
                            Description = "item1",
                            Quantity = 2.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0001
                        },
                        new ChargeItem()
                        {
                            Amount = 7.0m,
                            Description = "item2",
                            Quantity = 1.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0002
                        }
                    },
                    cbPayItems = new PayItem[]
                    {
                        new PayItem()
                        {
                            Amount = 45.45455m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0002,
                            ftPayItemCaseData="{ \"CurrencyCode\":\"USD\", \"ForeignCurrencyAmount\":50.0 }"
                        },
                        new PayItem()
                        {
                            Amount = 47.16m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0002,
                            ftPayItemCaseData="{ \"CurrencyCode\":\"CHF\", \"ForeignCurrencyAmount\":50.0 }"
                        },
                        new PayItem()
                        {
                            Amount = -71.18m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_000B
                        },
                        new PayItem()
                        {
                            Amount = -9.43m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_000C,
                            ftPayItemCaseData="{ \"CurrencyCode\":\"CHF\", \"ForeignCurrencyAmount\":-10.0 }"
                        }
                    }
                })
            };

            yield return new object[]
            {
                "Kassenbeleg-V1",
                "Beleg^5.00_7.00_0.00_0.00_-7.00^1.00:Bar_999.00:Bar:BBB_1.99:Bar:CCC_1.00:Unbar_17.00:Unbar:EEE",
                JsonConvert.SerializeObject(
                new ReceiptRequest()
                {
                    ftReceiptCase = 0x4445_0000_0000_0000,
                    cbChargeItems = new ChargeItem[]
                    {
                        new ChargeItem()
                        {
                            Amount = 5.0m,
                            Description = "item1",
                            Quantity = 2.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0001
                        },
                        new ChargeItem()
                        {
                            Amount = 7.0m,
                            Description = "item2",
                            Quantity = 1.0m,
                            ftChargeItemCase = 0x4445_0000_0000_0002
                        }
                    },
                    cbPayItems = new PayItem[]
                    {
                        new PayItem()
                        {
                            Amount = 1.0m,
                            Description = "creditcard",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0005,
                            ftPayItemCaseData="{ \"CurrencyCode\":\"EEE\", \"ForeignCurrencyAmount\":17 }"
                        },
                        new PayItem()
                        {
                            Amount = 1.0m,
                            Description = "creditcard",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0005,
                            ftPayItemCaseData="{ \"CurrencyCode\":\"EUR\", \"ForeignCurrencyAmount\":1 }"
                        },
                        new PayItem()
                        {
                            Amount = 1.0m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0002,
                            ftPayItemCaseData="{ \"CurrencyCode\":\"CCC\", \"ForeignCurrencyAmount\":1.98999 }"
                        },
                        new PayItem()
                        {
                            Amount = 1.0m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0002,
                            ftPayItemCaseData="{ \"CurrencyCode\":\"BBB\", \"ForeignCurrencyAmount\":999 }"
                        },
                        new PayItem()
                        {
                            Amount = 1.0m,
                            Description = "cash",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_0000,
                            ftPayItemCaseData="{ \"CurrencyCode\":\"EUR\", \"ForeignCurrencyAmount\":1 }"
                        },
                        new PayItem()
                        {
                            Amount = 7.0m,
                            Description = "voucher",
                            Quantity = 1.0m,
                            ftPayItemCase = 0x4445_0000_0000_000D,
                            ftPayItemCaseData="{ \"VoucherNr\":\"UAUA91829182HH\" }"
                        }
                    }
                })
            };

        }

        public static IEnumerable<object[]> SampleCollectionData()
        {
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T2\", \"cbReceiptReference\":\"ACC-124\",\"cbReceiptMoment\":\"2020-06-29T18:05:33.912Z\",\"cbChargeItems\":[{\"Quantity\":1.0,\"Description\":\"Lavazza Gusto Mokka\",\"Amount\":4.00,\"VATRate\":19.00,\"ftChargeItemCase\":4919338167972134913,\"Moment\":\"2020-06-29T17:45:40.505Z\"},{\"Quantity\":1.0,\"Description\":\"0,3 Fanta\",\"Amount\":3.50,\"VATRate\":19.00,\"ftChargeItemCase\":4919338167972134913,\"Moment\":\"2020-06-29T17:45:40.505Z\"},{\"Quantity\":1.0,\"Description\":\"0,3 Fanta\",\"Amount\":3.50,\"VATRate\":19.00,\"ftChargeItemCase\":4919338167972134913,\"Moment\":\"2020-06-29T17:55:20.705Z\"}],\"cbPayItems\":[{\"Quantity\":1.0,\"Description\":\"Cash\",\"Amount\":11.00,\"ftPayItemCase\":4919338167972134913,\"Moment\":\"2020-06-29T18:05:33.912Z\"}],\"ftReceiptCase\":4919338167972134913}" };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"001\", \"cbReceiptReference\": \"INIT\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [], \"cbPayItems\": [], \"ftReceiptCase\": 4919338172267102211, \"cbUser\": \"Admin\"}" };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"{abcd1234}\", \"cbTerminalID\": \"T1\", \"cbReceiptReference\": \"OutOfOperation\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [], \"cbPayItems\": [], \"ftReceiptCase\": 4919338172267102212, \"ftReceiptCaseData\": \"\", \"cbUser\": \"Admin\"}" };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"1\", \"cbReceiptReference\": \"ZeroReceipt\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [], \"cbPayItems\": [], \"ftReceiptCase\": 4919338172267102210 }" };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"{abcd1234\", \"cbTerminalID\": \"1\", \"cbReceiptReference\": \"ZeroReceiptAfterFailure\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [], \"cbPayItems\": [], \"ftReceiptCase\": 4919338172275490818 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"D\", \"cbReceiptReference\": \"daily-closing-1970-01-01T00:00:00.000Z\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [], \"cbPayItems\": [], \"ftReceiptCase\": 4919338172267102215 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"D\", \"cbReceiptReference\": \"monthly-closing-1970-01-01T00:00:00.000Z\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [], \"cbPayItems\": [], \"ftReceiptCase\": 4919338172267102213 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"D\", \"cbReceiptReference\": \"yearly-closing-1970-01-01T00:00:00.000Z\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [], \"cbPayItems\": [], \"ftReceiptCase\": 4919338172267102214 }  " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T2\", \"cbReceiptReference\":\"pos-action-identification-02\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":2.0, \"Description\":\"Broetchen\", \"Amount\":2, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134929, \"Moment\":\"2020-06-29T17:45:40.505Z\" }, { \"Quantity\":1.0, \"Description\":\"Coffee to Go\", \"Amount\":2.50, \"VATRate\":7.00, \"ftChargeItemCase\":4919338167972200466, \"Moment\":\"2020-06-29T17:45:40.505Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":4.50, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"2020-06-29T18:05:33.912Z\" } ], \"ftReceiptCase\":4919338172267102209 }  " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T3\", \"cbReceiptReference\":\"pos-action-identification-03\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"Car Service\", \"Amount\":11.60, \"VATRate\":16.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-29T17:45:40.505Z\" }, { \"Quantity\":1.0, \"Description\":\"Car Service\", \"Amount\":11.90, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-29T17:45:40.505Z\" }, { \"Quantity\":1.0, \"Description\":\"Car Service\", \"Amount\":11.60, \"VATRate\":16.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-07-29T17:45:40.505Z\" }, { \"Quantity\":1.0, \"Description\":\"Car Service\", \"Amount\":11.90, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-07-29T17:45:40.505Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":47.00, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"2020-07-29T18:05:33.912Z\" } ], \"ftReceiptCase\":4919338172267102209 }  " };
            yield return new object[] { "{ \"ftCashBoxID\":\"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\":\"12\", \"cbReceiptReference\":\"BC111\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash payout to customer\", \"Amount\":-5.00, \"VATRate\":0.00, \"ftChargeItemCase\":4919338167972135029, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":-5.00, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\":4919338172267102209 }  " };
            yield return new object[] { "{ \"ftCashBoxID\":\"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\":\"12\", \"cbReceiptReference\":\"BC111\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":-5.00, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"1970-01-01T00:00:00.000Z\" }, { \"Quantity\":1.0, \"Description\":\"Cash payout to customer\", \"Amount\":5.00, \"ftPayItemCase\":4919338167972134926, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\":4919338172267102209 }  " };
            yield return new object[] { "{ \"ftCashBoxID\":\"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\":\"12\", \"cbReceiptReference\":\"BC111\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"Geschirrset\", \"Amount\":12.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134929, \"Moment\":\"1970-01-01T00:00:00.000Z\" }, { \"Quantity\":1.0, \"Description\":\"loyality points\", \"Amount\":-6.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134961, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":6.00, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\":4919338172267102209 }  " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T3\", \"cbReceiptReference\":\"dsfinv-k-p105-01\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"Bike - down Payment\", \"Amount\":500.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972135041, \"Moment\":\"2020-06-29T17:45:40.505Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"cash payment\", \"Amount\":500.00, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"2020-06-29T18:05:33.912Z\" } ], \"ftReceiptCase\":4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T3\", \"cbReceiptReference\":\"dsfinv-k-p105-01\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"Bike\", \"Amount\":3500.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134929, \"Moment\":\"2020-06-29T17:45:40.505Z\" }, { \"Quantity\":1.0, \"Description\":\"Bike - down Payment\", \"Amount\":-500.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972135049, \"Moment\":\"2020-06-29T17:45:40.505Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"cash payment\", \"Amount\":3000.00, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"2020-06-29T18:05:33.912Z\" } ], \"ftReceiptCase\":4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T1\", \"cbReceiptReference\":\"RR223\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":200.0, \"Description\":\"Toni Box\", \"Amount\":3000.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913 }, { \"Quantity\":1.0, \"Description\":\"Euro Palette\", \"Amount\":20.00, \"VATRate\":7.00, \"ftChargeItemCase\":4919338167972134946 } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":3020.00, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\":4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T1\", \"cbReceiptReference\":\"RR223\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"Euro Palette\", \"Amount\":-20.00, \"VATRate\":7.00, \"ftChargeItemCase\":4919338167972134954 } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":-20.00, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\":4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T1\", \"cbReceiptReference\":\"RR223\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"Bergbauern Milch\", \"Amount\":2.00, \"VATRate\":7.00, \"ftChargeItemCase\":4919338167972134914 }, { \"Quantity\":1.0, \"Description\":\"Pfandflasche\", \"Amount\":0.40, \"VATRate\":7.00, \"ftChargeItemCase\":4919338167972134946 } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":2.40, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\":4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T1\", \"cbReceiptReference\":\"RR223\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"Pfandflasche\", \"Amount\":-0.40, \"VATRate\":7.00, \"ftChargeItemCase\":4919338167972134954 } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":-0.40, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\":4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T1\", \"cbReceiptReference\":\"RR223\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"Voucher for RX234 MP3 Player\", \"Amount\":15.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972135009, \"ftChargeItemCaseData\":\"{\\\"VoucherNr\\\":\\\"UAUA91829182HH\\\"}\", \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":15.00, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\":4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T1\", \"cbReceiptReference\":\"RR223\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"RX234 MP3 Player\", \"Amount\":15.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134929, \"Moment\":\"1970-01-01T00:00:00.000Z\" }, { \"Quantity\":1.0, \"Description\":\"Voucher for RX234 MP3 Player\", \"Amount\":-15.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972135017, \"ftChargeItemCaseData\":\"{\\\"VoucherNr\\\":\\\"UAUA91829182HH \\\"}\", \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"cbPayItems\":[], \"ftReceiptCase\":4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"R\", \"cbReceiptReference\":\"1234-000001\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[], \"cbPayItems\":[], \"ftReceiptCase\":4919338172267102227 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"N\", \"cbReceiptReference\":\"1234-000001\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"\u00DCbernachtung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-01T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":16.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-01T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"uebernachtung\", \"Amount\":80.00, \"VATRate\":5.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-01T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-01T23:00:00.01Z\" } ], \"cbPayItems\":[], \"ftReceiptCase\":4919338172267102224 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"C\", \"cbReceiptReference\":\"1234-000001\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"0.5 Becks\", \"Amount\":5.50, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-02T21:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"0.3 Afri Cola\", \"Amount\":4.50, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-02T21:00:00.01Z\" } ], \"cbPayItems\":[], \"ftReceiptCase\":4919338172267102224 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"N\", \"cbReceiptReference\":\"1234-000001\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"\u00DCbernachtung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"\u00DCbernachtung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T23:00:00.01Z\" } ], \"cbPayItems\":[], \"ftReceiptCase\":4919338172267102224 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"D\", \"cbReceiptReference\":\"1234-000001\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"N\u00E4chtigung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-01T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-01T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"N\u00E4chtigung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-01T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-01T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"0.5 Becks\", \"Amount\":5.50, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-02T21:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"0.3 Afri Cola\", \"Amount\":4.50, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-02T21:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"N\u00E4chtigung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"N\u00E4chtigung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134937, \"Moment\":\"2020-06-02T23:00:00.01Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"EC Karte\", \"Amount\":410.00, \"ftPayItemCase\":4919338167972134916, \"Moment\":\"2020-06-03T09:00:00.01Z\" } ], \"ftReceiptCase\":4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"D\", \"cbReceiptReference\":\"5671\", \"cbPreviousReceiptReference\":\"123401\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"\u00DCbernachtung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-01T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-01T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"\u00DCbernachtung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":0, \"Description\":\"Start Vorbereitung\", \"Amount\":0, \"VATRate\":0, \"ftChargeItemCase\":4919338167972134912, \"Moment\":\"2020-06-01T17:00:00.01Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"EC Karte\", \"Amount\":200.00, \"ftPayItemCase\":4919338167972134916, \"Moment\":\"2020-06-03T09:00:00.01Z\" } ], \"ftReceiptCase\":4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"D\", \"cbReceiptReference\":\"5672\", \"cbPreviousReceiptReference\":\"123401\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"\u00DCbernachtung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-01T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-01T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"0.5 Becks\", \"Amount\":5.50, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T21:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"0.3 Afri Cola\", \"Amount\":4.50, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T21:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"\u00DCbernachtung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":0, \"Description\":\"Start Vorbereitung\", \"Amount\":0, \"VATRate\":0, \"ftChargeItemCase\":4919338167972134912, \"Moment\":\"2020-06-01T17:00:00.01Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"EC Karte\", \"Amount\":210.00, \"ftPayItemCase\":4919338167972134916, \"Moment\":\"2020-06-03T09:00:00.01Z\" } ], \"ftReceiptCase\":4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"D\", \"cbReceiptReference\":\"6781\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"0.5 Becks\", \"Amount\":5.50, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T21:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"0.3 Afri Cola\", \"Amount\":4.50, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T21:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"N\u00E4chtigung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"N\u00E4chtigung\", \"Amount\":80.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":1.0, \"Description\":\"Fr\u00FChst\u00FCck\", \"Amount\":20.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-02T23:00:00.01Z\" }, { \"Quantity\":0, \"Description\":\"Start Vorbereitung\", \"Amount\":0, \"VATRate\":0, \"ftChargeItemCase\":4919338167972134912, \"Moment\":\"2020-06-02T17:00:00.01Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"EC Karte\", \"Amount\":210.00, \"ftPayItemCase\":4919338167972134916, \"Moment\":\"2020-06-03T09:00:00.01Z\" } ], \"ftReceiptCase\":4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"1\", \"cbReceiptReference\": \"XX-3333\", \"cbPreviousReceiptReference\": \"TR-2992\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [ { \"Quantity\": 1.0, \"Description\": \"Bier 0,5 lt\", \"Amount\": -3.80, \"VATRate\": 19.00, \"ftChargeItemCase\": 4919338167972134913, \"ftChargeItemCaseData\": \"\", \"VATAmount\": -0.6067226890756302521008403361, \"CostCenter\": \"1\", \"ProductGroup\": \"Bier\", \"ProductNumber\": \"101\", \"ProductBarcode\": \"\", \"Unit\": \"Liter\", \"UnitQuantity\": 1.0, \"Moment\":\"2020-06-29T11:00:10.138887Z\" }, { \"Quantity\": 1.0, \"Description\": \"Schnitzel\", \"Amount\": -9.20, \"VATRate\": 7.00, \"ftChargeItemCase\": 4919338167972134914, \"ftChargeItemCaseData\": \"\", \"VATAmount\": -0.601869158878504672897196262, \"CostCenter\": \"1\", \"ProductGroup\": \"Speisen\", \"ProductNumber\": \"102\", \"ProductBarcode\": \"\", \"Unit\": \"Stk\", \"UnitQuantity\": 1.0, \"Moment\":\"2020-06-29T11:00:10.138887Z\" } ], \"cbPayItems\": [ { \"Quantity\": 1.0, \"Description\": \"Bar\", \"Amount\": -13.00, \"ftPayItemCase\": 4919338167972134913, \"ftPayItemCaseData\": \"\", \"CostCenter\": \"1\", \"MoneyGroup\": \"1\", \"MoneyNumber\": \"\", \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\": 4919338172267364353, \"cbReceiptAmount\": -13.00, \"cbUser\": \"Astrid\" } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"1\", \"cbReceiptReference\": \"TR-2992\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [ { \"Quantity\": 1.0, \"Description\": \"Bier 0,5 lt\", \"Amount\": 3.80, \"VATRate\": 19.00, \"ftChargeItemCase\": 4919338167972134913, \"ftChargeItemCaseData\": \"\", \"VATAmount\": 0.6067226890756302521008403361, \"CostCenter\": \"1\", \"ProductGroup\": \"Bier\", \"ProductNumber\": \"101\", \"ProductBarcode\": \"\", \"Unit\": \"Liter\", \"UnitQuantity\": 1.0, \"Moment\":\"2020-06-29T11:00:10.138887Z\" }, { \"Quantity\": 1.0, \"Description\": \"Schnitzel\", \"Amount\": 9.20, \"VATRate\": 7.00, \"ftChargeItemCase\": 4919338167972134914, \"ftChargeItemCaseData\": \"\", \"VATAmount\": 0.601869158878504672897196262, \"CostCenter\": \"1\", \"ProductGroup\": \"Speisen\", \"ProductNumber\": \"102\", \"ProductBarcode\": \"\", \"Unit\": \"Stk\", \"UnitQuantity\": 1.0, \"Moment\":\"2020-06-29T11:00:10.138887Z\" } ], \"cbPayItems\": [ { \"Quantity\": 1.0, \"Description\": \"Bar\", \"Amount\": 13.00, \"ftPayItemCase\": 4919338167972134913, \"ftPayItemCaseData\": \"\", \"CostCenter\": \"1\", \"MoneyGroup\": \"1\", \"MoneyNumber\": \"\", \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\": 4919338172267102209, \"cbReceiptAmount\": 13.00, \"cbUser\": \"Astrid\" } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"1\", \"cbReceiptReference\": \"XX-2222\", \"cbPreviousReceiptReference\": \"TR-2992\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [ { \"Quantity\": 1.0, \"Description\": \"Bier 0,5 lt\", \"Amount\": -3.80, \"VATRate\": 19.00, \"ftChargeItemCase\": 4919338167972134913, \"ftChargeItemCaseData\": \"\", \"VATAmount\": -0.6067226890756302521008403361, \"CostCenter\": \"1\", \"ProductGroup\": \"Bier\", \"ProductNumber\": \"101\", \"ProductBarcode\": \"\", \"Unit\": \"Liter\", \"UnitQuantity\": 1.0, \"Moment\":\"2020-06-29T11:00:10.138887Z\" }, { \"Quantity\": 1.0, \"Description\": \"Schnitzel\", \"Amount\": -9.20, \"VATRate\": 7.00, \"ftChargeItemCase\": 4919338167972134914, \"ftChargeItemCaseData\": \"\", \"VATAmount\": -0.601869158878504672897196262, \"CostCenter\": \"1\", \"ProductGroup\": \"Speisen\", \"ProductNumber\": \"102\", \"ProductBarcode\": \"\", \"Unit\": \"Stk\", \"UnitQuantity\": 1.0, \"Moment\":\"2020-06-29T11:00:10.138887Z\" } ], \"cbPayItems\": [ { \"Quantity\": 1.0, \"Description\": \"Bar\", \"Amount\": -13.00, \"ftPayItemCase\": 4919338167972134913, \"ftPayItemCaseData\": \"\", \"CostCenter\": \"1\", \"MoneyGroup\": \"1\", \"MoneyNumber\": \"\", \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\": 4919338172267102209, \"cbReceiptAmount\": -13.00, \"cbUser\": \"Astrid\" } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T1\", \"cbReceiptReference\": \"R12345\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [ { \"Quantity\": 2.0, \"Description\": \"Br\u00F6tchen\", \"Amount\": 2, \"VATRate\": 19.00, \"ftChargeItemCase\": 4919338167972134929, \"Moment\": \"2021-01-18T17:45:40.505Z\" }, { \"Quantity\": 1.0, \"Description\": \"Coffee to Go\", \"Amount\": 2.50, \"VATRate\": 7.00, \"ftChargeItemCase\": 4919338167972200466, \"Moment\": \"2021-01-18T17:48:10.000Z\" } ], \"cbPayItems\": [ { \"Quantity\": 1.0, \"Description\": \"Cash\", \"Amount\": 4.50, \"ftPayItemCase\": 4919338167972134913, \"Moment\": \"2021-01-18T17:48:52.000Z\" } ], \"ftReceiptCase\": 4919338172267102209 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T2\", \"cbReceiptReference\":\"pos-action-identification-01\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":2.0, \"Description\":\"Schnitzl mit Pommes\", \"Amount\":23.50, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134929, \"Moment\":\"2020-06-29T17:45:40.505Z\", \"ftChargeItemCaseData\":\"{\\\"SubItems\\\":[{\\\"Name\\\":\\\"Salat statt Pommes\\\",\\\"Quantity\\\":2.00},{\\\"Name\\\":\\\"Ketchup\\\",\\\"Quantity\\\":1.00}]}\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":23.50, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"2020-06-29T18:05:33.912Z\" } ], \"ftReceiptCase\":4919338167972134913 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T2\", \"cbReceiptReference\":\"pos-action-identification-02\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":-345.67, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"1970-01-01T00:00:00.000Z\" }, { \"Quantity\":1.0, \"Description\":\"wage payment\", \"Amount\":345.67, \"ftPayItemCase\":4919338167972134933, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\":4919338167972134913 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T2\", \"cbReceiptReference\":\"pos-action-identification-02\", \"cbReceiptMoment\":\"1970-01-01T00:00:00.000Z\", \"cbChargeItems\":[ { \"Quantity\":2.0, \"Description\":\"Br\u00F6tchen\", \"Amount\":2, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134929, \"Moment\":\"2020-06-29T17:45:05.111Z\" }, { \"Quantity\":1.0, \"Description\":\"Coffee to Go\", \"Amount\":2.50, \"VATRate\":7.00, \"ftChargeItemCase\":4919338167972200466, \"Moment\":\"2020-06-29T17:45:10.222Z\" }, { \"Quantity\":1.0, \"Description\":\"Coffee to Go\", \"Amount\":2.50, \"VATRate\":7.00, \"ftChargeItemCase\":4919338167973183506, \"Moment\":\"2020-06-29T17:46:20.333Z\" }, { \"Quantity\":1.0, \"Description\":\"Coffee to Go\", \"Amount\":2.50, \"VATRate\":7.00, \"ftChargeItemCase\":4919338167973183506, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":9.50, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"1970-01-01T00:00:00.000Z\" } ], \"ftReceiptCase\":4919338167972134913 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T2\", \"cbReceiptReference\":\"ACC-124\", \"cbReceiptMoment\":\"2020-06-29T18:05:33.912Z\", \"cbChargeItems\":[ { \"Quantity\":1.0, \"Description\":\"Lavazza Gusto Mokka\", \"Amount\":4.00, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-29T17:45:40.505Z\" }, { \"Quantity\":1.0, \"Description\":\"0,3 Fanta\", \"Amount\":3.50, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-29T17:45:40.505Z\" }, { \"Quantity\":1.0, \"Description\":\"0,3 Fanta\", \"Amount\":3.50, \"VATRate\":19.00, \"ftChargeItemCase\":4919338167972134913, \"Moment\":\"2020-06-29T17:55:20.705Z\" } ], \"cbPayItems\":[ { \"Quantity\":1.0, \"Description\":\"Cash\", \"Amount\":11.00, \"ftPayItemCase\":4919338167972134913, \"Moment\":\"2020-06-29T18:05:33.912Z\" } ], \"ftReceiptCase\":4919338167972134913 }" };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T2\", \"cbReceiptReference\": \"pos-action-shift-03092020\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [ { \"Quantity\": 1.0, \"Description\": \"Cash transfer to till\", \"Amount\": 30.00, \"VATRate\": 0.00, \"ftChargeItemCase\": 4919338167972135056, \"Moment\": \"1970-01-01T00:00:00.000Z\" } ], \"cbPayItems\": [ { \"Quantity\": 1.0, \"Description\": \"Cash\", \"Amount\": 30.00, \"ftPayItemCase\": 4919338167972134913, \"Moment\": \"1970-01-01T00:00:00.000Z\" } ], \"cbUser\": \"Markus\", \"ftReceiptCase\": 4919338167972134929 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T2\", \"cbReceiptReference\": \"pos-action-shift-03092020\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [], \"cbPayItems\": [ { \"Quantity\": 1.0, \"Description\": \"Cash\", \"Amount\": 200.00, \"ftPayItemCase\": 4919338167972134913, \"Moment\": \"1970-01-01T00:00:00.000Z\" }, { \"Quantity\": 1.0, \"Description\": \"Cash transfer to till\", \"Amount\": -200.00, \"ftPayItemCase\": 4919338167972134932, \"Moment\": \"1970-01-01T00:00:00.000Z\" } ], \"cbUser\": \"Markus\", \"ftReceiptCase\": 4919338167972134929 } " };
            yield return new object[] { "{ \"ftCashBoxID\": \"abcd1234\", \"ftPosSystemId\": \"abcd1234\", \"cbTerminalID\": \"T2\", \"cbReceiptReference\": \"pos-action-shift-03092020\", \"cbReceiptMoment\": \"1970-01-01T00:00:00.000Z\", \"cbChargeItems\": [], \"cbPayItems\": [ { \"Quantity\": 1.0, \"Description\": \"Cash\", \"Amount\": -200.00, \"ftPayItemCase\": 4919338167972134913, \"Moment\": \"1970-01-01T00:00:00.000Z\" }, { \"Quantity\": 1.0, \"Description\": \"Cash transfer from till\", \"Amount\": 200.00, \"ftPayItemCase\": 4919338167972134932, \"Moment\": \"1970-01-01T00:00:00.000Z\" } ], \"cbUser\": \"Markus\", \"ftReceiptCase\": 4919338167972134929 } " };
        }

        [Theory]
        [MemberData(nameof(ReceiptPayloadData))]
        public void CreateReceiptPayload_Should_Create_Valid_Payload_For_NationalCurrency(string expectedProcessType, string expectedPayload, string request)
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(request);

            var sut = new DSFinVKTransactionPayloadFactory();
            var (processType, payload) = sut.CreateReceiptPayload(receiptRequest);

            processType.Should().Be(expectedProcessType);
            payload.Should().Be(expectedPayload);
        }

        [Theory]
        [MemberData(nameof(SampleCollectionData))]
        public void CreateReceiptPayload_Should_CreatePayloads_ThatAreEqualOnBothSides(string request)
        {
            var paySum = 0.00M;
            var vatSum = 0.00M;

            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(request);
            var sut = new DSFinVKTransactionPayloadFactory();
            var (processType, payload) = sut.CreateReceiptPayload(receiptRequest);
            if (processType != "Kassenbeleg-V1")
            {
                return;
            }

            var splitpayload = payload.Split('^');
            var splitvats = splitpayload[1].Split('_');
            foreach (var vatitem in splitvats)
            {
                vatSum += decimal.Parse(vatitem, CultureInfo.InvariantCulture);
            }

            var paysumwithcurrency = splitpayload[2].Split('_');

            foreach (var paymentitem in paysumwithcurrency)
            {
                var payblock = paymentitem.Split(':');
                if (!string.IsNullOrEmpty(payblock[0]))
                {
                    paySum += decimal.Parse(payblock[0], CultureInfo.InvariantCulture);
                }
            }
            vatSum.Should().Be(paySum);
        }
    }
}
