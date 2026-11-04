namespace JobRadar.Application.DTOs;

/// <summary>Estatística salarial de uma tecnologia, em base anual.</summary>
public record SalaryTechnologyDto(
    string Technology,
    decimal MedianAnnual,
    decimal P25Annual,
    decimal P75Annual,
    int Samples,
    string Currency
);

/// <summary>Quanto da base informa salário — transparência sobre a cobertura.</summary>
public record SalaryCoverageDto(int JobsWithSalary, int TotalJobs, decimal Percentage);

public record SalaryCurrencyOptionDto(string Currency, int Samples);

/// <summary>Resumo salarial por tecnologia para a moeda escolhida.</summary>
public record SalarySummaryDto(
    string Currency,
    SalaryCoverageDto Coverage,
    IReadOnlyList<SalaryTechnologyDto> Technologies,
    IReadOnlyList<SalaryCurrencyOptionDto> AvailableCurrencies,
    int MinimumSamples,
    string Disclaimer
);
