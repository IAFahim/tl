internal static class Verification
{
    public static void Run()
    {
        foreach (var pattern in Enum.GetValues<SeekPattern>())
        {
            var sum = new SumBenchmarks { Pattern = pattern };
            sum.Setup();
            Console.WriteLine($"sum/{pattern}: typed={sum.TypedScalar()} dynamic={sum.DynamicScalar()}");

            var combat = new CombatBenchmarks { Pattern = pattern };
            combat.Setup();
            Console.WriteLine($"combat/{pattern}: typed={combat.TypedScalar()} dynamic={combat.DynamicScalar()}");
        }
    }
}
