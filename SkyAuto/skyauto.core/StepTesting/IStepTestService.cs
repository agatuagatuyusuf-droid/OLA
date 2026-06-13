namespace SkyAuto.Core.StepTesting;

public interface IStepTestService
{
    Task<StepTestResult> TestStepAsync(StepTestRequest request);
}
