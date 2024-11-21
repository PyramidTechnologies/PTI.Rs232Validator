using PTI.Rs232Validator.Utility;
using System;

namespace PTI.Rs232Validator.Models;

/// <summary>
/// The entities that suggest a component requires service.
/// </summary>
[Flags]
public enum ServiceSuggestor : byte
{
    /// <summary>
    /// No entity suggests that a component requires service.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// The usage metrics suggest that a component requires service.
    /// </summary>
    UsageMetrics = 1 << 0,
    
    /// <summary>
    /// The diagnostics and errors of the system suggest that a component requires service.
    /// </summary>
    DiagnosticsAndError = 1 << 1
}

/// <summary>
/// The flags and info about what needs to be serviced.
/// </summary>
public class ServiceFlags
{
    /// <summary>
    /// An enumerator of <see cref="ServiceSuggestor"/> for the tach sensor.
    /// </summary>
    public ServiceSuggestor TachSensorServiceSuggestor { get; init; }
    
    /// <summary>
    /// An enumerator of <see cref="ServiceSuggestor"/> for the bill path.
    /// </summary>
    public ServiceSuggestor BillPathServiceSuggestor { get; init; }
    
    /// <summary>
    /// An enumerator of <see cref="ServiceSuggestor"/> for the cashbox belt.
    /// </summary>
    public ServiceSuggestor CashboxBeltServiceSuggestor { get; init; }
    
    /// <summary>
    /// An enumerator of <see cref="ServiceSuggestor"/> for the cashbox stacking mechanism.
    /// </summary>
    public ServiceSuggestor CashboxMechanismServiceSuggestor { get; init; }
    
    /// <summary>
    /// An enumerator of <see cref="ServiceSuggestor"/> for the mechanical anti-stringing lever (MAS).
    /// </summary>
    public ServiceSuggestor MasServiceSuggestor { get; init; }
    
    /// <summary>
    /// An enumerator of <see cref="ServiceSuggestor"/> for the spring rollers.
    /// </summary>
    public ServiceSuggestor SpringRollersServiceSuggestor { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(TachSensorServiceSuggestor).AddSpacesToCamelCase()}: {TachSensorServiceSuggestor} | " +
               $"{nameof(BillPathServiceSuggestor).AddSpacesToCamelCase()}: {BillPathServiceSuggestor} | " +
               $"{nameof(CashboxBeltServiceSuggestor).AddSpacesToCamelCase()}: {CashboxBeltServiceSuggestor} | " +
               $"{nameof(CashboxMechanismServiceSuggestor).AddSpacesToCamelCase()}: {CashboxMechanismServiceSuggestor} | " +
               $"{nameof(MasServiceSuggestor).AddSpacesToCamelCase()}: {MasServiceSuggestor} | " +
               $"{nameof(SpringRollersServiceSuggestor).AddSpacesToCamelCase()}: {SpringRollersServiceSuggestor}";
    }
}