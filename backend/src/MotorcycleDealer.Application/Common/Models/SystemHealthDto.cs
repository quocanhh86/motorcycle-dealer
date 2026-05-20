namespace MotorcycleDealer.Application.Common.Models;

public sealed record SystemHealthDto(
    string Status,
    string Service,
    string Version,
    string Environment,
    string Database,
    DateTimeOffset TimestampUtc,
    string TraceId);
