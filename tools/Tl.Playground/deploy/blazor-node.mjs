import { dotnet } from './wwwroot/_framework/dotnet.js'

const { runMainAndExit } = await dotnet
    .withDiagnosticTracing(false)
    .withEnvironmentVariable('TL_PLAYGROUND_SMOKE', '1')
    .create();

await runMainAndExit();
