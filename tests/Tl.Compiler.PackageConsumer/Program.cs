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

var ordered = new OrderedTimelinePlan(
    "ordered-package",
    false,
    [new(new("sum"), [])],
    [new(new(1), new("i32"), [1, 0, 0, 0]), new(new(2), new("i32"), [2, 0, 0, 0])],
    [new(0, new(1), new("sum"))],
    [new(0, new(2), 0, 1)],
    []).Validate();
if (ordered.FormatVersion != OrderedTimelinePlan.CurrentFormatVersion
    || ordered.Duration != 1
    || ordered.Regions.Length != 1
    || ordered.Occurrences.Length != 1
    || ordered.PayloadStorageIndices.Length != 2)
    return 2;

Console.WriteLine($"{validated.FormatVersion}:{validated.Duration}:{validated.Regions.Length}");
return 0;
