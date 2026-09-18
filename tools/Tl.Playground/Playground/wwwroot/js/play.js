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
            var before = toNumber(el.getAttribute('data-before'), 0);
            var after = toNumber(el.getAttribute('data-after'), 2);
            var minBefore = toFloat(el.getAttribute('data-min-before'), 200);
            var minAfter = toFloat(el.getAttribute('data-min-after'), 200);
            var start = 0;

            el.addEventListener('pointerdown', function (e) {
                var sizes = read();
                if (!sizes) return;
                start = vertical ? e.clientY : e.clientX;
                el.setPointerCapture(e.pointerId);
                el.classList.add('dragging');
                document.body.classList.add(vertical ? 'splitting-rows' : 'splitting-cols');
                e.preventDefault();
            });
            el.addEventListener('pointermove', function (e) {
                if (!el.classList.contains('dragging')) return;
                apply(read(), (vertical ? e.clientY : e.clientX) - start);
            });
            el.addEventListener('pointerup', end);
            el.addEventListener('pointercancel', end);
            el.addEventListener('keydown', function (e) {
                var delta = e.key === 'ArrowLeft' || e.key === 'ArrowUp' ? -24
                    : e.key === 'ArrowRight' || e.key === 'ArrowDown' ? 24 : 0;
                if (!delta) return;
                apply(read(), delta);
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
    }
};
