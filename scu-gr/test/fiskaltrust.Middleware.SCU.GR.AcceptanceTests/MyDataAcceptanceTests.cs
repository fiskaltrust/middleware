using System.Text;
using System.Xml.Serialization;
using fiskaltrust.Middleware.SCU.GR.MyData;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.SCU.GR.IntegrationTest.MyDataSCU
{
    /// <summary>
    /// Consolidated acceptance tests for all myDATA invoice types, income/expense classifications,
    /// and supplementary invoice correlation mechanisms.
    ///
    /// Each test builds raw XML and sends it to the myDATA dev endpoint to verify it is accepted.
    ///
    /// Tests are organized by invoice type (1.1, 1.2, ..., 11.5). Within each type:
    ///   - Invoice type acceptance test
    ///   - Income classification [Theory] tests
    ///   - Expense classification [Theory] tests
    ///   - Correlation / supplementary tests (where applicable)
    ///
    /// Source for classification pairs: syndiasmoi_xaraktirismwn_v1.0.10.xlsx (AADE official matrix)
    /// </summary>
    [Trait("only", "local")]
    public class MyDataAcceptanceTests
    {
        private readonly ITestOutputHelper _output;

        private const string ISSUER_VAT = "112545020";
        private const string CUSTOMER_VAT = "026883248";
        private const string EU_CUSTOMER_VAT = "ATU68541544";
        private const string THIRD_COUNTRY_VAT = "GB300325371";

        public MyDataAcceptanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region Helpers

        private ResponseDoc? GetResponse(string xmlContent)
        {
            var xmlSerializer = new XmlSerializer(typeof(ResponseDoc));
            using var stringReader = new StringReader(xmlContent);
            return xmlSerializer.Deserialize(stringReader) as ResponseDoc;
        }

        private async Task<long> SendToMyData(string xml)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://mydataapidev.aade.gr/")
            };
            httpClient.DefaultRequestHeaders.Add("aade-user-id", "user11111111");
            httpClient.DefaultRequestHeaders.Add("ocp-apim-subscription-key", "41291863a36d552c4d7fc8195d427dd3");

            _output.WriteLine("=== XML ===");
            _output.WriteLine(PrettyPrint(xml));

            var response = await httpClient.PostAsync("/myDataProvider/SendInvoices", new StringContent(xml, Encoding.UTF8, "application/xml"));
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine("=== RESPONSE ===");
            _output.WriteLine(content);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP {response.StatusCode}: {content}");

            var result = GetResponse(content);
            if (result?.response?[0]?.statusCode?.ToLower() != "success")
                throw new Exception($"myDATA rejected: {content}");

            for (var i = 0; i < result.response[0].ItemsElementName.Length; i++)
                if (result.response[0].ItemsElementName[i] == ItemsChoiceType.invoiceMark)
                    return long.Parse(result.response[0].Items[i].ToString()!);

            throw new Exception("No invoiceMark in response");
        }

        private static string PrettyPrint(string xml)
        {
            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xml);
                using var sw = new StringWriter();
                using var xw = System.Xml.XmlWriter.Create(sw, new System.Xml.XmlWriterSettings { Indent = true });
                doc.WriteTo(xw);
                xw.Flush();
                return sw.ToString();
            }
            catch { return xml; }
        }

        private static string NextAA() => DateTime.UtcNow.Ticks.ToString().Substring(8);

        private static IncomeClassificationType IC(IncomeClassificationCategoryType cat, IncomeClassificationValueType type, decimal amount) => new()
        {
            classificationCategory = cat,
            classificationType = type,
            classificationTypeSpecified = true,
            amount = amount,
        };

        private static PaymentMethodDetailType Cash(decimal amount) => new() { type = 3, amount = amount };
        private static PaymentMethodDetailType BankTransfer(decimal amount) => new() { type = 5, amount = amount };

        private static PartyType IssuerWithAddress() => new()
        {
            vatNumber = ISSUER_VAT, country = CountryType.GR, branch = 0,
            name = "Εκδότης Α.Ε.",
            address = new AddressType { street = "Λεωφόρος Βουλιαγμένης", number = "1", postalCode = "11636", city = "Αθηνών" }
        };
        private static PartyType Issuer() => new() { vatNumber = ISSUER_VAT, country = CountryType.GR, branch = 0 };
        private static PartyType GrCounterpart() => new()
        {
            vatNumber = CUSTOMER_VAT, country = CountryType.GR, branch = 0,
            address = new AddressType { street = "Κηφισίας", number = "12", postalCode = "12345", city = "Αθηνών" }
        };
        private static PartyType GrCounterpartWithName() => new()
        {
            vatNumber = CUSTOMER_VAT, country = CountryType.GR, branch = 0,
            name = "Πελάτης A.E.",
            address = new AddressType { street = "Κηφισίας", number = "12", postalCode = "12345", city = "Αθηνών" }
        };
        private static PartyType EuCounterpart() => new()
        {
            vatNumber = EU_CUSTOMER_VAT, country = CountryType.AT, branch = 0,
            name = "fiskaltrust consulting gmbh",
            address = new AddressType { street = "Alpenstraße 99a", number = "99", postalCode = "5020", city = "Salzburg" }
        };
        private static PartyType ThirdCountryCounterpart() => new()
        {
            vatNumber = THIRD_COUNTRY_VAT, country = CountryType.GB, branch = 0,
            name = "VIVA WALLET.COM LTD",
            address = new AddressType { street = "Silbury Boulevard", number = "1", postalCode = "MK9 2AH", city = "Milton Keynes" }
        };

        /// <summary>Generic invoice sender for types with standard structure.</summary>
        private async Task<long> SendInvoice(InvoiceType type, PartyType? counterpart, decimal net, decimal vat,
            IncomeClassificationType[] ic, PaymentMethodDetailType[]? pay = null,
            long[]? correlatedInvoices = null, long[]? multipleConnectedMarks = null,
            InvoiceRowType[]? customDetails = null, InvoiceSummaryType? customSummary = null,
            PartyType? issuerOverride = null, bool noCurrency = false)
        {
            var gross = net + vat;
            var inv = new AadeBookInvoiceType
            {
                invoiceHeader = new InvoiceHeaderType
                {
                    series = "A", aa = NextAA(), issueDate = DateTime.UtcNow,
                    invoiceType = type,
                    correlatedInvoices = correlatedInvoices,
                    multipleConnectedMarks = multipleConnectedMarks,
                },
                issuer = issuerOverride ?? Issuer(),
                paymentMethods = pay ?? new[] { Cash(gross) },
                invoiceDetails = customDetails ?? new[]
                {
                    new InvoiceRowType
                    {
                        lineNumber = 1, netValue = net, vatCategory = (vat > 0 ? 1 : 8), vatAmount = vat,
                        incomeClassification = ic,
                    }
                },
                invoiceSummary = customSummary ?? new InvoiceSummaryType
                {
                    totalNetValue = net, totalVatAmount = vat,
                    totalWithheldAmount = 0m, totalFeesAmount = 0m, totalStampDutyAmount = 0m,
                    totalOtherTaxesAmount = 0m, totalDeductionsAmount = 0m, totalGrossValue = gross,
                    incomeClassification = ic,
                }
            };
            if (!noCurrency)
            {
                inv.invoiceHeader.currency = CurrencyType.EUR;
                inv.invoiceHeader.currencySpecified = true;
            }
            if (counterpart != null) inv.counterpart = counterpart;

            var doc = new InvoicesDoc { invoice = new[] { inv } };
            var mark = await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));
            _output.WriteLine($">>> {type} MARK: {mark}");
            return mark;
        }

        /// <summary>Send base invoice, wait for MARK indexing, then send dependent invoice.</summary>
        private async Task<long> SendWithCorrelation(
            Func<Task<long>> sendBase,
            Func<long, Task<long>> sendDependent,
            int retries = 3, int delayMs = 2000)
        {
            var baseMark = await sendBase();
            _output.WriteLine($">>> Base MARK: {baseMark}");

            for (var attempt = 1; attempt <= retries; attempt++)
            {
                try
                {
                    await Task.Delay(delayMs);
                    return await sendDependent(baseMark);
                }
                catch (Exception ex) when (attempt < retries && ex.Message.Contains("not found"))
                {
                    _output.WriteLine($">>> MARK not yet indexed, retry {attempt}/{retries}...");
                }
            }
            throw new Exception("MARK not indexed after retries");
        }

        /// <summary>
        /// Sends a B2B invoice with the given type, counterpart, VAT setup, and income classification.
        /// </summary>
        private async Task<long> SendClassifiedInvoice(
            InvoiceType invoiceType, PartyType? counterpart,
            int vatCategory, decimal vatRate, int vatExemptionCategory,
            string categoryStr, string e3TypeStr)
        {
            var category = (IncomeClassificationCategoryType)Enum.Parse(typeof(IncomeClassificationCategoryType), categoryStr);
            var e3Type = (IncomeClassificationValueType)Enum.Parse(typeof(IncomeClassificationValueType), e3TypeStr);

            var net = 100m;
            var vat = vatCategory == 1 ? net * vatRate / 100m : 0m;
            var gross = net + vat;

            var ic = new IncomeClassificationType
            {
                classificationCategory = category,
                classificationType = e3Type,
                classificationTypeSpecified = true,
                amount = net,
            };

            var detail = new InvoiceRowType
            {
                lineNumber = 1, netValue = net, vatCategory = vatCategory, vatAmount = vat,
                incomeClassification = new[] { ic },
            };
            if (vatExemptionCategory > 0)
            {
                detail.vatExemptionCategory = vatExemptionCategory;
                detail.vatExemptionCategorySpecified = true;
            }

            var inv = new AadeBookInvoiceType
            {
                invoiceHeader = new InvoiceHeaderType
                {
                    series = "A", aa = NextAA(), issueDate = DateTime.UtcNow,
                    invoiceType = invoiceType,
                    currency = CurrencyType.EUR, currencySpecified = true,
                },
                issuer = Issuer(),
                paymentMethods = new[] { new PaymentMethodDetailType { type = 3, amount = gross } },
                invoiceDetails = new[] { detail },
                invoiceSummary = new InvoiceSummaryType
                {
                    totalNetValue = net, totalVatAmount = vat,
                    totalWithheldAmount = 0m, totalFeesAmount = 0m, totalStampDutyAmount = 0m,
                    totalOtherTaxesAmount = 0m, totalDeductionsAmount = 0m, totalGrossValue = gross,
                    incomeClassification = new[] { ic },
                }
            };
            if (counterpart != null) inv.counterpart = counterpart;

            var doc = new InvoicesDoc { invoice = new[] { inv } };
            var mark = await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));
            _output.WriteLine($">>> {invoiceType} [{categoryStr} + {e3TypeStr}] MARK: {mark}");
            return mark;
        }

        /// <summary>
        /// Sends an invoice with invoiceDetailType=1 (expense line) and only expense classification.
        /// For testing expense classification acceptance per invoice type.
        /// </summary>
        private async Task<long> SendExpenseInvoice(
            InvoiceType invoiceType, PartyType? counterpart,
            int vatCategory, decimal vatRate, int vatExemptionCategory,
            string expenseCategoryStr, string? expenseE3TypeStr)
        {
            var expenseCategory = (ExpensesClassificationCategoryType)Enum.Parse(typeof(ExpensesClassificationCategoryType), expenseCategoryStr);

            var net = 100m;
            var vat = vatCategory == 1 ? net * vatRate / 100m : 0m;
            var gross = net + vat;

            var ec = new ExpensesClassificationType
            {
                classificationCategory = expenseCategory,
                classificationCategorySpecified = true,
                amount = net,
            };
            if (expenseE3TypeStr != null)
            {
                var expenseE3Type = (ExpensesClassificationTypeClassificationType)Enum.Parse(typeof(ExpensesClassificationTypeClassificationType), expenseE3TypeStr);
                ec.classificationType = expenseE3Type;
                ec.classificationTypeSpecified = true;
            }

            var detail = new InvoiceRowType
            {
                lineNumber = 1, netValue = net, vatCategory = vatCategory, vatAmount = vat,
                invoiceDetailType = 1, invoiceDetailTypeSpecified = true,
                expensesClassification = new[] { ec },
            };
            if (vatExemptionCategory > 0)
            {
                detail.vatExemptionCategory = vatExemptionCategory;
                detail.vatExemptionCategorySpecified = true;
            }

            var inv = new AadeBookInvoiceType
            {
                invoiceHeader = new InvoiceHeaderType
                {
                    series = "A", aa = NextAA(), issueDate = DateTime.UtcNow,
                    invoiceType = invoiceType,
                    currency = CurrencyType.EUR, currencySpecified = true,
                },
                issuer = Issuer(),
                paymentMethods = new[] { new PaymentMethodDetailType { type = 3, amount = gross } },
                invoiceDetails = new[] { detail },
                invoiceSummary = new InvoiceSummaryType
                {
                    totalNetValue = net, totalVatAmount = vat,
                    totalWithheldAmount = 0m, totalFeesAmount = 0m, totalStampDutyAmount = 0m,
                    totalOtherTaxesAmount = 0m, totalDeductionsAmount = 0m, totalGrossValue = gross,
                    expensesClassification = new[] { ec },
                }
            };
            if (counterpart != null) inv.counterpart = counterpart;

            var doc = new InvoicesDoc { invoice = new[] { inv } };
            var mark = await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));
            _output.WriteLine($">>> {invoiceType} [expense: {expenseCategoryStr} + {expenseE3TypeStr ?? "none"}] MARK: {mark}");
            return mark;
        }

        /// <summary>
        /// Sends an 8.2 expense invoice with the special structure: net=0, otherTaxes, expense classification.
        /// </summary>
        private async Task<long> SendExpenseInvoice_8_2(string expenseCategoryStr, string expenseE3TypeStr)
        {
            var expenseCategory = (ExpensesClassificationCategoryType)Enum.Parse(typeof(ExpensesClassificationCategoryType), expenseCategoryStr);
            var expenseE3Type = (ExpensesClassificationTypeClassificationType)Enum.Parse(typeof(ExpensesClassificationTypeClassificationType), expenseE3TypeStr);

            var ec = new ExpensesClassificationType
            {
                classificationCategory = expenseCategory,
                classificationCategorySpecified = true,
                classificationType = expenseE3Type,
                classificationTypeSpecified = true,
                amount = 100m,
            };

            var inv = new AadeBookInvoiceType
            {
                invoiceHeader = new InvoiceHeaderType
                {
                    series = "A", aa = NextAA(), issueDate = DateTime.UtcNow,
                    invoiceType = InvoiceType.Item82,
                    currency = CurrencyType.EUR, currencySpecified = true,
                },
                issuer = Issuer(),
                paymentMethods = new[] { Cash(100m) },
                invoiceDetails = new[]
                {
                    new InvoiceRowType
                    {
                        lineNumber = 1, netValue = 0m, vatCategory = 8, vatAmount = 0m,
                        invoiceDetailType = 1, invoiceDetailTypeSpecified = true,
                        otherTaxesPercentCategory = 5, otherTaxesPercentCategorySpecified = true,
                        otherTaxesAmount = 100m, otherTaxesAmountSpecified = true,
                        expensesClassification = new[] { ec },
                    }
                },
                invoiceSummary = new InvoiceSummaryType
                {
                    totalNetValue = 0m, totalVatAmount = 0m,
                    totalWithheldAmount = 0m, totalFeesAmount = 0m, totalStampDutyAmount = 0m,
                    totalOtherTaxesAmount = 100m, totalDeductionsAmount = 0m, totalGrossValue = 100m,
                    expensesClassification = new[] { ec },
                }
            };

            var doc = new InvoicesDoc { invoice = new[] { inv } };
            var mark = await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));
            _output.WriteLine($">>> Item82 [expense: {expenseCategoryStr} + {expenseE3TypeStr}] MARK: {mark}");
            return mark;
        }

        // ────────────────────────────────────────────────────────────────
        // CORRELATION TEST BUILDERS (from SupplementaryInvoiceCorrelationTests)
        // ────────────────────────────────────────────────────────────────

        private InvoicesDoc Build_1_1(long[]? correlatedInvoices = null, long[]? multipleConnectedMarks = null)
        {
            return new InvoicesDoc
            {
                invoice = new[]
                {
                    new AadeBookInvoiceType
                    {
                        invoiceHeader = new InvoiceHeaderType
                        {
                            series = "A", aa = NextAA(), issueDate = DateTime.UtcNow,
                            invoiceType = InvoiceType.Item11,
                            currency = CurrencyType.EUR, currencySpecified = true,
                            correlatedInvoices = correlatedInvoices,
                            multipleConnectedMarks = multipleConnectedMarks,
                        },
                        issuer = Issuer(),
                        counterpart = GrCounterpart(),
                        paymentMethods = new[] { Cash(124.00m) },
                        invoiceDetails = new[]
                        {
                            new InvoiceRowType
                            {
                                lineNumber = 1, netValue = 100.00m, vatCategory = 1, vatAmount = 24.00m,
                                incomeClassification = new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_001, 100.00m) }
                            }
                        },
                        invoiceSummary = new InvoiceSummaryType
                        {
                            totalNetValue = 100.00m, totalVatAmount = 24.00m,
                            totalWithheldAmount = 0m, totalFeesAmount = 0m, totalStampDutyAmount = 0m,
                            totalOtherTaxesAmount = 0m, totalDeductionsAmount = 0m, totalGrossValue = 124.00m,
                            incomeClassification = new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_001, 100.00m) }
                        }
                    }
                }
            };
        }

        private InvoicesDoc Build_1_6(long[]? correlatedInvoices = null, long[]? multipleConnectedMarks = null)
        {
            return new InvoicesDoc
            {
                invoice = new[]
                {
                    new AadeBookInvoiceType
                    {
                        invoiceHeader = new InvoiceHeaderType
                        {
                            series = "A", aa = NextAA(), issueDate = DateTime.UtcNow,
                            invoiceType = InvoiceType.Item16,
                            currency = CurrencyType.EUR, currencySpecified = true,
                            correlatedInvoices = correlatedInvoices,
                            multipleConnectedMarks = multipleConnectedMarks,
                        },
                        issuer = Issuer(),
                        counterpart = GrCounterpart(),
                        paymentMethods = new[] { Cash(62.00m) },
                        invoiceDetails = new[]
                        {
                            new InvoiceRowType
                            {
                                lineNumber = 1, netValue = 50.00m, vatCategory = 1, vatAmount = 12.00m,
                                incomeClassification = new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_001, 50.00m) }
                            }
                        },
                        invoiceSummary = new InvoiceSummaryType
                        {
                            totalNetValue = 50.00m, totalVatAmount = 12.00m,
                            totalWithheldAmount = 0m, totalFeesAmount = 0m, totalStampDutyAmount = 0m,
                            totalOtherTaxesAmount = 0m, totalDeductionsAmount = 0m, totalGrossValue = 62.00m,
                            incomeClassification = new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_001, 50.00m) }
                        }
                    }
                }
            };
        }

        private InvoicesDoc Build_2_4(long[]? correlatedInvoices = null, long[]? multipleConnectedMarks = null)
        {
            return new InvoicesDoc
            {
                invoice = new[]
                {
                    new AadeBookInvoiceType
                    {
                        invoiceHeader = new InvoiceHeaderType
                        {
                            series = "A", aa = NextAA(), issueDate = DateTime.UtcNow,
                            invoiceType = InvoiceType.Item24,
                            currency = CurrencyType.EUR, currencySpecified = true,
                            correlatedInvoices = correlatedInvoices,
                            multipleConnectedMarks = multipleConnectedMarks,
                        },
                        issuer = Issuer(),
                        counterpart = GrCounterpart(),
                        paymentMethods = new[] { BankTransfer(99.20m) },
                        invoiceDetails = new[]
                        {
                            new InvoiceRowType
                            {
                                lineNumber = 1, netValue = 80.00m, vatCategory = 1, vatAmount = 19.20m,
                                incomeClassification = new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001, 80.00m) }
                            }
                        },
                        invoiceSummary = new InvoiceSummaryType
                        {
                            totalNetValue = 80.00m, totalVatAmount = 19.20m,
                            totalWithheldAmount = 0m, totalFeesAmount = 0m, totalStampDutyAmount = 0m,
                            totalOtherTaxesAmount = 0m, totalDeductionsAmount = 0m, totalGrossValue = 99.20m,
                            incomeClassification = new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001, 80.00m) }
                        }
                    }
                }
            };
        }

        private async Task<long> SendBaseInvoice_1_1()
        {
            var doc = Build_1_1();
            return await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));
        }

        private async Task<long> SendBaseServiceInvoice_2_1()
        {
            var doc = new InvoicesDoc
            {
                invoice = new[]
                {
                    new AadeBookInvoiceType
                    {
                        invoiceHeader = new InvoiceHeaderType
                        {
                            series = "A", aa = NextAA(), issueDate = DateTime.UtcNow,
                            invoiceType = InvoiceType.Item21,
                            currency = CurrencyType.EUR, currencySpecified = true,
                        },
                        issuer = Issuer(),
                        counterpart = GrCounterpart(),
                        paymentMethods = new[] { BankTransfer(248.00m) },
                        invoiceDetails = new[]
                        {
                            new InvoiceRowType
                            {
                                lineNumber = 1, netValue = 200.00m, vatCategory = 1, vatAmount = 48.00m,
                                incomeClassification = new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001, 200.00m) }
                            }
                        },
                        invoiceSummary = new InvoiceSummaryType
                        {
                            totalNetValue = 200.00m, totalVatAmount = 48.00m,
                            totalWithheldAmount = 0m, totalFeesAmount = 0m, totalStampDutyAmount = 0m,
                            totalOtherTaxesAmount = 0m, totalDeductionsAmount = 0m, totalGrossValue = 248.00m,
                            incomeClassification = new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001, 200.00m) }
                        }
                    }
                }
            };
            return await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 1.1 — Sales Invoice / Domestic
        // ════════════════════════════════════════════════════════════════

        #region 1.1

        [Fact]
        public async Task InvoiceType_1_1_SalesInvoice_Domestic()
        {
            var mark = await SendInvoice(InvoiceType.Item11, GrCounterpart(), 100m, 24m,
                new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_001, 100m) });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_1", "E3_561_001")]
        [InlineData("category1_2", "E3_561_001")]
        [InlineData("category1_3", "E3_561_001")]
        [InlineData("category1_4", "E3_880_001")]
        [InlineData("category1_5", "E3_561_007")]
        [InlineData("category1_7", "E3_881_001")]
        [InlineData("category1_95", "E3_596")]
        public async Task IncomeClassification_1_1(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item11, GrCounterpart(), 1, 24, 0, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        // NOTE: Expense classifications for B2B types (1.1, 1.4, 2.1, 5.2, 7.1, 8.1, 8.2) are classified
        // by the receiver (counterpart), not the issuer. The provider API rejects expensesClassification
        // on these types. Only 3.1/3.2 (Title of Acquisition) support expenses from the provider.

        // Correlation tests for 1.1
        [Fact]
        public async Task RegularInvoice_1_1_WithMultipleConnectedMarks_IsAcceptedByMyData()
        {
            var mark_first = await SendBaseInvoice_1_1();
            _output.WriteLine($">>> First 1.1 MARK: {mark_first}");

            var doc = Build_1_1(multipleConnectedMarks: new[] { mark_first });
            var mark_second = await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));
            _output.WriteLine($">>> Second 1.1 with multipleConnectedMarks MARK: {mark_second}");

            mark_second.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task RegularInvoice_1_1_WithCorrelatedInvoices_IsAcceptedByMyData()
        {
            var mark_first = await SendBaseInvoice_1_1();
            _output.WriteLine($">>> First 1.1 MARK: {mark_first}");

            var doc = Build_1_1(correlatedInvoices: new[] { mark_first });
            var mark_second = await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));
            _output.WriteLine($">>> Second 1.1 with correlatedInvoices MARK: {mark_second}");

            mark_second.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 1.2 — Sales Invoice / Intra-Community
        // ════════════════════════════════════════════════════════════════

        #region 1.2

        [Fact]
        public async Task InvoiceType_1_2_SalesInvoice_IntraCommunity()
        {
            var mark = await SendInvoice(InvoiceType.Item12, EuCounterpart(), 100m, 0m,
                new[] { IC(IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_005, 100m) },
                customDetails: new[]
                {
                    new InvoiceRowType
                    {
                        lineNumber = 1, netValue = 100m, vatCategory = 7, vatAmount = 0m,
                        vatExemptionCategory = 3, vatExemptionCategorySpecified = true,
                        incomeClassification = new[] { IC(IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_005, 100m) }
                    }
                });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_1", "E3_561_005")]
        [InlineData("category1_2", "E3_561_005")]
        [InlineData("category1_3", "E3_561_005")]
        [InlineData("category1_4", "E3_880_003")]
        [InlineData("category1_5", "E3_561_005")]
        [InlineData("category1_7", "E3_881_003")]
        public async Task IncomeClassification_1_2(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item12, EuCounterpart(), 7, 0, 3, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 1.3 — Sales Invoice / Third Country
        // ════════════════════════════════════════════════════════════════

        #region 1.3

        [Fact]
        public async Task InvoiceType_1_3_SalesInvoice_ThirdCountry()
        {
            var mark = await SendInvoice(InvoiceType.Item13, ThirdCountryCounterpart(), 100m, 0m,
                new[] { IC(IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_006, 100m) },
                customDetails: new[]
                {
                    new InvoiceRowType
                    {
                        lineNumber = 1, netValue = 100m, vatCategory = 7, vatAmount = 0m,
                        vatExemptionCategory = 4, vatExemptionCategorySpecified = true,
                        incomeClassification = new[] { IC(IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_006, 100m) }
                    }
                });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_1", "E3_561_006")]
        [InlineData("category1_2", "E3_561_006")]
        [InlineData("category1_3", "E3_561_006")]
        [InlineData("category1_4", "E3_880_004")]
        [InlineData("category1_5", "E3_561_006")]
        [InlineData("category1_7", "E3_881_004")]
        public async Task IncomeClassification_1_3(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item13, ThirdCountryCounterpart(), 7, 0, 4, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 1.4 — Sales Invoice / On Behalf of Third Parties
        // ════════════════════════════════════════════════════════════════

        #region 1.4

        [Fact]
        public async Task InvoiceType_1_4_SalesInvoice_OnBehalfOfThirdParties()
        {
            var mark = await SendInvoice(InvoiceType.Item14, GrCounterpart(), 100m, 24m,
                new[] { IC(IncomeClassificationCategoryType.category1_7, IncomeClassificationValueType.E3_881_001, 100m) });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_7", "E3_881_001")]
        public async Task IncomeClassification_1_4(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item14, GrCounterpart(), 1, 24, 0, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 1.5 — Sales Invoice / Settlement
        // ════════════════════════════════════════════════════════════════

        #region 1.5

        [Fact]
        public async Task InvoiceType_1_5_SalesInvoice_Settlement()
        {
            // 1.5 requires two lines: detailtype=1 (expense) + detailtype=2 (income)
            var expenseClassification = new ExpensesClassificationType
            {
                classificationCategory = ExpensesClassificationCategoryType.category2_9,
                classificationCategorySpecified = true,
                amount = 80.65m,
            };
            var incomeClassification = IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001, 19.35m);

            var mark = await SendInvoice(InvoiceType.Item15, GrCounterpart(), 100m, 24m,
                Array.Empty<IncomeClassificationType>(),
                customDetails: new[]
                {
                    // Line 1: expense — amount returned to principal
                    new InvoiceRowType
                    {
                        lineNumber = 1, netValue = 80.65m, vatCategory = 1, vatAmount = 19.35m,
                        invoiceDetailType = 1, invoiceDetailTypeSpecified = true,
                        expensesClassification = new[] { expenseClassification },
                    },
                    // Line 2: income — agent commission
                    new InvoiceRowType
                    {
                        lineNumber = 2, netValue = 19.35m, vatCategory = 1, vatAmount = 4.65m,
                        invoiceDetailType = 2, invoiceDetailTypeSpecified = true,
                        incomeClassification = new[] { incomeClassification },
                    },
                },
                customSummary: new InvoiceSummaryType
                {
                    totalNetValue = 100m, totalVatAmount = 24m,
                    totalWithheldAmount = 0m, totalFeesAmount = 0m, totalStampDutyAmount = 0m,
                    totalOtherTaxesAmount = 0m, totalDeductionsAmount = 0m, totalGrossValue = 124m,
                    incomeClassification = new[] { incomeClassification },
                    expensesClassification = new[] { expenseClassification },
                });
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 1.6 — Sales Invoice / Supplementary
        // ════════════════════════════════════════════════════════════════

        #region 1.6

        [Fact]
        public async Task InvoiceType_1_6_SalesInvoice_Supplementary()
        {
            var mark = await SendWithCorrelation(
                sendBase: () => SendInvoice(InvoiceType.Item11, GrCounterpart(), 100m, 24m,
                    new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_001, 100m) }),
                sendDependent: baseMark => SendInvoice(InvoiceType.Item16, GrCounterpart(), 50m, 12m,
                    new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_001, 50m) },
                    correlatedInvoices: new[] { baseMark }));
            mark.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task SupplementaryInvoice_1_6_WithCorrelatedInvoices_IsAcceptedByMyData()
        {
            var mark_1_1 = await SendBaseInvoice_1_1();
            _output.WriteLine($">>> Base 1.1 MARK: {mark_1_1}");

            var doc = Build_1_6(correlatedInvoices: new[] { mark_1_1 });
            var mark_1_6 = await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));
            _output.WriteLine($">>> Supplementary 1.6 MARK: {mark_1_6}");

            mark_1_6.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task SupplementaryInvoice_1_6_WithMultipleConnectedMarks_ShouldBeRejectedByMyData()
        {
            var mark_1_1 = await SendBaseInvoice_1_1();
            _output.WriteLine($">>> Base 1.1 MARK: {mark_1_1}");

            var doc = Build_1_6(multipleConnectedMarks: new[] { mark_1_1 });
            var act = async () => await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));

            await act.Should().ThrowAsync<Exception>("1.6 should not accept multipleConnectedMarks");
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 2.1 — Service Invoice / Domestic
        // ════════════════════════════════════════════════════════════════

        #region 2.1

        [Fact]
        public async Task InvoiceType_2_1_ServiceInvoice_Domestic()
        {
            var mark = await SendInvoice(InvoiceType.Item21, GrCounterpart(), 200m, 48m,
                new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001, 200m) },
                pay: new[] { BankTransfer(248m) });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_3", "E3_561_001")]
        [InlineData("category1_5", "E3_561_007")]
        public async Task IncomeClassification_2_1(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item21, GrCounterpart(), 1, 24, 0, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 2.2 — Service Invoice / Intra-Community
        // ════════════════════════════════════════════════════════════════

        #region 2.2

        [Fact]
        public async Task InvoiceType_2_2_ServiceInvoice_IntraCommunity()
        {
            var mark = await SendInvoice(InvoiceType.Item22, EuCounterpart(), 200m, 0m,
                new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_005, 200m) },
                pay: new[] { BankTransfer(200m) },
                customDetails: new[]
                {
                    new InvoiceRowType
                    {
                        lineNumber = 1, netValue = 200m, vatCategory = 7, vatAmount = 0m,
                        vatExemptionCategory = 3, vatExemptionCategorySpecified = true,
                        incomeClassification = new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_005, 200m) }
                    }
                });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_3", "E3_561_005")]
        [InlineData("category1_5", "E3_561_005")]
        public async Task IncomeClassification_2_2(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item22, EuCounterpart(), 7, 0, 3, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 2.3 — Service Invoice / Third Country
        // ════════════════════════════════════════════════════════════════

        #region 2.3

        [Fact]
        public async Task InvoiceType_2_3_ServiceInvoice_ThirdCountry()
        {
            var mark = await SendInvoice(InvoiceType.Item23, ThirdCountryCounterpart(), 200m, 0m,
                new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_006, 200m) },
                pay: new[] { BankTransfer(200m) },
                customDetails: new[]
                {
                    new InvoiceRowType
                    {
                        lineNumber = 1, netValue = 200m, vatCategory = 7, vatAmount = 0m,
                        vatExemptionCategory = 4, vatExemptionCategorySpecified = true,
                        incomeClassification = new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_006, 200m) }
                    }
                });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_3", "E3_561_006")]
        [InlineData("category1_5", "E3_561_006")]
        public async Task IncomeClassification_2_3(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item23, ThirdCountryCounterpart(), 7, 0, 4, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 2.4 — Service Invoice / Supplementary
        // ════════════════════════════════════════════════════════════════

        #region 2.4

        [Fact]
        public async Task InvoiceType_2_4_ServiceInvoice_Supplementary()
        {
            var mark = await SendWithCorrelation(
                sendBase: () => SendInvoice(InvoiceType.Item21, GrCounterpart(), 200m, 48m,
                    new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001, 200m) },
                    pay: new[] { BankTransfer(248m) }),
                sendDependent: baseMark => SendInvoice(InvoiceType.Item24, GrCounterpart(), 80m, 19.20m,
                    new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001, 80m) },
                    pay: new[] { BankTransfer(99.20m) },
                    correlatedInvoices: new[] { baseMark }));
            mark.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task SupplementaryServiceInvoice_2_4_WithCorrelatedInvoices_IsAcceptedByMyData()
        {
            var mark_2_1 = await SendBaseServiceInvoice_2_1();
            _output.WriteLine($">>> Base 2.1 MARK: {mark_2_1}");

            var doc = Build_2_4(correlatedInvoices: new[] { mark_2_1 });
            var mark_2_4 = await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));
            _output.WriteLine($">>> Supplementary 2.4 MARK: {mark_2_4}");

            mark_2_4.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task SupplementaryServiceInvoice_2_4_WithMultipleConnectedMarks_ShouldBeRejectedByMyData()
        {
            var mark_2_1 = await SendBaseServiceInvoice_2_1();
            _output.WriteLine($">>> Base 2.1 MARK: {mark_2_1}");

            var doc = Build_2_4(multipleConnectedMarks: new[] { mark_2_1 });
            var act = async () => await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));

            await act.Should().ThrowAsync<Exception>("2.4 should not accept multipleConnectedMarks");
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 3.1 — Title of Acquisition
        // ════════════════════════════════════════════════════════════════

        #region 3.1

        [Fact]
        public async Task InvoiceType_3_1_TitleOfAcquisition()
        {
            var mark = await SendInvoice(InvoiceType.Item31, GrCounterpart(), 100m, 0m,
                Array.Empty<IncomeClassificationType>());
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category2_1", "E3_102_002")]
        [InlineData("category2_2", "E3_202_002")]
        [InlineData("category2_3", "E3_585_004")]
        [InlineData("category2_5", "E3_585_004")]
        [InlineData("category2_7", "E3_882_002")]
        public async Task ExpenseClassification_3_1(string expenseCategory, string expenseE3Type)
        {
            var mark = await SendExpenseInvoice(InvoiceType.Item31, GrCounterpart(), 8, 0, 0, expenseCategory, expenseE3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 3.2 — Title of Acquisition / Denial
        // ════════════════════════════════════════════════════════════════

        #region 3.2

        [Fact]
        public async Task InvoiceType_3_2_TitleOfAcquisition_Denial()
        {
            var mark = await SendWithCorrelation(
                sendBase: () => SendInvoice(InvoiceType.Item31, GrCounterpart(), 100m, 0m,
                    Array.Empty<IncomeClassificationType>()),
                sendDependent: baseMark => SendInvoice(InvoiceType.Item32, GrCounterpart(), 100m, 0m,
                    Array.Empty<IncomeClassificationType>(),
                    correlatedInvoices: new[] { baseMark }));
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category2_1", "E3_102_001")]
        [InlineData("category2_2", "E3_202_001")]
        [InlineData("category2_3", "E3_585_001")]
        [InlineData("category2_5", "E3_585_001")]
        [InlineData("category2_7", "E3_882_001")]
        public async Task ExpenseClassification_3_2(string expenseCategory, string expenseE3Type)
        {
            // 3.2 requires correlation with a 3.1 base invoice
            var baseMark = await SendInvoice(InvoiceType.Item31, GrCounterpart(), 100m, 0m,
                Array.Empty<IncomeClassificationType>());
            _output.WriteLine($">>> Base 3.1 MARK for 3.2 expense test: {baseMark}");

            await Task.Delay(2000);

            var expenseCategory_ = (ExpensesClassificationCategoryType)Enum.Parse(typeof(ExpensesClassificationCategoryType), expenseCategory);
            var expenseE3Type_ = (ExpensesClassificationTypeClassificationType)Enum.Parse(typeof(ExpensesClassificationTypeClassificationType), expenseE3Type);

            var ec = new ExpensesClassificationType
            {
                classificationCategory = expenseCategory_,
                classificationCategorySpecified = true,
                classificationType = expenseE3Type_,
                classificationTypeSpecified = true,
                amount = 100m,
            };

            var inv = new AadeBookInvoiceType
            {
                invoiceHeader = new InvoiceHeaderType
                {
                    series = "A", aa = NextAA(), issueDate = DateTime.UtcNow,
                    invoiceType = InvoiceType.Item32,
                    currency = CurrencyType.EUR, currencySpecified = true,
                    correlatedInvoices = new[] { baseMark },
                },
                issuer = Issuer(),
                counterpart = GrCounterpart(),
                paymentMethods = new[] { Cash(100m) },
                invoiceDetails = new[]
                {
                    new InvoiceRowType
                    {
                        lineNumber = 1, netValue = 100m, vatCategory = 8, vatAmount = 0m,
                        invoiceDetailType = 1, invoiceDetailTypeSpecified = true,
                        expensesClassification = new[] { ec },
                    }
                },
                invoiceSummary = new InvoiceSummaryType
                {
                    totalNetValue = 100m, totalVatAmount = 0m,
                    totalWithheldAmount = 0m, totalFeesAmount = 0m, totalStampDutyAmount = 0m,
                    totalOtherTaxesAmount = 0m, totalDeductionsAmount = 0m, totalGrossValue = 100m,
                    expensesClassification = new[] { ec },
                }
            };

            var doc = new InvoicesDoc { invoice = new[] { inv } };
            var mark = await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));
            _output.WriteLine($">>> Item32 [expense: {expenseCategory} + {expenseE3Type}] MARK: {mark}");
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 5.1 — Credit Note / Referenced
        // ════════════════════════════════════════════════════════════════

        #region 5.1

        [Fact]
        public async Task InvoiceType_5_1_CreditNote_Referenced()
        {
            var mark = await SendWithCorrelation(
                sendBase: () => SendInvoice(InvoiceType.Item11, GrCounterpart(), 100m, 24m,
                    new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_001, 100m) }),
                sendDependent: baseMark => SendInvoice(InvoiceType.Item51, GrCounterpart(), 100m, 24m,
                    new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_001, 100m) },
                    correlatedInvoices: new[] { baseMark }));
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 5.2 — Credit Note / Unreferenced
        // ════════════════════════════════════════════════════════════════

        #region 5.2

        [Fact]
        public async Task InvoiceType_5_2_CreditNote_Unreferenced()
        {
            var mark = await SendInvoice(InvoiceType.Item52, GrCounterpart(), 100m, 24m,
                new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_001, 100m) });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_1", "E3_561_001")]
        [InlineData("category1_2", "E3_561_001")]
        [InlineData("category1_3", "E3_561_001")]
        [InlineData("category1_4", "E3_880_001")]
        [InlineData("category1_5", "E3_561_005")]
        [InlineData("category1_7", "E3_881_001")]
        public async Task IncomeClassification_5_2(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item52, GrCounterpart(), 1, 24, 0, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 6.1 — Own Consumption / Self-Delivery
        // ════════════════════════════════════════════════════════════════

        #region 6.1

        [Fact]
        public async Task InvoiceType_6_1_OwnConsumption()
        {
            var mark = await SendInvoice(InvoiceType.Item61, GrCounterpart(), 100m, 24m,
                new[] { IC(IncomeClassificationCategoryType.category1_6, IncomeClassificationValueType.E3_595, 100m) });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_6", "E3_595")]
        public async Task IncomeClassification_6_1(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item61, GrCounterpart(), 1, 24, 0, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 6.2 — Own Consumption / Supplementary
        // ════════════════════════════════════════════════════════════════

        #region 6.2

        [Fact]
        public async Task InvoiceType_6_2_OwnConsumption_Supplementary()
        {
            var mark = await SendInvoice(InvoiceType.Item62, GrCounterpart(), 100m, 24m,
                new[] { IC(IncomeClassificationCategoryType.category1_6, IncomeClassificationValueType.E3_595, 100m) });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_6", "E3_595")]
        public async Task IncomeClassification_6_2(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item62, GrCounterpart(), 1, 24, 0, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 7.1 — Contract - Income
        // ════════════════════════════════════════════════════════════════

        #region 7.1

        [Fact]
        public async Task InvoiceType_7_1_ContractIncome()
        {
            var mark = await SendInvoice(InvoiceType.Item71, GrCounterpart(), 200m, 48m,
                new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_007, 200m) },
                pay: new[] { BankTransfer(248m) });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_1", "E3_561_001")]
        [InlineData("category1_2", "E3_561_001")]
        [InlineData("category1_3", "E3_561_001")]
        [InlineData("category1_4", "E3_880_001")]
        [InlineData("category1_5", "E3_561_007")]
        public async Task IncomeClassification_7_1(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item71, GrCounterpart(), 1, 24, 0, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 8.1 — Rents - Income
        // ════════════════════════════════════════════════════════════════

        #region 8.1

        [Fact]
        public async Task InvoiceType_8_1_RentIncome()
        {
            var mark = await SendInvoice(InvoiceType.Item81, GrCounterpart(), 500m, 0m,
                new[] { IC(IncomeClassificationCategoryType.category1_5, IncomeClassificationValueType.E3_562, 500m) },
                pay: new[] { BankTransfer(500m) });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_3", "E3_561_001")]
        [InlineData("category1_5", "E3_562")]
        public async Task IncomeClassification_8_1(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item81, GrCounterpart(), 8, 0, 0, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 8.2 — Special Record (receipts)
        // ════════════════════════════════════════════════════════════════

        #region 8.2

        [Fact]
        public async Task InvoiceType_8_2_SpecialRecord()
        {
            var mark = await SendInvoice(InvoiceType.Item82, null, 0m, 0m,
                Array.Empty<IncomeClassificationType>(),
                customDetails: new[]
                {
                    new InvoiceRowType
                    {
                        lineNumber = 1, netValue = 0m, vatCategory = 8, vatAmount = 0m,
                        otherTaxesPercentCategory = 5, otherTaxesPercentCategorySpecified = true,
                        otherTaxesAmount = 100m, otherTaxesAmountSpecified = true,
                    }
                },
                customSummary: new InvoiceSummaryType
                {
                    totalNetValue = 0m, totalVatAmount = 0m,
                    totalWithheldAmount = 0m, totalFeesAmount = 0m, totalStampDutyAmount = 0m,
                    totalOtherTaxesAmount = 100m, totalDeductionsAmount = 0m, totalGrossValue = 100m,
                },
                pay: new[] { Cash(100m) });
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 8.4 — Payment Transfer
        // ════════════════════════════════════════════════════════════════

        #region 8.4

        [Fact]
        public async Task InvoiceType_8_4_PaymentTransfer()
        {
            var mark = await SendInvoice(InvoiceType.Item84, null, 100m, 0m,
                Array.Empty<IncomeClassificationType>(),
                pay: new[] { Cash(100m) });
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 8.5 — Payment Transfer / Refund
        // ════════════════════════════════════════════════════════════════

        #region 8.5

        [Fact]
        public async Task InvoiceType_8_5_PaymentTransfer_Refund()
        {
            var mark = await SendWithCorrelation(
                sendBase: () => SendInvoice(InvoiceType.Item84, null, 100m, 0m,
                    Array.Empty<IncomeClassificationType>(),
                    pay: new[] { Cash(100m) }),
                sendDependent: baseMark => SendInvoice(InvoiceType.Item85, null, 100m, 0m,
                    Array.Empty<IncomeClassificationType>(),
                    pay: new[] { Cash(100m) },
                    correlatedInvoices: new[] { baseMark }));
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 9.3 — Delivery Note
        // ════════════════════════════════════════════════════════════════

        #region 9.3

        [Fact]
        public async Task InvoiceType_9_3_DeliveryNote()
        {
            var inv = new AadeBookInvoiceType
            {
                invoiceHeader = new InvoiceHeaderType
                {
                    series = "A", aa = NextAA(), issueDate = DateTime.UtcNow,
                    invoiceType = InvoiceType.Item93,
                    movePurpose = 1, movePurposeSpecified = true,
                    vehicleNumber = "ΝΒΧ8311",
                    dispatchDate = DateTime.UtcNow, dispatchDateSpecified = true,
                    dispatchTime = DateTime.UtcNow, dispatchTimeSpecified = true,
                    otherDeliveryNoteHeader = new OtherDeliveryNoteHeaderType
                    {
                        loadingAddress = new AddressType { street = "Παπαδιαμάντη", number = "24", postalCode = "56429", city = "Θεσσαλονίκη" },
                        deliveryAddress = new AddressType { street = "ΙΚΤΙΝΟΥ", number = "22", postalCode = "54622", city = "ΘΕΣΣΑΛΟΝΙΚΗ" },
                        startShippingBranch = 0, startShippingBranchSpecified = true,
                        completeShippingBranch = 0, completeShippingBranchSpecified = true,
                    },
                },
                issuer = IssuerWithAddress(),
                counterpart = GrCounterpartWithName(),
                invoiceDetails = new[]
                {
                    new InvoiceRowType
                    {
                        lineNumber = 1, netValue = 0m, vatCategory = 8, vatAmount = 0m,
                        itemDescr = "Εμπόρευμα - Goods",
                        quantity = 10, quantitySpecified = true,
                        measurementUnit = 1, measurementUnitSpecified = true,
                    }
                },
                invoiceSummary = new InvoiceSummaryType
                {
                    totalNetValue = 0m, totalVatAmount = 0m,
                    totalWithheldAmount = 0m, totalFeesAmount = 0m, totalStampDutyAmount = 0m,
                    totalOtherTaxesAmount = 0m, totalDeductionsAmount = 0m, totalGrossValue = 0m,
                }
            };

            var doc = new InvoicesDoc { invoice = new[] { inv } };
            var mark = await SendToMyData(AADEFactory.GenerateInvoicePayload(doc));
            _output.WriteLine($">>> 9.3 MARK: {mark}");
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 11.1 — Retail Receipt / Goods
        // ════════════════════════════════════════════════════════════════

        #region 11.1

        [Fact]
        public async Task InvoiceType_11_1_RetailReceipt_Goods()
        {
            var mark = await SendInvoice(InvoiceType.Item111, null, 50m, 12m,
                new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_003, 50m) });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_1", "E3_561_003")]
        [InlineData("category1_2", "E3_561_003")]
        [InlineData("category1_3", "E3_561_003")]
        [InlineData("category1_4", "E3_880_002")]
        [InlineData("category1_5", "E3_561_007")]
        [InlineData("category1_6", "E3_595")]
        [InlineData("category1_7", "E3_881_002")]
        public async Task IncomeClassification_11_1(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item111, null, 1, 24, 0, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 11.2 — Retail Receipt / Services
        // ════════════════════════════════════════════════════════════════

        #region 11.2

        [Fact]
        public async Task InvoiceType_11_2_RetailReceipt_Services()
        {
            var mark = await SendInvoice(InvoiceType.Item112, null, 50m, 12m,
                new[] { IC(IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_003, 50m) });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_3", "E3_561_003")]
        [InlineData("category1_5", "E3_561_007")]
        [InlineData("category1_6", "E3_595")]
        public async Task IncomeClassification_11_2(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item112, null, 1, 24, 0, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 11.3 — Simplified Invoice
        // ════════════════════════════════════════════════════════════════

        #region 11.3

        [Fact]
        public async Task InvoiceType_11_3_SimplifiedInvoice()
        {
            var mark = await SendInvoice(InvoiceType.Item113, null, 50m, 12m,
                new[] { IC(IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_003, 50m) });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_1", "E3_561_001")]
        [InlineData("category1_2", "E3_561_001")]
        [InlineData("category1_3", "E3_561_001")]
        [InlineData("category1_4", "E3_880_001")]
        [InlineData("category1_5", "E3_561_007")]
        [InlineData("category1_6", "E3_595")]
        [InlineData("category1_7", "E3_881_001")]
        public async Task IncomeClassification_11_3(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item113, null, 1, 24, 0, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 11.4 — Retail Receipt / Refund
        // ════════════════════════════════════════════════════════════════

        #region 11.4

        [Fact]
        public async Task InvoiceType_11_4_RetailReceipt_Refund()
        {
            var mark = await SendWithCorrelation(
                sendBase: () => SendInvoice(InvoiceType.Item111, null, 50m, 12m,
                    new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_003, 50m) }),
                sendDependent: baseMark => SendInvoice(InvoiceType.Item114, null, 50m, 12m,
                    new[] { IC(IncomeClassificationCategoryType.category1_1, IncomeClassificationValueType.E3_561_003, 50m) },
                    multipleConnectedMarks: new[] { baseMark }));
            mark.Should().BeGreaterThan(0);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // 11.5 — Retail Receipt / Third-Party Sales
        // ════════════════════════════════════════════════════════════════

        #region 11.5

        [Fact]
        public async Task InvoiceType_11_5_RetailReceipt_ThirdPartySales()
        {
            var mark = await SendInvoice(InvoiceType.Item115, null, 50m, 12m,
                new[] { IC(IncomeClassificationCategoryType.category1_7, IncomeClassificationValueType.E3_881_002, 50m) });
            mark.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("category1_7", "E3_881_002")]
        public async Task IncomeClassification_11_5(string category, string e3Type)
        {
            var mark = await SendClassifiedInvoice(InvoiceType.Item115, null, 1, 24, 0, category, e3Type);
            mark.Should().BeGreaterThan(0);
        }

        #endregion
    }
}
