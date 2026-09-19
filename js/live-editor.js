(function () {
    'use strict';

    const editors = new Map();

    function esc(s) {
        return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function render(target, lang, value) {
        let html;
        try {
            if (window.hljs) {
                if (lang && hljs.getLanguage(lang)) {
                    html = hljs.highlight(value, { language: lang, ignoreIllegals: true }).value;
                } else {
                    html = hljs.highlightAuto(value).value;
                }
            } else {
                html = esc(value);
            }
        } catch (_) {
            html = esc(value);
        }
        target.innerHTML = html + '\n';
    }

    function gutter(target, value) {
        const lines = value.split('\n').length;
        let numbers = '';
        for (let i = 1; i <= lines; i++) numbers += i + '\n';
        target.innerText = numbers;
    }

    function sync(editor) {
        const value = editor.input.value;
        render(editor.highlight, editor.lang, value);
        gutter(editor.gutter, value);
        if (editor.fill) return;
        editor.input.style.height = 'auto';
        editor.input.style.height = editor.input.scrollHeight + 'px';
        editor.highlight.style.height = editor.input.style.height;
        editor.gutter.style.height = editor.input.style.height;
    }

    function wireHidden(editor, hiddenId) {
        const hidden = hiddenId ? document.getElementById(hiddenId) : null;
        editor.input.addEventListener('input', () => {
            sync(editor);
            if (hidden) {
                hidden.value = editor.input.value;
                hidden.dispatchEvent(new Event('input', { bubbles: true }));
            }
        });
    }

    window.tlLiveEditor = {
        init(rootId, lang, initial, hiddenId) {
            const root = document.getElementById(rootId);
            if (!root) return;
            const fill = root.classList.contains('editor-fill');
            root.classList.add('editor');
            root.innerHTML =
                '<div class="editor-gutter"></div>' +
                '<div class="editor-scroll">' +
                '<pre class="editor-highlight" aria-hidden="true"><code></code></pre>' +
                '<textarea class="editor-input" spellcheck="false" wrap="off"></textarea>' +
                '</div>';
            const input = root.querySelector('.editor-input');
            const highlight = root.querySelector('.editor-highlight');
            const gutter = root.querySelector('.editor-gutter');
            input.value = initial;
            const editor = { root, input, highlight, gutter, lang, fill };
            editors.set(rootId, editor);
            wireHidden(editor, hiddenId);
            input.addEventListener('scroll', () => {
                highlight.scrollTop = input.scrollTop;
                highlight.scrollLeft = input.scrollLeft;
                gutter.scrollTop = input.scrollTop;
            });
            input.addEventListener('keydown', (e) => {
                if (e.key === 'Tab') {
                    e.preventDefault();
                    const start = input.selectionStart, end = input.selectionEnd;
                    input.value = input.value.slice(0, start) + '    ' + input.value.slice(end);
                    input.selectionStart = input.selectionEnd = start + 4;
                    sync(editor);
                    if (hiddenId) {
                        const hidden = document.getElementById(hiddenId);
                        if (hidden) {
                            hidden.value = input.value;
                            hidden.dispatchEvent(new Event('input', { bubbles: true }));
                        }
                    }
                }
            });
            sync(editor);
        },
        initReadonly(rootId, lang, initial) {
            const root = document.getElementById(rootId);
            if (!root) return;
            const fill = root.classList.contains('editor-fill');
            root.classList.add('editor', 'editor-readonly');
            root.innerHTML =
                '<div class="editor-gutter"></div>' +
                '<div class="editor-scroll">' +
                '<pre class="editor-highlight editor-static" aria-hidden="true"><code></code></pre>' +
                '</div>';
            const highlight = root.querySelector('.editor-highlight');
            const gutter = root.querySelector('.editor-gutter');
            const editor = { root, input: { value: initial, scrollTop: 0, style: {} }, highlight, gutter, lang, fill };
            editors.set(rootId, editor);
            render(highlight, lang, initial);
            let numbers = '';
            const lines = initial.split('\n').length;
            for (let i = 1; i <= lines; i++) numbers += i + '\n';
            gutter.innerText = numbers;
            highlight.addEventListener('scroll', () => {
                gutter.scrollTop = highlight.scrollTop;
            });
        },
        setReadonlyValue(rootId, value) {
            const editor = editors.get(rootId);
            if (!editor) return;
            editor.input.value = value;
            render(editor.highlight, editor.lang, value);
            let numbers = '';
            const lines = value.split('\n').length;
            for (let i = 1; i <= lines; i++) numbers += i + '\n';
            editor.gutter.innerText = numbers;
            editor.root.scrollTop = 0;
            editor.highlight.scrollTop = 0;
        }
    };
})();
