using PTI.Rs232Validator.Messages.Responses.Telemetry;

namespace PTI.Rs232Validator.Test.Tests;

public class TelemetryResponseMessageTests
{
    [Test]
    public void TelemetryResponseMessage_DeserializesData()
    {
        var responsePayload = new byte[]
            { 0x02, 0x0E, 0x61, 0x32, 0x31, 0x30, 0x32, 0x36, 0x30, 0x30, 0x31, 0x30, 0x03, 0x59 };

        var expectedData = new byte[] { 0x32, 0x31, 0x30, 0x32, 0x36, 0x30, 0x30, 0x31, 0x30 };

        var telemetryResponseMessage = new TelemetryResponseMessage(responsePayload);

        Assert.That(telemetryResponseMessage.IsValid, Is.True);
        Assert.That(telemetryResponseMessage.Data, Is.EqualTo(expectedData));
    }

    [Test]
    public void GetSerialNumberResponseMessage_DeserializesSerialNumber()
    {
        var responsePayload = new byte[]
            { 0x02, 0x0E, 0x61, 0x32, 0x31, 0x30, 0x32, 0x36, 0x30, 0x30, 0x31, 0x30, 0x03, 0x59 };

        const string expectedSerialNumber = "210260010";

        var getSerialNumberResponseMessage = new GetSerialNumberResponseMessage(responsePayload);

        Assert.That(getSerialNumberResponseMessage.IsValid, Is.True);
        Assert.That(getSerialNumberResponseMessage.SerialNumber, Is.EqualTo(expectedSerialNumber));
    }

    [Test]
    public void GetCashboxMetricsResponseMessage_DeserializesCashboxMetrics()
    {
        var responsePayload = new byte[]
        {
            0x02, 0x35, 0x61,
            0x0F, 0x0E, 0x0F, 0x0E, 0x0F, 0x0E, 0x0F, 0x0E,
            0x0E, 0x0D, 0x0E, 0x0D, 0x0E, 0x0D, 0x0E, 0x0D,
            0x0D, 0x0C, 0x0D, 0x0C, 0x0D, 0x0C, 0x0D, 0x0C,
            0x0C, 0x0B, 0x0C, 0x0B, 0x0C, 0x0B, 0x0C, 0x0B,
            0x0B, 0x0A, 0x0B, 0x0A, 0x0B, 0x0A, 0x0B, 0x0A,
            0x0A, 0x09, 0x0A, 0x09, 0x0A, 0x09, 0x0A, 0x09,
            0x03, 0x54
        };

        const uint expectedCashboxRemovedCount = 0xFEFEFEFE;
        const uint expectedCashboxFullCount = 0xEDEDEDED;
        const uint expectedBillsStackedSinceCashboxRemoved = 0xDCDCDCDC;
        const uint expectedBillsStackedSincePowerUp = 0xCBCBCBCB;
        const uint expectedAverageTimeToStack = 0xBABABABA;
        const uint expectedTotalBillsStacked = 0xA9A9A9A9;

        var getCashboxMetricsResponseMessage = new GetCashboxMetricsResponseMessage(responsePayload);

        Assert.That(getCashboxMetricsResponseMessage.IsValid, Is.True);
        Assert.That(getCashboxMetricsResponseMessage.CashboxRemovedCount, Is.EqualTo(expectedCashboxRemovedCount));
        Assert.That(getCashboxMetricsResponseMessage.CashboxFullCount, Is.EqualTo(expectedCashboxFullCount));
        Assert.That(getCashboxMetricsResponseMessage.BillsStackedSinceCashboxRemoved,
            Is.EqualTo(expectedBillsStackedSinceCashboxRemoved));
        Assert.That(getCashboxMetricsResponseMessage.BillsStackedSincePowerUp,
            Is.EqualTo(expectedBillsStackedSincePowerUp));
        Assert.That(getCashboxMetricsResponseMessage.AverageTimeToStack, Is.EqualTo(expectedAverageTimeToStack));
        Assert.That(getCashboxMetricsResponseMessage.TotalBillsStacked, Is.EqualTo(expectedTotalBillsStacked));
    }

