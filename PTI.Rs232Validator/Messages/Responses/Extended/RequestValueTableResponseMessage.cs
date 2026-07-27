using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Microsoft.VisualBasic;

namespace PTI.Rs232Validator.Messages.Responses.Extended;

public class RequestValueTableResponseMessage : ExtendedResponseMessage
{
    
    private const byte PayloadByteSize = 82;
    
    public RequestValueTableResponseMessage(IReadOnlyList<byte> payload) : base(payload)
    {
        if (!IsValid)
        {
            return;
        }

        if (payload.Count < PayloadByteSize)
        {
            PayloadIssues.Add(
                $"The payload size is {payload.Count} bytes, but {PayloadByteSize} bytes are expected.");
        }
        
        var result = new StringBuilder();

        for (var i = 0; i < 7; i++)
        {
            var offset = i * 10;
            var expectedIndex = i + 1;
            
            if (Data[offset] != expectedIndex)
            {
                PayloadIssues.Add(
                    $"The index for row {expectedIndex} is {Data[offset]}, but {expectedIndex} is expected.");
            }
            
            if (Data[offset + 1] == 0x00)
            {
                for (var j = 2; j <= 9; j++)
                {
                    if (Data[offset + j] == 0x00) continue;
                    PayloadIssues.Add(
                        $"Row {expectedIndex} contains both null and non-null bytes.");
                    break;
                }

                if (result.Length > 0)
                {
                    result.Append('|');
                }

                result.Append(",,");
                continue;
            }
            
            var isoCode = ReadIsoCode(offset + 1, expectedIndex);
            
            var baseValueText = ReadAsciiDigits(
                offset + 4, 3, expectedIndex, "base value");

            var exponentSign = ReadExponentSign(
                Data[offset + 7], expectedIndex);
            
            var exponentText = ReadAsciiDigits(offset + 8, 2, expectedIndex, "exponent text");
            
            var calculatedValue = CalculateValue(baseValueText, exponentText, exponentSign);

            if (result.Length > 0)
            {
                result.Append('|');
            }
            
            result.Append(expectedIndex);
            result.Append(',');
            result.Append(isoCode);
            result.Append(',');
            result.Append(calculatedValue);
        }
        
        ValueTable = result.ToString();
    }

    private string CalculateValue(string baseValueText, string exponent, char exponentSign)
    {
       if(!int.TryParse(baseValueText, out var baseValue))
       {
           PayloadIssues.Add($"Failed to parse base value '{baseValueText}' as an integer.");
           return string.Empty;
       }
       
       if(!int.TryParse(exponent, out var exponentValue))
       {
           PayloadIssues.Add($"Failed to parse exponent '{exponent}' as an integer.");
           return string.Empty;
       }
       
       var result = baseValue * Math.Pow(10, exponentSign == '-' ? -exponentValue : exponentValue);
       return result.ToString("F2");
    }

    private char ReadExponentSign(byte value, int rowNumber)
    {
        if (value is (byte)'+' or (byte)'-') return (char)value;
        PayloadIssues.Add($"Row {rowNumber} contains an invalid exponent sign.");
        return (char)0x00;

    }

    private string ReadAsciiDigits(int offset, int length, int rowNumber, string fieldName)
    {
        var characters = new char[length];

        for (var i = 0; i < length; i++)
        {
            var value = Data[offset + i];

            if (value is < (byte)'0' or > (byte)'9')
            {
                PayloadIssues.Add($"Row {rowNumber} contains an invalid {fieldName} character. Expected an ASCII digit");
                return string.Empty;
            }
            characters[i] = (char)value;
        }
        
        return new string(characters);
    }

    private string ReadIsoCode(int offset, int index)
    {
        var characters = new char[3];
        for (var i = 0; i < characters.Length; i++)
        {
            var value = Data[offset + i];

            if (value is < (byte)'A' or > (byte)'Z')
            {
                PayloadIssues.Add($"The ISO code for row {index} is invalid. Expected uppercase letters A-Z or null bytes, but found byte value {value}.");
                return string.Empty;
            }
            characters[i] = (char)value;
        }
        
        var result = new string(characters);
        
        return result;
    }

    public string ValueTable { get; } = string.Empty;

    public override string ToString()
    {
        return IsValid ? $"Value Table: {ValueTable}" : base.ToString();
    }
}