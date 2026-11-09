using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Film;
using UnityEngine;
using UnityEngine.UIElements;

// Draws the film with UI Toolkit. It owns no timing: every element, caption and number comes from the OnScreen column that Tl
// fills from the Timeline's clips. A Reveal clip's mask says which elements exist (they are instantiated and removed as the mask
// changes), a Sweep clip's value moves playheads and counters, a Footage clip picks the recorded frame.
public sealed class FilmUi : MonoBehaviour
{
    public UIDocument Document;
    public TextAsset DataJson;

    public static readonly string FootageDir = Environment.GetEnvironmentVariable("FILM_FOOTAGE_DIR") ?? "";
    public const int TicksPerVideoFrame = 2;     // Tl runs 60 ticks per second in Unity, the recorded footage plays at 30 frames per second

    FilmData _data;
    FilmPresenters.Presenter[] _scenes;
    VisualElement _beat, _playhead;
    Label _caption, _pill;
    float _tick = -1f, _flashAt = -10f;
    int _flashes;

    void OnEnable()
    {
        _data = JsonUtility.FromJson<FilmData>(DataJson.text);
        var root = Document.rootVisualElement;
        FilmPresenters.UseMono(root.Q<Label>("brand"));
        root.Q<Label>("tag").text = _data.title + "  ·  this film is a Tl timeline in Unity, drawn with UI Toolkit";
        _beat = root.Q("beat");
        _caption = root.Q<Label>("caption");
        _pill = root.Q<Label>("stepPill");
        var stage = root.Q("stage");
        _scenes = new FilmPresenters.Presenter[_data.scenes.Length];
        for (var i = 0; i < _scenes.Length; i++)
        {
            _scenes[i] = FilmPresenters.Make(_data, _data.scenes[i]);
            stage.Add(_scenes[i].Root);
            _scenes[i].Root.AddToClassList("is-hidden");
        }
        BuildLanes(root.Q("lanes"));
    }

    void BuildLanes(VisualElement lanes)
    {
        for (var i = 0; i < _data.lanes.Length; i++)
        {
            var lane = _data.lanes[i];
            var row = FilmPresenters.E("lane lane-" + lane.name.ToLowerInvariant(), lanes);
            row.style.top = i * 24;
            FilmPresenters.L(lane.name, "lane-label", row);
            var rail = FilmPresenters.E("lane-rail", row);
            for (var c = 0; c + 1 < lane.spans.Length; c += 2)
            {
                var clip = FilmPresenters.E("lane-clip", rail);
                clip.style.left = Length.Percent(lane.spans[c] * 100f / _data.ticks);
                clip.style.width = Length.Percent(Mathf.Max(0.12f, (lane.spans[c + 1] - lane.spans[c]) * 100f / _data.ticks));
            }
        }
        _playhead = FilmPresenters.E("playhead", lanes);
    }

    void Update()
    {
        if (_data == null || !FilmBridge.Has) return;
        var s = FilmBridge.Current;
        if (s.Scene <= 0 || s.Scene > _scenes.Length) return;
        _tick = _tick < 0f ? 0f : _tick + Time.deltaTime * 30f;

        var table = _data.sceneTable[s.Scene - 1];
        var local = _tick - table.start;
        var length = table.end - table.start;
        for (var i = 0; i < _scenes.Length; i++)
            _scenes[i].Root.EnableInClassList("is-hidden", i != s.Scene - 1);
        var presenter = _scenes[s.Scene - 1];
        presenter.Root.style.opacity = Mathf.Clamp01(local / 8f) * Mathf.Clamp01((length - local) / 8f + 0.5f);
        presenter.Paint(s, _tick);

        _caption.text = s.Caption > 0 && s.Caption <= _data.captions.Length ? _data.captions[s.Caption - 1] : "";
        var step = _data.scenes[s.Scene - 1].step;
        _pill.EnableInClassList("is-hidden", step == 0);
        if (step != 0) _pill.text = "STEP " + step + " / 6";
        if (s.Flashes != _flashes) { _flashes = s.Flashes; _flashAt = _tick; }
        _beat.style.opacity = Mathf.Clamp01(1f - (_tick - _flashAt) / 9f);
        _playhead.style.left = 140f + _tick / _data.ticks * 1540f;
    }
}

static class FilmPresenters
{
    public static VisualElement E(string classes, VisualElement parent = null)
    {
        var e = new VisualElement();
        foreach (var c in classes.Split(' '))
            if (c.Length > 0) e.AddToClassList(c);
        parent?.Add(e);
        return e;
    }

