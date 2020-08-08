namespace PTI.Rs232Validator
{
    using System;

    /// <summary>
    ///     Serial data provider contract.
    ///     You can use this interface to provide your own serial connection or mock interface.
    /// </summary>
    public interface ISerialProvider : IDisposable
    {
        /// <summary>
        ///     Returns true if provider is in a state
        ///     that allows for reading and writing of data
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        ///     Optional logger
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        ///     Try to enter the open state or return false
        /// </summary>
        /// <returns>True on success, otherwise false</returns>
        bool TryOpen();

        /// <summary>
        ///     Close the data provider
        /// </summary>
        void Close();

        /// <summary>
        ///     Read and return count bytes from provider
        /// </summary>
        /// <param name="count">Count of bytes to read</param>
        /// <returns>Data from provider</returns>
        byte[] Read(int count);

        /// <summary>
        ///     Write data to provider
        /// </summary>
        /// <param name="data">Data to write</param>
        void Write(byte[] data);
    }
}