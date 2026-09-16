if (args is ["--verify"])
{
    Verification.Run();
    return 0;
}

Console.Error.WriteLine("usage: Alpha --verify");
return 1;
