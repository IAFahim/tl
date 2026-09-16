# Deploy notes

The site is the published `Playground/wwwroot` pushed to the `gh-pages` branch root
(history-free force-push, `.nojekyll` included). The app's `index.html` uses a
location-independent `<base href="./" />`, so the same tree serves under `/tl/` on
GitHub Pages and from any static server locally.

`blazor-node.mjs` boots the *published* app's `Main` under Node with
`TL_PLAYGROUND_SMOKE=1`; `Program.cs` routes that environment variable to the SMOKE
receipt instead of the Blazor host. Copy it next to the publish output's `wwwroot`
before running (ES module imports resolve relative to the file's own path):

```sh
dotnet publish tools/Tl.Playground/Playground/Playground.csproj -c Release -o /tmp/pg116-publish
cp tools/Tl.Playground/deploy/blazor-node.mjs /tmp/pg116-publish/
node /tmp/pg116-publish/blazor-node.mjs
```

GitHub Pages does not serve the SDK's `.br` twins; the payload accounting in the issue
reports both the raw payload and the Brotli transfer estimate (the budget shape from #107).