    [Test]
    public void GetUnitMetricsResponseMessage_DeserializesUnitMetrics()
    {
        var responsePayload = new byte[]
        {
            0x02, 0x45, 0x61,
            0x0F, 0x0E, 0x0F, 0x0E, 0x0F, 0x0E, 0x0F, 0x0E,
            0x0E, 0x0D, 0x0E, 0x0D, 0x0E, 0x0D, 0x0E, 0x0D,
            0x0D, 0x0C, 0x0D, 0x0C, 0x0D, 0x0C, 0x0D, 0x0C,
            0x0C, 0x0B, 0x0C, 0x0B, 0x0C, 0x0B, 0x0C, 0x0B,
            0x0B, 0x0A, 0x0B, 0x0A, 0x0B, 0x0A, 0x0B, 0x0A,
            0x0A, 0x09, 0x0A, 0x09, 0x0A, 0x09, 0x0A, 0x09,
            0x09, 0x08, 0x09, 0x08, 0x09, 0x08, 0x09, 0x08,
            0x08, 0x07, 0x08, 0x07, 0x08, 0x07, 0x08, 0x07,
            0x03, 0x24
        };

        const uint expectedTotalValueStacked = 0xFEFEFEFE;
        const uint expectedTotalDistanceMoved = 0xEDEDEDED;
        const uint expectedPowerUpCount = 0xDCDCDCDC;
        const uint expectedPushButtonCount = 0xCBCBCBCB;
        const uint expectedConfigurationCount = 0xBABABABA;
        const uint expectedUsbEnumerationsCount = 0xA9A9A9A9;
        const uint expectedTotalCheatAttemptsDetected = 0x98989898;
        const uint expectedTotalSecurityLockupCount = 0x87878787;

        var getUnitMetricsResponseMessage = new GetUnitMetricsResponseMessage(responsePayload);

        Assert.That(getUnitMetricsResponseMessage.IsValid, Is.True);
        Assert.That(getUnitMetricsResponseMessage.TotalValueStacked, Is.EqualTo(expectedTotalValueStacked));
        Assert.That(getUnitMetricsResponseMessage.TotalDistanceMoved, Is.EqualTo(expectedTotalDistanceMoved));
        Assert.That(getUnitMetricsResponseMessage.PowerUpCount, Is.EqualTo(expectedPowerUpCount));
        Assert.That(getUnitMetricsResponseMessage.PushButtonCount, Is.EqualTo(expectedPushButtonCount));
        Assert.That(getUnitMetricsResponseMessage.ConfigurationCount, Is.EqualTo(expectedConfigurationCount));
        Assert.That(getUnitMetricsResponseMessage.UsbEnumerationsCount, Is.EqualTo(expectedUsbEnumerationsCount));
        Assert.That(getUnitMetricsResponseMessage.TotalCheatAttemptsDetected,
            Is.EqualTo(expectedTotalCheatAttemptsDetected));
        Assert.That(getUnitMetricsResponseMessage.TotalSecurityLockupCount,
            Is.EqualTo(expectedTotalSecurityLockupCount));
    }

    [Test]
    public void GetServiceUsageCountersResponseMessage_DeserializesServiceUsageCounters()
    {
        var responsePayload = new byte[]
        {
            0x02, 0x35, 0x61,
            0x0F, 0x0E, 0x0F, 0x0E, 0x0F, 0x0E, 0x0F, 0x0E,
            0x0E, 0x0D, 0x0E, 0x0D, 0x0E, 0x0D, 0x0E, 0x0D,
            0x0D, 0x0C, 0x0D, 0x0C, 0x0D, 0x0C, 0x0D, 0x0C,
            0x0C, 0x0B, 0x0C, 0x0B, 0x0C, 0x0B, 0x0C, 0x0B,
            0x0B, 0x0A, 0x0B, 0x0A, 0x0B, 0x0A, 0x0B, 0x0A,
            0x0A, 0x09, 0x0A, 0x09, 0x0A, 0x09, 0x0A, 0x09,
            0x03, 0x54
        };

        const uint expectedDistancedMovedSinceLastTachSensorService = 0xFEFEFEFE;
        const uint expectedDistanceMovedSinceLastBillPathService = 0xEDEDEDED;
        const uint expectedDistancedMoveSinceLastBeltService = 0xDCDCDCDC;
        const uint expectedBillsStackedSinceLastCashboxService = 0xCBCBCBCB;
        const uint expectedDistanceMovedSinceLastMasService = 0xBABABABA;
        const uint expectedDistanceMovedSinceLastSpringRollerService = 0xA9A9A9A9;

        var getServiceUsageCountersResponseMessage = new GetServiceUsageCountersResponseMessage(responsePayload);

        Assert.That(getServiceUsageCountersResponseMessage.IsValid, Is.True);
        Assert.That(getServiceUsageCountersResponseMessage.DistancedMovedSinceLastTachSensorService,
            Is.EqualTo(expectedDistancedMovedSinceLastTachSensorService));
        Assert.That(getServiceUsageCountersResponseMessage.DistanceMovedSinceLastBillPathService,
            Is.EqualTo(expectedDistanceMovedSinceLastBillPathService));
        Assert.That(getServiceUsageCountersResponseMessage.DistancedMoveSinceLastBeltService,
            Is.EqualTo(expectedDistancedMoveSinceLastBeltService));
        Assert.That(getServiceUsageCountersResponseMessage.BillsStackedSinceLastCashboxService,
            Is.EqualTo(expectedBillsStackedSinceLastCashboxService));
        Assert.That(getServiceUsageCountersResponseMessage.DistanceMovedSinceLastMasService,
            Is.EqualTo(expectedDistanceMovedSinceLastMasService));
        Assert.That(getServiceUsageCountersResponseMessage.DistanceMovedSinceLastSpringRollerService,
            Is.EqualTo(expectedDistanceMovedSinceLastSpringRollerService));
    }

