using System;

namespace PTI.Rs232Validator.Models;

/// <summary>
/// The info that was attached to the last service.
/// </summary>
public class ServiceInfo
{
    private const int CustomDataByteSize = 4;

    private readonly byte[] _lastCustomerService = new byte[CustomDataByteSize];
    private readonly byte[] _lastServiceCenterService = new byte[CustomDataByteSize];
    private readonly byte[] _lastOemService = new byte[CustomDataByteSize];

    /// <summary>
    /// The 4 bytes of custom data that a customer wrote to an acceptor on the last service.
    /// </summary>
    public byte[] LastCustomerService
    {
        get => _lastCustomerService;
        init
        {
            if (value.Length != 4)
            {
                throw new ArgumentException(
                    $"The provided value is {value.Length} bytes, but {CustomDataByteSize} bytes are expected.");
            }

            _lastCustomerService = value;
        }
    }

    /// <summary>
    /// The 4 bytes of custom data that a service center wrote to an acceptor on the last service.
    /// </summary>
    public byte[] LastServiceCenterService
    {
        get => _lastServiceCenterService;
        init
        {
            if (value.Length != 4)
            {
                throw new ArgumentException(
                    $"The provided value is {value.Length} bytes, but {CustomDataByteSize} bytes are expected.");
            }

            _lastServiceCenterService = value;
        }
    }

    /// <summary>
    /// The 4 bytes of custom data that an OEM wrote to an acceptor on the last service.
    /// </summary>
    public byte[] LastOemService
    {
        get => _lastOemService;
        init
        {
            if (value.Length != 4)
            {
                throw new ArgumentException(
                    $"The provided value is {value.Length} bytes, but {CustomDataByteSize} bytes are expected.");
            }

            _lastOemService = value;
        }
    }
}