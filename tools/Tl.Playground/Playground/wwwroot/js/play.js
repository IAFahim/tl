window.play = {
    syncQuery: function (query) {
        try { history.replaceState(null, '', location.pathname + '?' + query + location.hash); } catch (e) { }
    },
    measureHeight: function () {
        var height = Math.max(document.documentElement.scrollHeight, document.body.scrollHeight);
        try { window.parent.postMessage({ type: 'setHeight', height: height }, '*'); } catch (e) { }
        return height;
    }
};
