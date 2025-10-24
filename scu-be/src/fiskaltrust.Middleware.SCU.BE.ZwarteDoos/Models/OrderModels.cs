using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

/// <summary>
/// Represents the order input data for the SignOrder mutation
/// </summary>
public class OrderInput
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = "NL";

    [JsonPropertyName("vatNo")]
    public string VatNo { get; set; } = null!;

    [JsonPropertyName("estNo")]
    public string EstNo { get; set; } = null!;

    [JsonPropertyName("posId")]
    public string PosId { get; set; } = null!;

    [JsonPropertyName("posFiscalTicketNo")]
    public int PosFiscalTicketNo { get; set; }

    [JsonPropertyName("posDateTime")]
    public string PosDateTime { get; set; } = null!;

    [JsonPropertyName("posSwVersion")]
    public string PosSwVersion { get; set; } = null!;

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = null!;

    [JsonPropertyName("terminalId")]
    public string TerminalId { get; set; } = null!;

    [JsonPropertyName("bookingPeriodId")]
    public string BookingPeriodId { get; set; } = null!;

    [JsonPropertyName("bookingDate")]
    public string BookingDate { get; set; } = null!;

    [JsonPropertyName("ticketMedium")]
    public string TicketMedium { get; set; } = "PAPER";

    [JsonPropertyName("employeeId")]
    public string EmployeeId { get; set; } = null!;

    [JsonPropertyName("costCenter")]
    public CostCenter? CostCenter { get; set; }

    [JsonPropertyName("transaction")]
    public Transaction Transaction { get; set; } = null!;
}

// Report input models
/// <summary>
/// Input data for ReportTurnoverX mutation - reporting turnover so far for current booking date
/// </summary>
public class ReportTurnoverXInput
{
    [JsonPropertyName("vatNo")]
    public string? VatNo { get; set; }

    [JsonPropertyName("estNo")]
    public string? EstNo { get; set; }

    [JsonPropertyName("posId")]
    public string? PosId { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("terminalId")]
    public string? TerminalId { get; set; }

    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; set; }
}

/// <summary>
/// Input data for ReportTurnoverZ mutation - reporting finalized turnover after closing booking date
/// </summary>
public class ReportTurnoverZInput
{
    [JsonPropertyName("vatNo")]
    public string? VatNo { get; set; }

    [JsonPropertyName("estNo")]
    public string? EstNo { get; set; }

    [JsonPropertyName("posId")]
    public string? PosId { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("terminalId")]
    public string? TerminalId { get; set; }

    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; set; }
}

/// <summary>
/// Input data for ReportUserX mutation - reporting operator statistics so far for current booking date
/// </summary>
public class ReportUserXInput
{
    [JsonPropertyName("vatNo")]
    public string? VatNo { get; set; }

    [JsonPropertyName("estNo")]
    public string? EstNo { get; set; }

    [JsonPropertyName("posId")]
    public string? PosId { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("terminalId")]
    public string? TerminalId { get; set; }

    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; set; }
}

/// <summary>
/// Input data for ReportUserZ mutation - reporting finalized operator statistics after closing booking date
/// </summary>
public class ReportUserZInput
{
    [JsonPropertyName("vatNo")]
    public string? VatNo { get; set; }

    [JsonPropertyName("estNo")]
    public string? EstNo { get; set; }

    [JsonPropertyName("posId")]
    public string? PosId { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("terminalId")]
    public string? TerminalId { get; set; }

    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; set; }
}

public class CostCenter
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = null!;
}

public class Transaction
{
    [JsonPropertyName("transactionLines")]
    public List<TransactionLine> TransactionLines { get; set; } = new();

    [JsonPropertyName("transactionTotal")]
    public decimal TransactionTotal { get; set; }
}

public class TransactionLine
{
    [JsonPropertyName("lineType")]
    public string LineType { get; set; } = "SINGLE_PRODUCT";

    [JsonPropertyName("mainProduct")]
    public MainProduct MainProduct { get; set; } = null!;

    [JsonPropertyName("lineTotal")]
    public decimal LineTotal { get; set; }
}

public class MainProduct
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = null!;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = null!;

    [JsonPropertyName("departmentId")]
    public string DepartmentId { get; set; } = null!;

    [JsonPropertyName("departmentName")]
    public string DepartmentName { get; set; } = null!;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("quantityType")]
    public string QuantityType { get; set; } = "PIECE";

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("vats")]
    public List<VatInfo> Vats { get; set; } = new();
}

public class VatInfo
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = null!;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}

/// <summary>
/// Builder class to simplify creating OrderInput objects
/// </summary>
public class OrderInputBuilder
{
    private readonly OrderInput _order = new();

    public OrderInputBuilder WithBasicInfo(string vatNo, string estNo, string posId, int ticketNo, string deviceId, string terminalId)
    {
        _order.VatNo = vatNo;
        _order.EstNo = estNo;
        _order.PosId = posId;
        _order.PosFiscalTicketNo = ticketNo;
        _order.DeviceId = deviceId;
        _order.TerminalId = terminalId;
        _order.Transaction = new Transaction();
        _order.PosDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
        _order.BookingDate = DateTime.Now.ToString("yyyy-MM-dd");
        return this;
    }

    public OrderInputBuilder WithBookingPeriod(string bookingPeriodId)
    {
        _order.BookingPeriodId = bookingPeriodId;
        return this;
    }

    public OrderInputBuilder WithEmployee(string employeeId)
    {
        _order.EmployeeId = employeeId;
        return this;
    }

    public OrderInputBuilder WithCostCenter(string id, string type, string reference)
    {
        _order.CostCenter = new CostCenter
        {
            Id = id,
            Type = type,
            Reference = reference
        };
        return this;
    }

    public OrderInputBuilder WithPosVersion(string version)
    {
        _order.PosSwVersion = version;
        return this;
    }

    public OrderInputBuilder WithLanguage(string language)
    {
        _order.Language = language;
        return this;
    }

    public OrderInputBuilder WithTicketMedium(string medium)
    {
        _order.TicketMedium = medium;
        return this;
    }

    public OrderInputBuilder AddProduct(string productId, string productName, string departmentId, 
        string departmentName, decimal quantity, decimal unitPrice, string vatLabel, decimal vatPrice)
    {
        var product = new MainProduct
        {
            ProductId = productId,
            ProductName = productName,
            DepartmentId = departmentId,
            DepartmentName = departmentName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Vats = new List<VatInfo> { new() { Label = vatLabel, Price = vatPrice } }
        };

        var line = new TransactionLine
        {
            MainProduct = product,
            LineTotal = vatPrice
        };

        _order.Transaction.TransactionLines.Add(line);
        return this;
    }

    public OrderInput Build()
    {
        // Calculate transaction total
        decimal total = 0;
        foreach (var line in _order.Transaction.TransactionLines)
        {
            total += line.LineTotal;
        }
        _order.Transaction.TransactionTotal = total;

        return _order;
    }
}