    [Test]
    public void GetServiceFlagsResponseMessage_DeserializesServiceFlags()
    {
        var responsePayload = new byte[]
        {
            0x02, 0x0B, 0x61,
            0b00000000, 0b00000001, 0b00000001, 0b00000010, 0b00000010, 0b00000011,
            0x03, 0x69
        };

        const GetServiceFlagsResponseMessage.ServiceSuggestor expectedTachSensorServiceSuggestor =
            GetServiceFlagsResponseMessage.ServiceSuggestor.None;
        const GetServiceFlagsResponseMessage.ServiceSuggestor expectedBillPathServiceSuggestor =
            GetServiceFlagsResponseMessage.ServiceSuggestor.UsageMetrics;
        const GetServiceFlagsResponseMessage.ServiceSuggestor expectedCashboxBeltServiceSuggestor =
            GetServiceFlagsResponseMessage.ServiceSuggestor.UsageMetrics;
        const GetServiceFlagsResponseMessage.ServiceSuggestor expectedCashboxMechanismServiceSuggestor =
            GetServiceFlagsResponseMessage.ServiceSuggestor.DiagnosticsAndError;
        const GetServiceFlagsResponseMessage.ServiceSuggestor expectedMasServiceSuggestor =
            GetServiceFlagsResponseMessage.ServiceSuggestor.DiagnosticsAndError;
        const GetServiceFlagsResponseMessage.ServiceSuggestor expectedSpringRollersServiceSuggestor =
            GetServiceFlagsResponseMessage.ServiceSuggestor.UsageMetrics
            | GetServiceFlagsResponseMessage.ServiceSuggestor.DiagnosticsAndError;

        var getServiceFlagsResponseMessage = new GetServiceFlagsResponseMessage(responsePayload);

        Assert.That(getServiceFlagsResponseMessage.IsValid, Is.True);
        Assert.That(getServiceFlagsResponseMessage.TachSensorServiceSuggestor,
            Is.EqualTo(expectedTachSensorServiceSuggestor));
        Assert.That(getServiceFlagsResponseMessage.BillPathServiceSuggestor,
            Is.EqualTo(expectedBillPathServiceSuggestor));
        Assert.That(getServiceFlagsResponseMessage.CashboxBeltServiceSuggestor,
            Is.EqualTo(expectedCashboxBeltServiceSuggestor));
        Assert.That(getServiceFlagsResponseMessage.CashboxMechanismServiceSuggestor,
            Is.EqualTo(expectedCashboxMechanismServiceSuggestor));
        Assert.That(getServiceFlagsResponseMessage.MasServiceSuggestor, Is.EqualTo(expectedMasServiceSuggestor));
        Assert.That(getServiceFlagsResponseMessage.SpringRollersServiceSuggestor,
            Is.EqualTo(expectedSpringRollersServiceSuggestor));
    }

