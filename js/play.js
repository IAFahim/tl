window.play = {
    syncQuery: function (query) {
        try { history.replaceState(null, '', location.pathname + '?' + query + location.hash); } catch (e) { }
    },
    measureHeight: function () {
        var height = Math.max(document.documentElement.scrollHeight, document.body.scrollHeight);
        try { window.parent.postMessage({ type: 'setHeight', height: height }, '*'); } catch (e) { }
        return height;
    },
    wireSplitters: function () {
        var splitters = document.querySelectorAll('.splitter');
        for (var i = 0; i < splitters.length; i++) wire(splitters[i]);

        function wire(el) {
            if (el.dataset.wired) return;
            var container = el.closest('.split-grid');
            if (!container) return;
            el.dataset.wired = '1';
            var vertical = el.classList.contains('splitter-y');
            var persistKey = container.dataset.persist || '';
            var before = toNumber(el.getAttribute('data-before'), 0);
            var after = toNumber(el.getAttribute('data-after'), 2);
            var minBefore = toFloat(el.getAttribute('data-min-before'), 200);
            var minAfter = toFloat(el.getAttribute('data-min-after'), 200);
            var start = 0, base = null;
            restore(container, vertical, persistKey);

            el.addEventListener('pointerdown', function (e) {
                base = read();
                if (!base) return;
                start = vertical ? e.clientY : e.clientX;
                el.setPointerCapture(e.pointerId);
                el.classList.add('dragging');
                document.body.classList.add(vertical ? 'splitting-rows' : 'splitting-cols');
                e.preventDefault();
            });
            el.addEventListener('pointermove', function (e) {
                if (!el.classList.contains('dragging') || !base) return;
                apply(base, (vertical ? e.clientY : e.clientX) - start);
            });
            el.addEventListener('pointerup', function (e) {
                if (!el.classList.contains('dragging')) return;
                end(e);
                persist(container, vertical, persistKey);
            });
            el.addEventListener('pointercancel', function (e) {
                if (!el.classList.contains('dragging')) return;
                end(e);
                persist(container, vertical, persistKey);
            });
            el.addEventListener('dblclick', function () {
                reset(container, vertical, persistKey);
            });
            el.addEventListener('keydown', function (e) {
                if (e.key === 'Home') {
                    reset(container, vertical, persistKey);
                    e.preventDefault();
                    return;
                }
                var delta = e.key === 'ArrowLeft' || e.key === 'ArrowUp' ? -24
                    : e.key === 'ArrowRight' || e.key === 'ArrowDown' ? 24 : 0;
                if (!delta) return;
                apply(read(), delta);
                persist(container, vertical, persistKey);
                e.preventDefault();
            });

            function toNumber(text, fallback) {
                var parsed = parseInt(text, 10);
                return isNaN(parsed) ? fallback : parsed;
            }
            function toFloat(text, fallback) {
                var parsed = parseFloat(text);
                return isNaN(parsed) ? fallback : parsed;
            }
            function read() {
                var computed = getComputedStyle(container);
                var parts = (vertical ? computed.gridTemplateRows : computed.gridTemplateColumns).split(' ');
                if (parts.length <= Math.max(before, after)) return null;
                var sizes = [];
                for (var i = 0; i < parts.length; i++) {
                    var size = parseFloat(parts[i]);
                    if (isNaN(size)) return null;
                    sizes.push(size);
                }
                return sizes;
            }
            function apply(sizes, delta) {
                if (!sizes) return;
                var total = sizes[before] + sizes[after];
                var size = Math.min(Math.max(sizes[before] + delta, minBefore), total - minAfter);
                var values = sizes.slice();
                values[before] = size;
                values[after] = total - size;
                var template = text(values);
                if (vertical) container.style.gridTemplateRows = template;
                else container.style.gridTemplateColumns = template;
            }
            function text(values) {
                var out = [];
                for (var i = 0; i < values.length; i++) out.push(values[i] + 'px');
                return out.join(' ');
            }
            function end(e) {
                if (!el.classList.contains('dragging')) return;
                el.classList.remove('dragging');
                document.body.classList.remove('splitting-cols', 'splitting-rows');
                if (e && el.hasPointerCapture(e.pointerId)) el.releasePointerCapture(e.pointerId);
            }
        }

        function persist(container, vertical, key) {
            if (!key) return;
            var computed = getComputedStyle(container);
            var parts = (vertical ? computed.gridTemplateRows : computed.gridTemplateColumns).split(' ');
            var total = vertical ? container.clientHeight : container.clientWidth;
            if (parts.length < 2 || !(total > 0)) return;
            var fractions = [];
            for (var i = 0; i < parts.length; i++) {
                var size = parseFloat(parts[i]);
                if (isNaN(size)) return;
                fractions.push(size / total);
            }
            try { localStorage.setItem(key, JSON.stringify(fractions)); } catch (e) { }
        }
        function restore(container, vertical, key) {
            if (!key || container.dataset.restored) return;
            container.dataset.restored = '1';
            var raw;
            try { raw = localStorage.getItem(key); } catch (e) { return; }
            if (!raw) return;
            var fractions;
            try { fractions = JSON.parse(raw); } catch (e) { return; }
            if (!Array.isArray(fractions) || fractions.length < 2) return;
            var total = vertical ? container.clientHeight : container.clientWidth;
            if (!(total > 0)) return;
            var values = [];
            for (var i = 0; i < fractions.length; i++) {
                var size = Number(fractions[i]) * total;
                if (isNaN(size)) return;
                values.push(size);
            }
            var template = text(values);
            if (vertical) container.style.gridTemplateRows = template;
            else container.style.gridTemplateColumns = template;
        }
        function reset(container, vertical, key) {
            if (vertical) container.style.gridTemplateRows = '';
            else container.style.gridTemplateColumns = '';
            if (!key) return;
            try { localStorage.removeItem(key); } catch (e) { }
        }
        function text(values) {
            var out = [];
            for (var i = 0; i < values.length; i++) out.push(values[i] + 'px');
            return out.join(' ');
        }
    },
    wireRunKey: function (dotNetRef) {
        if (!play.wireRunKey.handler) {
            play.wireRunKey.handler = function (e) {
                if (e.key !== 'Enter' || !(e.ctrlKey || e.metaKey)) return;
                e.preventDefault();
                if (play.wireRunKey.ref) play.wireRunKey.ref.invokeMethodAsync('OnRunKey');
            };
            document.addEventListener('keydown', play.wireRunKey.handler);
        }
        play.wireRunKey.ref = dotNetRef;
    },
    disposeRunKey: function () {
        if (!play.wireRunKey.handler) return;
        document.removeEventListener('keydown', play.wireRunKey.handler);
        play.wireRunKey.handler = null;
        play.wireRunKey.ref = null;
    }
};