    public static Label L(string text, string classes, VisualElement parent = null)
    {
        var l = new Label(text);
        foreach (var c in classes.Split(' '))
            if (c.Length > 0) l.AddToClassList(c);
        parent?.Add(l);
        return l;
    }

    static Font _mono;
    static Font Mono => _mono != null ? _mono : _mono = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New" }, 24);
    public static void UseMono(VisualElement e) => e.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(Mono));

    // The elements of a scene exist only while their bit is set: a bit turning on instantiates the element, turning off removes it.
    sealed class RevealSet
    {
        readonly VisualElement _container;
        readonly Func<int, VisualElement> _make;
        readonly VisualElement[] _live = new VisualElement[32];

        public RevealSet(VisualElement container, Func<int, VisualElement> make) { _container = container; _make = make; }

        public VisualElement this[int bit] => _live[bit];

        public int Highest
        {
            get
            {
                for (var i = 31; i >= 0; i--)
                    if (_live[i] != null) return i;
                return -1;
            }
        }

        public void Sync(uint mask)
        {
            for (var i = 0; i < 32; i++)
            {
                var want = (mask >> i & 1) != 0;
                if (want && _live[i] == null)
                {
                    var index = 0;
                    for (var j = 0; j < i; j++)
                        if (_live[j] != null) index++;
                    _live[i] = _make(i);
                    _container.Insert(index, _live[i]);
                }
                else if (!want && _live[i] != null)
                {
                    _live[i].RemoveFromHierarchy();
                    _live[i] = null;
                }
            }
        }
    }

    sealed class Frames
    {
        readonly Texture2D _texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        string _name;
        int _index = -1;

        public Frames(Image target) { target.image = _texture; target.scaleMode = ScaleMode.ScaleAndCrop; }

        public void Show(string name, int index)
        {
            if (name == _name && index == _index) return;
            var path = Path.Combine(FilmUi.FootageDir, name, "f" + index.ToString("D4") + ".jpg");
            if (File.Exists(path)) _texture.LoadImage(File.ReadAllBytes(path));
            _name = name;
            _index = index;
        }
    }

    public abstract class Presenter
    {
        public VisualElement Root;
        public abstract void Paint(in OnScreen screen, float tick);
    }

    public static Presenter Make(FilmData data, FilmScene scene)
    {
        switch (scene.show)
        {
            case "title": return new Title(scene, false);
            case "outro": return new Title(scene, true);
            case "pipeline": return new Pipeline(scene);
            case "code": return new Code(data, scene);
            case "numberline": return new NumberLine(data, scene);
            case "stats": return new Stats(data, scene);
            case "inventory": return new Inventory(data, scene);
            case "footage": return new FootageScene(data, scene);
            case "terminal": return new Terminal(scene);
            default: throw new ArgumentException("unknown scene type " + scene.show);
        }
    }

    sealed class Title : Presenter
    {
        readonly RevealSet _big, _words;
        readonly string[] _wordText = { "clip", "track", "kind", "column" };

        public Title(FilmScene scene, bool outro)
        {
            Root = E("scene");
            var wrap = E("big-wrap", Root);
            var row = E("word-row", wrap);
            _big = new RevealSet(wrap, bit =>
            {
                if (bit != 0) return L(scene.sub, "big-sub");
                var title = L(scene.title, "big-title");
                UseMono(title);
                return title;
            });
            _words = new RevealSet(row, bit => L(_wordText[bit], "word"));
        }

        public override void Paint(in OnScreen s, float tick)
        {
            _big.Sync(s.Reveal & 3u);
            _words.Sync(s.Reveal >> 2 & 15u);
        }
    }

    sealed class Pipeline : Presenter
    {
        static readonly string[][] Parts =
        {
            new[] { "Clip", "the designer's numbers", "TeleportClip" },
            new[] { "Track", "turns a clip into values", "TeleportTrack" },
            new[] { "Kind", "combines the values", "TeleportKind" },
            new[] { "Column", "stores the result", "Teleported" },
            new[] { "System", "your code reads it", "your ISystem" },
        };

        readonly RevealSet _boxes;

        public Pipeline(FilmScene scene)
        {
            Root = E("scene");
            L(scene.title, "scene-title", Root);
            var pipe = E("pipe", Root);
            _boxes = new RevealSet(pipe, bit =>
            {
                var item = E("pipe-item");
                if (bit > 0) L("→", "pipe-arrow", item);
                var box = E("pipe-box", item);
                L(Parts[bit][0], "pipe-name", box);
                L(Parts[bit][1], "pipe-desc", box);
                L(Parts[bit][2], "pipe-code", box);
                return item;
            });
        }

        public override void Paint(in OnScreen s, float tick)
        {
            _boxes.Sync(s.Reveal);
            var now = _boxes.Highest;
            for (var i = 0; i < 5; i++)
                _boxes[i]?.Q(className: "pipe-box")?.EnableInClassList("is-now", i == now);
        }
    }

    sealed class Code : Presenter
    {
        static readonly string[] Names = { "clip", "track", "kind", "column" };
        readonly List<VisualElement>[] _blocks = { new List<VisualElement>(), new List<VisualElement>(), new List<VisualElement>(), new List<VisualElement>() };
        readonly Label _name, _text;

        public Code(FilmData data, FilmScene scene)
        {
            Root = E("scene");
            var wrap = E("code-wrap", Root);
            var panel = E("panel code-panel", wrap);
            UseMono(panel);
            var explain = E("explain", wrap);
            _name = L("", "explain-name", explain);
            _text = L("", "explain-text", explain);

            // the real Teleport.cs: without the using and namespace lines, dedented, split into its four blocks
            var lines = new List<string>();
            foreach (var raw in data.helloCode.Replace("\r", "").Split('\n'))
                if (!Regex.IsMatch(raw, @"^\s*using |^\s*namespace |^\s*[{}]\s*$") || Regex.IsMatch(raw, @"^\s{4,}[{}]"))
                    lines.Add(raw.StartsWith("    ") ? raw.Substring(4) : raw);
            var block = -1;
            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim().Length == 0)
                {
                    var next = lines.GetRange(i + 1, lines.Count - i - 1).Find(l => l.Trim().Length > 0);
                    if (next != null && !char.IsWhiteSpace(next[0]) && block >= 0 && block < 3) { block++; E("code-gap", panel); }
                    continue;
                }
                if (block < 0) block = 0;
                // UI Toolkit collapses leading spaces, so the indentation becomes non-breaking spaces
                var indent = lines[i].Length - lines[i].TrimStart().Length;
                var line = L(new string(' ', indent) + lines[i].TrimStart(), "code-line", panel);
                _blocks[block].Add(line);
            }
        }

        public override void Paint(in OnScreen s, float tick)
        {
            for (var b = 0; b < 4; b++)
                foreach (var line in _blocks[b])
                    line.EnableInClassList("is-lit", (s.Reveal >> b & 1) != 0);
            switch (s.Reveal)
            {
                case 15: _name.text = "One file"; _text.text = "The generator writes everything else."; break;
                case 12: _name.text = "Kind and column"; _text.text = "The kind adds every jump to the column where the result lives."; break;
                case 1: _name.text = "Clip"; _text.text = "The numbers the designer types in the Inspector: Distance and ComeBack."; break;
                case 2: _name.text = "Track"; _text.text = "Turns a clip into values: a jump when the clip starts, a return when it ends."; break;
                case 4: _name.text = "Kind"; _text.text = "Takes each jump and adds it to the column."; break;
                case 8: _name.text = "Column"; _text.text = "Where the result lives: the player's Offset."; break;
                default: _name.text = ""; _text.text = ""; break;
            }
        }
    }

    sealed class NumberLine : Presenter
    {
        const float Max = 150f, RailWidth = 1680f - 150f - 40f;
        readonly FilmData _data;
        readonly VisualElement _head, _console;
        readonly Label _x, _tick;
        readonly Frames _frames;
        readonly Label[] _lines;

        static float Left(float t) => 150f + t / Max * RailWidth;

        public NumberLine(FilmData data, FilmScene scene)
        {
            _data = data;
            Root = E("scene");
            L(scene.title, "scene-title", Root);
            var lane = E("nl-lane", Root);
            L("Teleport", "nl-label", lane);
            E("nl-rail", lane);
            foreach (var clip in new[] { (30f, 60f, "+5"), (90f, 120f, "+8, back") })
            {
                var c = L(clip.Item3, "nl-clip", lane);
                c.style.left = Left(clip.Item1);
                c.style.width = Left(clip.Item2) - Left(clip.Item1);
            }
            _head = E("nl-head", lane);
            var shot = new Image();
            shot.AddToClassList("nl-shot");
            Root.Add(shot);
            _frames = new Frames(shot);
            _x = L("", "nl-x", Root);
            _tick = L("", "nl-tick", Root);
            _console = E("panel nl-console", Root);
            UseMono(_console);
            L("recorded from Unity", "nl-tag", Root);
            _lines = new Label[data.helloSteps.Length];
            for (var i = 0; i < _lines.Length; i++)
                _lines[i] = L("Hello Tl: the player is at x = " + data.helloValues[i] + " (timeline tick " + data.helloSteps[i] + ")", "nl-line is-hidden", _console);
        }

        public override void Paint(in OnScreen s, float tick)
        {
            var t = Mathf.Floor(s.Sweep);
            _head.style.left = Left(t);
            float x = 0;
            for (var i = 0; i < _data.helloSteps.Length; i++)
                if (_data.helloSteps[i] <= t) x = _data.helloValues[i];
            _x.text = "x = " + x;
            _tick.text = "timeline tick " + (int)t;
            _frames.Show("hello", Mathf.Clamp(Mathf.RoundToInt(t / 2f), 0, 74));
            for (var i = 0; i < _lines.Length; i++)
                _lines[i].EnableInClassList("is-hidden", _data.helloSteps[i] > t);
        }
    }

    sealed class Stats : Presenter
    {
        const float RailWidth = 800f - 150f - 20f;
        readonly FilmStats[] _rows;
        readonly Label _health, _attack, _step;
        readonly VisualElement _fill, _head;

        static float X(float t) => 150f + t / 600f * RailWidth;

        static VisualElement Lane(VisualElement parent, string name)
        {
            var lane = E("st-lane", parent);
            L(name, "st-lane-name", lane);
            E("st-rail", lane);
            return lane;
        }

        static void Place(VisualElement e, float from, float to = -1f)
        {
            e.style.left = X(from);
            if (to >= 0f) e.style.width = X(to) - X(from);
        }

        public Stats(FilmData data, FilmScene scene)
        {
            _rows = data.stats;
            Root = E("scene");
            L(scene.title, "scene-title", Root);
            var left = E("st-left", Root);
            UseMono(left);
            L("hero Health", "st-name", left);
            _health = L("", "st-big", left);
            _fill = E("st-fill", E("st-meter", left));
            L("hero Attack (base + bonus)", "st-name", left);
            _attack = L("", "st-big st-attack", left);
            var right = E("st-right", Root);
            UseMono(right);
            var duel = Lane(right, "Duel");
            foreach (var hit in new[] { 60f, 120f, 180f }) Place(E("st-marker", duel), hit);
            Place(E("st-poison", duel), 240f, 480f);
            Place(E("st-heal", duel), 500f);
            var haste = Lane(right, "Haste");
            Place(E("st-haste", haste), 100f, 250f);
            Place(E("st-haste", haste), 250f, 550f);
            _head = E("st-head", right);
            L("red: hits (-12) and poison (-1 every 20 ticks)   green: heal (+20)   amber: haste (+10 attack)", "st-legend", right);
            _step = L("", "st-step", right);
        }

        public override void Paint(in OnScreen s, float tick)
        {
            var step = Mathf.RoundToInt(s.Sweep);
            var row = _rows[Mathf.Min(_rows.Length - 1, step / 25)];
            _health.text = row.health + " / " + row.max;
            _fill.style.width = Length.Percent(Mathf.Clamp01(row.health / (float)row.max) * 100f);
            _fill.EnableInClassList("is-low", row.health < 60);
            _attack.text = row.attack.ToString();
            _head.style.left = X(step);
            _step.text = "step " + step;
        }
    }

    sealed class Inventory : Presenter
    {
        static readonly string[] Names = { "Wood", "Stone", "Plank", "Potion", "Sword", "Coin", "Key", "Gem" };
        const float RailWidth = 1680f - 150f - 20f;
        readonly FilmBag[] _rows;
        readonly Label _dir, _step;
        readonly Label[] _counts = new Label[8];
        readonly VisualElement[] _tiles = new VisualElement[8];
        readonly VisualElement _head;

        static float X(float step) => 150f + step / 1500f * RailWidth;

        static int Count(FilmBag bag, int i)
        {
            switch (i)
            {
                case 0: return bag.Wood;
                case 1: return bag.Stone;
                case 2: return bag.Plank;
                case 3: return bag.Potion;
                case 4: return bag.Sword;
                case 5: return bag.Coin;
                case 6: return bag.Key;
                default: return bag.Gem;
            }
        }

        public Inventory(FilmData data, FilmScene scene)
        {
            _rows = data.inventory;
            Root = E("scene");
            L(scene.title, "scene-title", Root);
            var info = E("inv-info", Root);
            _dir = L("", "inv-dir", info);
            _step = L("", "inv-step", info);
            UseMono(Root);
            var grid = E("inv-grid", Root);
            for (var i = 0; i < 8; i++)
            {
                _tiles[i] = E("inv-tile", grid);
                _counts[i] = L("0", "inv-count", _tiles[i]);
                L(Names[i], "inv-name", _tiles[i]);
            }
            var lane = E("inv-lane", Root);
            L("Timeline", "st-lane-name", lane);
            E("st-rail", lane);
            foreach (var seg in new[] { (0f, 450f, "play", "is-play"), (450f, 900f, "rewind", "is-rewind"), (900f, 1500f, "play again", "is-play") })
            {
                var e = L(seg.Item3, "inv-seg " + seg.Item4, lane);
                e.style.left = X(seg.Item1);
                e.style.width = X(seg.Item2) - X(seg.Item1);
            }
            _head = E("st-head", lane);
        }

        public override void Paint(in OnScreen s, float tick)
        {
            var step = Mathf.RoundToInt(s.Sweep);
            var index = Mathf.Min(_rows.Length - 1, step / 25);
            var row = _rows[index];
            var before = _rows[Mathf.Max(0, index - 1)];
            var back = step > 450 && step <= 900;
            _dir.text = back ? "<< rewinding" : ">> playing";
            _dir.EnableInClassList("is-back", back);
            _dir.EnableInClassList("is-forward", !back);
            var timelineTick = step <= 450 ? step : step <= 900 ? 900 - step : step - 900;
            _step.text = "step " + step + "   timeline tick " + timelineTick;
            for (var i = 0; i < 8; i++)
            {
                _counts[i].text = Count(row, i).ToString();
                _tiles[i].EnableInClassList("is-up", Count(row, i) > Count(before, i));
                _tiles[i].EnableInClassList("is-down", Count(row, i) < Count(before, i));
            }
            _head.style.left = X(step);
        }
    }

    sealed class FootageScene : Presenter
    {
        readonly FilmFootage _rec;
        readonly FilmScene _scene;
        readonly Frames _frames;
        readonly Label[] _values;
        readonly string[] _axes;
        readonly RevealSet _stat;

        public FootageScene(FilmData data, FilmScene scene)
        {
            _scene = scene;
            _rec = data.FootageOf(scene.footage);
            Root = E("scene");
            L(scene.title, "scene-title", Root);
            var shot = new Image();
            shot.AddToClassList("fo-shot");
            Root.Add(shot);
            _frames = new Frames(shot);
            var side = E("fo-side", Root);
            UseMono(side);
            L("recorded from Unity, " + _rec.frames + " frames per lap", "fo-tag", side);
            _axes = scene.readout;
            _values = new Label[_axes.Length];
            for (var i = 0; i < _axes.Length; i++)
            {
                var row = E("fo-axis", side);
                L(_axes[i], "fo-axis-name", row);
                _values[i] = L("", "fo-value", row);
            }
            _stat = new RevealSet(E("", side), bit =>
            {
                var card = E("panel fo-stat");
                L(scene.statBig, "fo-stat-big", card);
                L(scene.statSub, "fo-stat-sub", card);
                return card;
            });
        }

        public override void Paint(in OnScreen s, float tick)
        {
            var f = Mathf.Max(0, s.FootageTick) / FilmUi.TicksPerVideoFrame % _rec.frames;
            _frames.Show(_scene.footage, f);
            for (var i = 0; i < _axes.Length; i++)
            {
                var v = _axes[i] == "x" ? _rec.x[f] : _axes[i] == "y" ? _rec.y[f] : _rec.z[f];
                _values[i].text = v.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);
            }
            _stat.Sync(s.Reveal & 1u);
        }
    }

    sealed class Terminal : Presenter
    {
        readonly RevealSet _lines;

        public Terminal(FilmScene scene)
        {
            Root = E("scene");
            L(scene.title, "scene-title", Root);
            var panel = E("panel term-panel", Root);
            UseMono(panel);
            L("● ● ●", "term-dots", panel);
            _lines = new RevealSet(panel, bit =>
            {
                var text = scene.lines[bit].text;
                var line = L(text, "term-line");
                if (text.StartsWith("ok ")) line.AddToClassList("is-ok");
                else if (text.StartsWith("$") || text.StartsWith(">") || text.StartsWith("irm")) line.AddToClassList("is-command");
                return line;
            });
        }

        public override void Paint(in OnScreen s, float tick) => _lines.Sync(s.Reveal);
    }
}