    [Test]
    public void GetServiceInfoResponseMessage_DeserializesServiceInfo()
    {
        var responsePayload = new byte[]
        {
            0x02, 0x11, 0x61,
            0b11111111, 0b11111111, 0b11111111, 0b11111111,
            0b10111111, 0b10111111, 0b10111111, 0b10111111,
            0b10011111, 0b10011111, 0b10011111, 0b10011111,
            0x03, 0x70
        };

        byte[] expectedLastCustomerService = [0b01111111, 0b01111111, 0b01111111, 0b01111111];
        byte[] expectedLastServiceCenterService = [0b00111111, 0b00111111, 0b00111111, 0b00111111];
        byte[] expectedLastOemService = [0b00011111, 0b00011111, 0b00011111, 0b00011111];

        var getServiceInfoResponseMessage = new GetServiceInfoResponseMessage(responsePayload);

        Assert.That(getServiceInfoResponseMessage.IsValid, Is.True);
        Assert.That(getServiceInfoResponseMessage.LastCustomerService, Is.EqualTo(expectedLastCustomerService));
        Assert.That(getServiceInfoResponseMessage.LastServiceCenterService,
            Is.EqualTo(expectedLastServiceCenterService));
        Assert.That(getServiceInfoResponseMessage.LastOemService, Is.EqualTo(expectedLastOemService));
    }

    [Test]
    public void GetFirmwareMetricsResponseMessage_DeserializesFirmwareMetrics()
    {
        var responsePayload = new byte[]
        {
            0x02, 0x45, 0x61,
            0x0F, 0x0E, 0x0F, 0x0E, 0x0F, 0x0E, 0x0F, 0x0E,
            0x0E, 0x0D, 0x0E, 0x0D, 0x0E, 0x0D, 0x0E, 0x0D,
            0x0D, 0x0C, 0x0D, 0x0C, 0x0D, 0x0C, 0x0D, 0x0C,
            0x0C, 0x0B, 0x0C, 0x0B,
            0x0B, 0x0A, 0x0B, 0x0A,
            0x0A, 0x09, 0x0A, 0x09, 0x0A, 0x09, 0x0A, 0x09,
            0x09, 0x08, 0x09, 0x08, 0x09, 0x08, 0x09, 0x08,
            0x08, 0x07, 0x08, 0x07,
            0x07, 0x06, 0x07, 0x06,
            0x06, 0x05, 0x06, 0x05, 0x06, 0x05, 0x06, 0x05,
            0x03, 0x24
        };

        const uint expectedFlashUpdateCount = 0xFEFEFEFE;
        const uint expectedUsbFlashDriveFirmwareUpdateCount = 0xEDEDEDED;
        const uint expectedTotalFlashDriveInsertCount = 0xDCDCDCDC;
        const ushort expectedFirmwareCountryRevision = 0xCBCB;
        const ushort expectedFirmwareCoreRevision = 0xBABA;
        const uint expectedFirmwareBuildRevision = 0xA9A9A9A9;
        const uint expectedFirmwareCrc = 0x98989898;
        const ushort expectedBootloaderMajorRevision = 0x8787;
        const ushort expectedBootloaderMinorRevision = 0x7676;
        const uint expectedBootloaderBuildRevision = 0x65656565;

        var getFirmwareMetricsResponseMessage = new GetFirmwareMetricsResponseMessage(responsePayload);

        Assert.That(getFirmwareMetricsResponseMessage.IsValid, Is.True);
        Assert.That(getFirmwareMetricsResponseMessage.FlashUpdateCount, Is.EqualTo(expectedFlashUpdateCount));
        Assert.That(getFirmwareMetricsResponseMessage.UsbFlashDriveFirmwareUpdateCount,
            Is.EqualTo(expectedUsbFlashDriveFirmwareUpdateCount));
        Assert.That(getFirmwareMetricsResponseMessage.TotalFlashDriveInsertCount,
            Is.EqualTo(expectedTotalFlashDriveInsertCount));
        Assert.That(getFirmwareMetricsResponseMessage.FirmwareCountryRevision,
            Is.EqualTo(expectedFirmwareCountryRevision));
        Assert.That(getFirmwareMetricsResponseMessage.FirmwareCoreRevision, Is.EqualTo(expectedFirmwareCoreRevision));
        Assert.That(getFirmwareMetricsResponseMessage.FirmwareBuildRevision, Is.EqualTo(expectedFirmwareBuildRevision));
        Assert.That(getFirmwareMetricsResponseMessage.FirmwareCrc, Is.EqualTo(expectedFirmwareCrc));
        Assert.That(getFirmwareMetricsResponseMessage.BootloaderMajorRevision,
            Is.EqualTo(expectedBootloaderMajorRevision));
        Assert.That(getFirmwareMetricsResponseMessage.BootloaderMinorRevision,
            Is.EqualTo(expectedBootloaderMinorRevision));
        Assert.That(getFirmwareMetricsResponseMessage.BootloaderBuildRevision,
            Is.EqualTo(expectedBootloaderBuildRevision));
    }
}