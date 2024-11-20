namespace PTI.Rs232Validator.Models;

/// <summary>
/// The telemetry data and metrics since the last time an acceptor was serviced.
/// </summary>
public class ServiceUsageCounters
{
    /// <summary>
    /// The total amount of movement since the last tach sensor service in mm.
    /// </summary>
    public uint DistancedMovedSinceLastTachSensorService { get; init; }
    
    /// <summary>
    /// The total amount of movement since the last bill path service in mm.
    /// </summary>
    public uint DistanceMovedSinceLastBillPathService { get; init; }
    
    /// <summary>
    /// The total amount of movement since the last belt service in mm.
    /// </summary>
    public uint DistancedMoveSinceLastBeltService { get; init; }
    
    /// <summary>
    /// The total amount of bills stacked since the last cashbox mechanism service in mm.
    /// </summary>
    public uint BillsStackedSinceLastCashboxService { get; init; }
    
    /// <summary>
    /// The total amount of movement since the last mechanical anti-stringing lever (MAS) service in mm.
    /// </summary>
    public uint DistanceMovedSinceLastMasService { get; init; }
    
    /// <summary>
    /// The total amount of movement since the last spring roller service in mm.
    /// </summary>
    public uint DistanceMovedSinceLastSpringRollerService { get; init; }
}