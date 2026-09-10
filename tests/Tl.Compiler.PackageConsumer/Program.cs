using Tl.Compiler;

var plan = new TimelinePlan(
    "package",
    7,
    false,
    [new TrackPlan(3, 11, new OperationId("sum"))],
    [new ClipPlan(3, 13, 2, 9)]);
var validated = plan.Validate();
if (validated.FormatVersion != TimelinePlan.CurrentFormatVersion
    || validated.Duration != 9
    || validated.Regions.Length != 2
    || validated.Regions[1].Works.Length != 1)
    return 1;

Console.WriteLine($"{validated.FormatVersion}:{validated.Duration}:{validated.Regions.Length}");
return 0;
