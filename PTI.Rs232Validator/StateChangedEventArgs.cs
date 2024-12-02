using System;

namespace PTI.Rs232Validator;

/// <summary>
/// An implementation of <see cref="EventArgs"/> that contains information about a state change of an acceptor.
/// </summary>
public class StateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="StateChangedEventArgs"/>.
    /// </summary>
    /// <param name="oldState"><see cref="OldState"/>.</param>
    /// <param name="newState"><see cref="NewState"/>.</param>
    public StateChangedEventArgs(Rs232State oldState, Rs232State newState)
    {
        OldState = oldState;
        NewState = newState;
    }

    /// <summary>
    /// The previous state of the device.
    /// </summary>
    public Rs232State OldState { get; }

    /// <summary>
    /// The new state of the device.
    /// </summary>
    public Rs232State NewState { get; }
}