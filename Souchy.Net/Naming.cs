using System;
using System.Collections.Generic;
using System.Text;

namespace Souchy.Net;

public class Naming
{
    public static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        if (input.Length == 1)
            return input.ToLowerInvariant();
        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }

    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        if (input.Length == 1)
            return input.ToUpperInvariant();
        return char.ToUpperInvariant(input[0]) + input.Substring(1);
    }

    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        var sb = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            bool isDigit = char.IsDigit(c);
            if ((char.IsUpper(c) || isDigit) && i > 0 && char.IsLower(input[i - 1])) //(char.IsLower(input[i - 1]) || char.IsDigit(input[i - 1])))
            {
                sb.Append('_');
            }
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    public static string ToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        var sb = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            bool isDigit = char.IsDigit(c);
            if ((char.IsUpper(c) || isDigit) && i > 0 && char.IsLower(input[i - 1]) ) //(char.IsLower(input[i - 1]) || char.IsDigit(input[i - 1])))
            {
                sb.Append('-');
            }
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

}
