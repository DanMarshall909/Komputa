namespace Komputa.Domain.ValueObjects;

/// <summary>
/// Value object representing memory importance with decay calculations
/// </summary>
public record MemoryScore
{
    public double Value { get; }

    private MemoryScore(double value)
    {
        if (value < 0 || value > 1.0)
            throw new ArgumentException("Memory score must be between 0 and 1.0", nameof(value));
            
        Value = value;
    }

    public static MemoryScore Create(double value) => new(value);

    /// <summary>
    /// Calculate time-based decay for the memory score
    /// </summary>
    public MemoryScore ApplyTimeDecay(DateTime timestamp, DateTime now)
    {
        var ageInDays = (now - timestamp).TotalDays;
        
        // Apply exponential decay: score = originalScore * e^(-decayRate * age)
        // Decay rate of 0.1 means 10% reduction per day
        const double decayRate = 0.1;
        var decayFactor = Math.Exp(-decayRate * ageInDays);
        
        var decayedValue = Value * decayFactor;
        return new MemoryScore(Math.Max(decayedValue, 0.0));
    }

    /// <summary>
    /// Boost score for recent usage
    /// </summary>
    public MemoryScore ApplyUsageBoost(double boostFactor = 0.1)
    {
        if (boostFactor < 0 || boostFactor > 1.0)
            throw new ArgumentException("Boost factor must be between 0 and 1.0", nameof(boostFactor));
            
        var boostedValue = Math.Min(Value + boostFactor, 1.0);
        return new MemoryScore(boostedValue);
    }

    /// <summary>
    /// Combine with keyword importance boost
    /// </summary>
    public MemoryScore ApplyKeywordBoost(double keywordBoost = 0.2)
    {
        if (keywordBoost < 0)
            throw new ArgumentException("Keyword boost cannot be negative", nameof(keywordBoost));
            
        var boostedValue = Math.Min(Value + keywordBoost, 1.0);
        return new MemoryScore(boostedValue);
    }

    public static implicit operator double(MemoryScore score) => score.Value;
    
    public override string ToString() => Value.ToString("F2");
}
