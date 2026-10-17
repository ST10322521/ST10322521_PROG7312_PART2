using SmartX.Shared.Enums;

namespace SmartX.Shared.Models;

public class TelemetryBatch
{
    private readonly double[,]? _rectangularValues;
    private readonly double[][]? _jaggedValues;
    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryBatch"/> class with a rectangular array of values.
    /// </summary>
    /// <param name="sensorCount"></param>
    /// <param name="readingsPerSensor"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public TelemetryBatch(int sensorCount, int readingsPerSensor)
    {
        if (sensorCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sensorCount));
        }

        if (readingsPerSensor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(readingsPerSensor));
        }

        _rectangularValues = new double[sensorCount, readingsPerSensor];
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryBatch"/> class with a jagged array of values.
    /// </summary>
    /// <param name="readingsPerSensor"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public TelemetryBatch(int[] readingsPerSensor)
    {
        if (readingsPerSensor is null || readingsPerSensor.Length == 0)
        {
            throw new ArgumentException("At least one sensor must be provided.", nameof(readingsPerSensor));
        }

        if (readingsPerSensor.Any(count => count <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(readingsPerSensor), "Each sensor must have at least one reading.");
        }

        _jaggedValues = new double[readingsPerSensor.Length][];

        for (var sensorIndex = 0; sensorIndex < readingsPerSensor.Length; sensorIndex++)
        {
            _jaggedValues[sensorIndex] = new double[readingsPerSensor[sensorIndex]];
        }
    }
    /// <summary>
    /// Gets the number of sensors in the batch. For rectangular arrays, this is the first dimension length; for jagged arrays, this is the length of the outer array.
    /// </summary>
    public int SensorCount
    {
        get
        {
            if (_rectangularValues is not null)
            {
                return _rectangularValues.GetLength(0);
            }

            return _jaggedValues!.Length;
        }
    }
    /// <summary>
    /// Gets the number of readings per sensor. This property is only valid for rectangular arrays; for jagged arrays, use <see cref="GetJaggedReadingCount(int)"/> instead.
    /// </summary>
    public int ReadingsPerSensor
    {
        get
        {
            if (_rectangularValues is null)
            {
                throw new InvalidOperationException("This batch uses a jagged array with variable reading counts.");
            }

            return _rectangularValues.GetLength(1);
        }
    }
    /// <summary>
    /// Gets the number of readings for a specific sensor in a jagged array. This method is only valid for jagged arrays; for rectangular arrays, use <see cref="ReadingsPerSensor"/> instead.
    /// </summary>
    /// <param name="sensorIndex"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public int GetJaggedReadingCount(int sensorIndex)
    {
        if (_jaggedValues is null)
        {
            throw new InvalidOperationException("This batch does not use a jagged array.");
        }

        return _jaggedValues[sensorIndex].Length;
    }
    /// <summary>
    /// Sets the value for a specific sensor and reading in a rectangular array. This method is only valid for rectangular arrays; for jagged arrays, use <see cref="SetJaggedValue(int, int, double)"/> instead.
    /// </summary>
    /// <param name="sensorIndex"></param>
    /// <param name="readingIndex"></param>
    /// <param name="value"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void SetRectangularValue(
        int sensorIndex,
        int readingIndex,
        double value)
    {
        if (_rectangularValues is null)
        {
            throw new InvalidOperationException("This batch does not use a rectangular array.");
        }

        _rectangularValues[sensorIndex, readingIndex] = value;
    }
    /// <summary>
    /// Gets the value for a specific sensor and reading in a rectangular array. This method is only valid for rectangular arrays; for jagged arrays, use <see cref="GetJaggedValue(int, int)"/> instead.
    /// </summary>
    /// <param name="sensorIndex"></param>
    /// <param name="readingIndex"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public double GetRectangularValue(int sensorIndex, int readingIndex)
    {
        if (_rectangularValues is null)
        {
            throw new InvalidOperationException("This batch does not use a rectangular array.");
        }

        return _rectangularValues[sensorIndex, readingIndex];
    }
    /// <summary>
    /// Sets the value for a specific sensor and reading in a jagged array. This method is only valid for jagged arrays; for rectangular arrays, use <see cref="SetRectangularValue(int, int, double)"/> instead.
    /// </summary>
    /// <param name="sensorIndex"></param>
    /// <param name="readingIndex"></param>
    /// <param name="value"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void SetJaggedValue(
        int sensorIndex,
        int readingIndex,
        double value)
    {
        if (_jaggedValues is null)
        {
            throw new InvalidOperationException("This batch does not use a jagged array.");
        }

        _jaggedValues[sensorIndex][readingIndex] = value;
    }
    /// <summary>
    /// Gets the value for a specific sensor and reading in a jagged array. This method is only valid for jagged arrays; for rectangular arrays, use <see cref="GetRectangularValue(int, int)"/> instead.
    /// </summary>
    /// <param name="sensorIndex"></param>
    /// <param name="readingIndex"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public double GetJaggedValue(int sensorIndex, int readingIndex)
    {
        if (_jaggedValues is null)
        {
            throw new InvalidOperationException("This batch does not use a jagged array.");
        }

        return _jaggedValues[sensorIndex][readingIndex];
    }
    /// <summary>
    /// Converts the telemetry batch into a list of <see cref="SensorReading"/> instances, using the provided device IDs, telemetry type, and timestamp. The number of device IDs must match the number of sensors in the batch.
    /// </summary>
    /// <param name="deviceIds"></param>
    /// <param name="telemetryType"></param>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public List<SensorReading> ToReadings(
        IReadOnlyList<string> deviceIds,
        TelemetryType telemetryType,
        DateTime timestamp)
    {
        if (deviceIds.Count != SensorCount)
        {
            throw new ArgumentException("Device IDs count must match the number of sensors.", nameof(deviceIds));
        }

        if (_rectangularValues is not null)
        {
            return ConvertRectangularToReadings(
                deviceIds,
                telemetryType,
                timestamp);
        }

        return ConvertJaggedToReadings(
            deviceIds,
            telemetryType,
            timestamp);
    }
    /// <summary>
    /// Converts the rectangular array of values into a list of <see cref="SensorReading"/> instances, using the provided device IDs, telemetry type, and timestamp.
    /// </summary>
    /// <param name="deviceIds"></param>
    /// <param name="telemetryType"></param>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    private List<SensorReading> ConvertRectangularToReadings(
        IReadOnlyList<string> deviceIds,
        TelemetryType telemetryType,
        DateTime timestamp)
    {
        var readings = new List<SensorReading>(SensorCount * ReadingsPerSensor);

        for (var sensorIndex = 0; sensorIndex < SensorCount; sensorIndex++)
        {
            for (var readingIndex = 0; readingIndex < ReadingsPerSensor; readingIndex++)
            {
                readings.Add(new SensorReading(
                    deviceIds[sensorIndex],
                    timestamp,
                    telemetryType,
                    _rectangularValues![sensorIndex, readingIndex]));
            }
        }

        return readings;
    }
    /// <summary>
    /// Converts the jagged array of values into a list of <see cref="SensorReading"/> instances, using the provided device IDs, telemetry type, and timestamp.
    /// </summary>
    /// <param name="deviceIds"></param>
    /// <param name="telemetryType"></param>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private List<SensorReading> ConvertJaggedToReadings(
        IReadOnlyList<string> deviceIds,
        TelemetryType telemetryType,
        DateTime timestamp)
    {
        var jaggedValues = _jaggedValues ?? throw new InvalidOperationException("This batch does not use a jagged array.");

        var totalReadingCount = jaggedValues.Sum(values => values.Length);
        var readings = new List<SensorReading>(totalReadingCount);

        for (var sensorIndex = 0; sensorIndex < jaggedValues.Length; sensorIndex++)
        {
            for (var readingIndex = 0; readingIndex < jaggedValues[sensorIndex].Length; readingIndex++)
            {
                readings.Add(new SensorReading(
                    deviceIds[sensorIndex],
                    timestamp,
                    telemetryType,
                    jaggedValues[sensorIndex][readingIndex]));
            }
        }

        return readings;
    }
}