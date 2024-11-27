using System.Windows;

namespace PTI.Rs232Validator.Desktop.Views;

// This portion processes bills in escrow.
partial class MainWindow
{
    private static readonly object ManualLock = new();
    private bool _isInEscrowMode;
    private bool _isBillInEscrow;

    /// <summary>
    /// <see cref="Rs232Configuration.ShouldEscrow"/>.
    /// </summary>
    public bool IsInEscrowMode
    {
        get => _isInEscrowMode;
        set
        {
            if (_rs232Configuration is not null)
            {
                _rs232Configuration.ShouldEscrow = value;
            }
            
            _isInEscrowMode = value;
            NotifyPropertyChanged(nameof(IsInEscrowMode));
        }
    }

    /// <summary>
    /// Is a bill in escrow?
    /// </summary>
    public bool IsBillInEscrow
    {
        get => _isBillInEscrow;
        set
        {
            _isBillInEscrow = value;
            NotifyPropertyChanged(nameof(IsBillInEscrow));
        }
    }
    
    private void BillValidator_OnBillEscrowed(object? sender, byte billType)
    {
        LogInfo("Escrowed a bill of type {0}.", billType);

        DoOnUiThread(() =>
        {
            // Rejects are triggered by:
            // 1) invalid bills
            // 2) cheat attempts

            // Returns are triggered by:
            // 1) bills disabled by the enable mask
            // 2) manual delivery of a poll message requesting that the bill be returned

            lock (ManualLock)
            {
                IsBillInEscrow = true;
            }
        });
    }

    /// <summary>
    /// Notifies <see cref="_billValidator"/> to stack the bill in escrow.
    /// </summary>
    private void StackButton_Click(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        billValidator.StackBill();

        lock (ManualLock)
        {
            IsBillInEscrow = false;
        }
    }

    /// <summary>
    /// Notifies <see cref="_billValidator"/> to return the bill in escrow.
    /// </summary>
    private void ReturnButton_Click(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        billValidator.ReturnBill();

        lock (ManualLock)
        {
            IsBillInEscrow = false;
        }
    }
}