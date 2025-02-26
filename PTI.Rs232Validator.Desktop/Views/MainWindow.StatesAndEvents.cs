﻿using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace PTI.Rs232Validator.Desktop.Views;

// This portion displays the current state and latest events of an acceptor.
partial class MainWindow
{
    private static readonly SolidColorBrush InactiveBrush = new(Colors.LightGray);
    private static readonly SolidColorBrush CashBoxAttachedBrush = new(Colors.LightYellow);
    private static readonly SolidColorBrush ActiveEventBrush = new(Colors.LightGreen);
    private static readonly SolidColorBrush ActiveStateBrush = new(Colors.LightBlue);

    private Rs232State _state = Rs232State.None;
    private Rs232Event _event = Rs232Event.None;

    /// <summary>
    /// The <see cref="Rs232State"/> enumerator for <see cref="_billValidator"/>.
    /// </summary>
    public Rs232State State
    {
        get => _state;
        set
        {
            DoOnUiThread(() =>
            {
                DeactivateButtonsWithTag(_stateTagText);
                switch (value)
                {
                    case Rs232State.Idling:
                        IdlingButton.Background = ActiveStateBrush;
                        break;
                    case Rs232State.Accepting:
                        AcceptingButton.Background = ActiveStateBrush;
                        break;
                    case Rs232State.Escrowed:
                        EscrowedButton.Background = ActiveStateBrush;
                        break;
                    case Rs232State.Stacking:
                        StackingButton.Background = ActiveStateBrush;
                        break;
                    case Rs232State.Returning:
                        ReturningButton.Background = ActiveStateBrush;
                        break;
                    case Rs232State.BillJammed:
                        BillJammedButton.Background = ActiveStateBrush;
                        break;
                    case Rs232State.StackerFull:
                        StackerFullButton.Background = ActiveStateBrush;
                        break;
                    case Rs232State.Failure:
                        FailureButton.Background = ActiveStateBrush;
                        break;
                }
            });

            _state = value;
            NotifyPropertyChanged(nameof(State));
        }
    }

    /// <summary>
    /// The <see cref="Rs232Event"/> enumerators reported by <see cref="_billValidator"/>.
    /// </summary>
    public Rs232Event Event
    {
        get => _event;
        set
        {
            DoOnUiThread(() =>
            {
                DeactivateButtonsWithTag(_eventTagText);
                
                if (value.HasFlag(Rs232Event.Stacked))
                {
                    StackedButton.Background = ActiveEventBrush;
                }
                if (value.HasFlag(Rs232Event.Returned))
                {
                    ReturnedButton.Background = ActiveEventBrush;
                }
                if (value.HasFlag(Rs232Event.Cheated))
                {
                    CheatedButton.Background = ActiveEventBrush;
                }
                if (value.HasFlag(Rs232Event.BillRejected))
                {
                    RejectedButton.Background = ActiveEventBrush;
                }
                if (value.HasFlag(Rs232Event.PowerUp))
                {
                    LogInfo("The bill acceptor was powered up.");
                }
            });

            _event = value;
            NotifyPropertyChanged(nameof(Event));
        }
    }

    private void BillValidator_OnStateChanged(object? sender, StateChangedEventArgs eventArgs)
    {
        LogInfo("The state changed from {0} to {1}.", eventArgs.OldState, eventArgs.NewState);
        State = eventArgs.NewState;
    }

    private void BillValidator_OnEventReported(object? sender, Rs232Event rs232Event)
    {
        LogInfo("Received event(s): {0}.", rs232Event);
        Event = rs232Event;
    }

    private void BillValidator_CashboxAttached(object? sender, EventArgs e)
    {
        LogInfo("The cashbox was attached.");
        DoOnUiThread(() => CashboxButton.Background = CashBoxAttachedBrush);
    }

    private void BillValidator_CashboxRemoved(object? sender, EventArgs e)
    {
        LogInfo("The cashbox was removed.");
        DoOnUiThread(() => CashboxButton.Background = InactiveBrush);
    }

    private void DeactivateButtonsWithTag(string tagText)
    {
        var buttons = StateMachine.Children.OfType<Button>();
        foreach (var button in buttons)
        {
            if (button.Tag as string == tagText)
            {
                button.Background = InactiveBrush;
            }
        }
    }
}