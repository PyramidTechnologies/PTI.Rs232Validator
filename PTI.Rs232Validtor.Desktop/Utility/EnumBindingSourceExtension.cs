using System;
using System.Windows.Markup;

namespace PTI.Rs232Validator.Desktop.Utility;

/// <summary>
/// An implementation of <see cref="MarkupExtension"/> that allows enumerations to be used as a binding source.
/// </summary>
public class EnumBindingSourceExtension : MarkupExtension
{
    private readonly Type _enumType;

    public EnumBindingSourceExtension(Type enumType)
    {
        if (!enumType.IsEnum)
        {
            throw new ArgumentException("The provided type is not an enumeration.", nameof(enumType));
        }
        
        _enumType = enumType;
    }
    
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Enum.GetValues(_enumType);
    }
}