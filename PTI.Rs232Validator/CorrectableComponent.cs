namespace PTI.Rs232Validator;

/// <summary>
/// The components of an acceptor that may require service.
/// </summary>
public enum CorrectableComponent : byte
{
    /// <summary>
    /// The tach sensor.
    /// </summary>
    TachSensor = 0,

    /// <summary>
    /// The bill path.
    /// </summary>
    BillPath = 1,

    /// <summary>
    /// The cashbox belt.
    /// </summary>
    CashboxBelt = 2,

    /// <summary>
    /// The cashbox stacking mechanism.
    /// </summary>
    CashboxMechanism = 3,

    /// <summary>
    /// The mechanical anti-stringing lever (MAS).
    /// </summary>
    MAS = 4,

    /// <summary>
    /// The spring rollers.
    /// </summary>
    SpringRollers = 5,

    /// <summary>
    /// All components.
    /// </summary>
    All = 0x7F
}