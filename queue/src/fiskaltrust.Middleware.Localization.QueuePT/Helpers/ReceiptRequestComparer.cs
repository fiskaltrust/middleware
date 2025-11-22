using System.Text;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueuePT.Helpers;

public static class ReceiptRequestComparer
{
    public static (bool areEqual, string differences) Compare(ReceiptRequest original, ReceiptRequest incoming)
    {
        var differences = new StringBuilder();
        
        // Compare basic properties
        CompareProperty(differences, nameof(ReceiptRequest.cbArea), original.cbArea, incoming.cbArea);
        CompareProperty(differences, nameof(ReceiptRequest.cbCustomer), original.cbCustomer, incoming.cbCustomer);
        CompareProperty(differences, nameof(ReceiptRequest.cbReceiptAmount), original.cbReceiptAmount, incoming.cbReceiptAmount);
        CompareProperty(differences, nameof(ReceiptRequest.cbSettlement), original.cbSettlement, incoming.cbSettlement);
        CompareProperty(differences, nameof(ReceiptRequest.cbTerminalID), original.cbTerminalID, incoming.cbTerminalID);
        CompareProperty(differences, nameof(ReceiptRequest.cbUser), original.cbUser, incoming.cbUser);
        //CompareProperty(differences, nameof(ReceiptRequest.ftReceiptCase), original.ftReceiptCase, incoming.ftReceiptCase);
        CompareProperty(differences, nameof(ReceiptRequest.ftReceiptCaseData), original.ftReceiptCaseData, incoming.ftReceiptCaseData);
        
        // Compare ChargeItems
        if (!CompareChargeItems(differences, original.cbChargeItems, incoming.cbChargeItems))
        {
            // Differences already added in CompareChargeItems
        }
        
        // Compare PayItems
        if (!ComparePayItems(differences, original.cbPayItems, incoming.cbPayItems))
        {
            // Differences already added in ComparePayItems
        }
        
        var differencesText = differences.ToString();
        return (string.IsNullOrEmpty(differencesText), differencesText);
    }
    
    private static void CompareProperty<T>(StringBuilder differences, string propertyName, T? original, T? incoming)
    {
        if (!Equals(original, incoming))
        {
            differences.AppendLine($"{propertyName}: Original='{original}', Incoming='{incoming}'");
        }
    }
    
    private static bool CompareChargeItems(StringBuilder differences, List<ChargeItem>? original, List<ChargeItem>? incoming)
    {
        if (original is null && incoming is null)
        {
            return true;
        }
        
        if (original is null || incoming is null)
        {
            differences.AppendLine($"cbChargeItems: Original is {(original is null ? "null" : "not null")}, Incoming is {(incoming is null ? "null" : "not null")}");
            return false;
        }
        
        if (original.Count != incoming.Count)
        {
            differences.AppendLine($"cbChargeItems.Count: Original={original.Count}, Incoming={incoming.Count}");
            return false;
        }
        
        var allEqual = true;
        for (int i = 0; i < original.Count; i++)
        {
            if (!CompareChargeItem(differences, i, original[i], incoming[i]))
            {
                allEqual = false;
            }
        }
        
        return allEqual;
    }
    
    private static bool CompareChargeItem(StringBuilder differences, int index, ChargeItem original, ChargeItem incoming)
    {
        var itemDifferences = new StringBuilder();
        
        CompareProperty(itemDifferences, "Quantity", original.Quantity, incoming.Quantity);
        CompareProperty(itemDifferences, "Description", original.Description, incoming.Description);
        CompareProperty(itemDifferences, "Amount", original.Amount, incoming.Amount);
        CompareProperty(itemDifferences, "VATRate", original.VATRate, incoming.VATRate);
        CompareProperty(itemDifferences, "VATAmount", original.VATAmount, incoming.VATAmount);
        CompareProperty(itemDifferences, "ftChargeItemCase", original.ftChargeItemCase, incoming.ftChargeItemCase);
        CompareProperty(itemDifferences, "ftChargeItemCaseData", original.ftChargeItemCaseData, incoming.ftChargeItemCaseData);
        CompareProperty(itemDifferences, "ProductGroup", original.ProductGroup, incoming.ProductGroup);
        CompareProperty(itemDifferences, "ProductNumber", original.ProductNumber, incoming.ProductNumber);
        CompareProperty(itemDifferences, "ProductBarcode", original.ProductBarcode, incoming.ProductBarcode);
        CompareProperty(itemDifferences, "Unit", original.Unit, incoming.Unit);
        CompareProperty(itemDifferences, "UnitQuantity", original.UnitQuantity, incoming.UnitQuantity);
        CompareProperty(itemDifferences, "UnitPrice", original.UnitPrice, incoming.UnitPrice);
        CompareProperty(itemDifferences, "Moment", original.Moment, incoming.Moment);
        CompareProperty(itemDifferences, "CostCenter", original.CostCenter, incoming.CostCenter);
        
        if (itemDifferences.Length > 0)
        {
            differences.AppendLine($"cbChargeItems[{index}]:");
            differences.Append(itemDifferences);
            return false;
        }
        
        return true;
    }
    
    private static bool ComparePayItems(StringBuilder differences, List<PayItem>? original, List<PayItem>? incoming)
    {
        if (original is null && incoming is null)
        {
            return true;
        }
        
        if (original is null || incoming is null)
        {
            differences.AppendLine($"cbPayItems: Original is {(original is null ? "null" : "not null")}, Incoming is {(incoming is null ? "null" : "not null")}");
            return false;
        }
        
        if (original.Count != incoming.Count)
        {
            differences.AppendLine($"cbPayItems.Count: Original={original.Count}, Incoming={incoming.Count}");
            return false;
        }
        
        var allEqual = true;
        for (int i = 0; i < original.Count; i++)
        {
            if (!ComparePayItem(differences, i, original[i], incoming[i]))
            {
                allEqual = false;
            }
        }
        
        return allEqual;
    }
    
    private static bool ComparePayItem(StringBuilder differences, int index, PayItem original, PayItem incoming)
    {
        var itemDifferences = new StringBuilder();
        
        CompareProperty(itemDifferences, "Quantity", original.Quantity, incoming.Quantity);
        CompareProperty(itemDifferences, "Description", original.Description, incoming.Description);
        CompareProperty(itemDifferences, "Amount", original.Amount, incoming.Amount);
        CompareProperty(itemDifferences, "ftPayItemCase", original.ftPayItemCase, incoming.ftPayItemCase);
        CompareProperty(itemDifferences, "ftPayItemCaseData", original.ftPayItemCaseData, incoming.ftPayItemCaseData);
        CompareProperty(itemDifferences, "Moment", original.Moment, incoming.Moment);
        CompareProperty(itemDifferences, "CostCenter", original.CostCenter, incoming.CostCenter);
        CompareProperty(itemDifferences, "MoneyGroup", original.MoneyGroup, incoming.MoneyGroup);
        CompareProperty(itemDifferences, "MoneyNumber", original.MoneyNumber, incoming.MoneyNumber);
        
        if (itemDifferences.Length > 0)
        {
            differences.AppendLine($"cbPayItems[{index}]:");
            differences.Append(itemDifferences);
            return false;
        }
        
        return true;
    }
}
