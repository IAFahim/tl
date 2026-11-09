using Film;
using Tl.Unity;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

// Draws the film. It owns no timing: every label is read from the OnScreen column that Tl fills from the timeline.
public sealed class FilmUi : MonoBehaviour
{
    public UIDocument Document;
    public string[] SceneTitles;
    public string[] Captions;

    EntityQuery _query;
    Label _scene, _caption;
    VisualElement _phase, _beat;
    int _flashes;
    float _flashAt = -10f;

    void OnEnable()
    {
        var root = Document.rootVisualElement;
        _scene = root.Q<Label>("scene");
        _caption = root.Q<Label>("caption");
        _phase = root.Q("phase");
        _beat = root.Q("beat");
    }

    void LateUpdate()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || _scene == null) return;
        var entities = world.EntityManager;
        if (_query == default) _query = entities.CreateEntityQuery(ComponentType.ReadOnly<OnScreen>());
        if (_query.CalculateEntityCount() != 1) { if (Time.frameCount % 60 == 0) Debug.Log("FilmUi: OnScreen entities = " + _query.CalculateEntityCount()); return; }
        _query.CompleteDependency();             // Tl writes this column in a job; managed code must wait for it before reading
        var screen = _query.GetSingleton<OnScreen>();

        if (Time.frameCount % 60 == 0)
        {
            var pq = entities.CreateEntityQuery(ComponentType.ReadOnly<Playback>());
            var e = pq.CalculateEntityCount() > 0 ? pq.ToEntityArray(Unity.Collections.Allocator.Temp)[0] : Entity.Null;
            var pb = e != Entity.Null ? entities.GetBuffer<Playback>(e) : default;
            Debug.Log("FilmUi: set " + (e != Entity.Null && entities.HasBuffer<TlTimelineSet>(e) ? entities.GetBuffer<TlTimelineSet>(e).Length : -1) + " requests " + (e != Entity.Null && entities.HasBuffer<TimelineRequest>(e) ? entities.GetBuffer<TimelineRequest>(e).Length : -1) + " playing " + (e != Entity.Null && entities.HasComponent<TimelinePlaying>(e)) + " step " + (entities.CreateEntityQuery(ComponentType.ReadOnly<TimelineStep>()).CalculateEntityCount().ToString()));
            Debug.Log("FilmUi: playback entities " + pq.CalculateEntityCount() + (e != Entity.Null ? " len " + pb.Length + (pb.Length > 0 ? " timeline " + pb[0].Timeline + " tick " + pb[0].Tick + " flags " + pb[0].Flags : "") : ""));
        }
        if (Time.frameCount % 60 == 0) Debug.Log("FilmUi: scene " + screen.Scene + " caption " + screen.Caption + " phase " + screen.ScenePhase + " flashes " + screen.Flashes);
        _scene.text = screen.Scene > 0 && screen.Scene <= SceneTitles.Length ? SceneTitles[screen.Scene - 1] : "";
        _caption.text = screen.Caption > 0 && screen.Caption <= Captions.Length ? Captions[screen.Caption - 1] : "";
        _phase.style.width = Length.Percent(screen.ScenePhase * 100f);
        if (screen.Flashes != _flashes) { _flashes = screen.Flashes; _flashAt = Time.time; }
        _beat.style.opacity = Mathf.Clamp01(1f - (Time.time - _flashAt) * 4f);
    }
}
