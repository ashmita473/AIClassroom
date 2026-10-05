(() => {
    'use strict';

    const form = document.getElementById('lessonEditorForm');
    if (!form) return;

    const sync = () => {
        document.querySelectorAll('.rich-editor').forEach(editor => {
            const target = form.querySelector(`textarea[name="${editor.dataset.target}"]`);
            if (target) target.value = editor.innerHTML;
        });
    };

    // Word-style lesson editor
    document.querySelectorAll('.word-toolbar [data-command]').forEach(button => {
        button.addEventListener('click', () => {
            document.execCommand(button.dataset.command, false, null);
            sync();
        });
    });

    document.querySelectorAll('.word-toolbar select').forEach(select => {
        select.addEventListener('change', () => {
            document.execCommand(select.dataset.command, false, select.value);
            sync();
        });
    });

    document.getElementById('insertImage')?.addEventListener('click', () => {
        const url = window.prompt('Image URL (https://...)');
        if (url) {
            document.execCommand('insertImage', false, url);
            sync();
        }
    });

    document.getElementById('createLink')?.addEventListener('click', () => {
        const url = window.prompt('Link URL');
        if (url) {
            document.execCommand('createLink', false, url);
            sync();
        }
    });

    document.querySelectorAll('.rich-editor').forEach(editor => {
        editor.addEventListener('input', sync);
    });

    // Activities
    const activityList = document.getElementById('activityList');
    const addActivity = document.getElementById('addActivity');
    let activityIndex = activityList
        ? activityList.querySelectorAll('.activity-editor-row').length
        : 0;

    addActivity?.addEventListener('click', () => {
        if (!activityList) return;

        const i = activityIndex++;
        const wrap = document.createElement('div');
        wrap.className = 'activity-editor-row';
        wrap.innerHTML = `
            <div class="editor-grid-3">
                <label>Type<input name="Activities[${i}].ActivityType" value="Question" /></label>
                <label>XP<input name="Activities[${i}].XpReward" type="number" min="0" value="10" /></label>
                <label>Order<input name="Activities[${i}].SortOrder" type="number" value="${i + 1}" /></label>
            </div>
            <label>Activity title<input name="Activities[${i}].Title" placeholder="Mission challenge" /></label>
            <label>Activity content<textarea name="Activities[${i}].Content" rows="3" placeholder="Describe what students should do..."></textarea></label>
            <label class="delete-row"><input type="checkbox" name="Activities[${i}].Delete" value="true" /> Delete this activity</label>`;
        activityList.appendChild(wrap);
    });

    // YouTube video editor
    const videoList = document.getElementById('videoList');
    const addVideo = document.getElementById('addVideo');
    let videoIndex = videoList
        ? videoList.querySelectorAll('[data-video-row]').length
        : 0;

    const videoIdFromUrl = value => {
        try {
            const url = new URL(value);
            const host = url.hostname.toLowerCase().replace(/^www\./, '');

            if (host === 'youtu.be') {
                return url.pathname.split('/').filter(Boolean)[0] || '';
            }

            if (host === 'youtube.com' || host.endsWith('.youtube.com')) {
                const queryId = url.searchParams.get('v');
                if (queryId) return queryId;

                const parts = url.pathname.split('/').filter(Boolean);
                if (parts.length >= 2 && ['embed', 'shorts', 'live'].includes(parts[0].toLowerCase())) {
                    return parts[1];
                }
            }
        } catch (_) {
            // Invalid/incomplete URL while the teacher is typing.
        }
        return '';
    };

    const wireVideoRow = row => {
        const input = row.querySelector('[data-youtube-url]');
        const preview = row.querySelector('[data-video-preview]');
        if (!input || !preview) return;

        const updatePreview = () => {
            const id = videoIdFromUrl(input.value.trim());
            preview.innerHTML = id
                ? `<iframe src="https://www.youtube.com/embed/${encodeURIComponent(id)}?rel=0&playsinline=1" title="YouTube preview" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" allowfullscreen loading="lazy"></iframe>`
                : '<span>Paste a valid YouTube URL to preview it here.</span>';
        };

        input.addEventListener('input', updatePreview);
    };

    document.querySelectorAll('[data-video-row]').forEach(wireVideoRow);

    addVideo?.addEventListener('click', () => {
        if (!videoList) return;

        const i = videoIndex++;
        const row = document.createElement('div');
        row.className = 'video-editor-row';
        row.setAttribute('data-video-row', '');
        row.innerHTML = `
            <input type="hidden" name="Videos[${i}].Id" value="" />
            <div class="editor-grid-2">
                <label>Video title<input name="Videos[${i}].Title" placeholder="Introduction to AI" /></label>
                <label>Order<input name="Videos[${i}].SortOrder" type="number" min="1" value="${i + 1}" /></label>
            </div>
            <label>YouTube URL<input name="Videos[${i}].YouTubeUrl" data-youtube-url placeholder="https://www.youtube.com/watch?v=..." /></label>
            <label>Description<textarea name="Videos[${i}].Description" rows="2" placeholder="What will students learn from this video?"></textarea></label>
            <div class="video-editor-options">
                <label><input name="Videos[${i}].IsRequired" type="checkbox" value="true" /> Required to complete lesson</label>
                <label>XP<input name="Videos[${i}].XpReward" type="number" min="0" max="1000" value="10" /></label>
                <label><input name="Videos[${i}].IsPublished" type="checkbox" value="true" checked /> Published</label>
            </div>
            <div class="video-preview" data-video-preview><span>Paste a YouTube URL to preview it here.</span></div>
            <div class="video-row-actions">
                <button type="button" class="small-btn video-up">↑</button>
                <button type="button" class="small-btn video-down">↓</button>
                <label class="delete-row"><input type="checkbox" name="Videos[${i}].Delete" value="true" /> Delete</label>
            </div>`;

        videoList.appendChild(row);
        wireVideoRow(row);

        // Put focus in the new title field so the teacher can start typing immediately.
        row.querySelector(`input[name="Videos[${i}].Title"]`)?.focus();
    });

    videoList?.addEventListener('click', event => {
        const button = event.target.closest('.video-up, .video-down');
        if (!button) return;

        const row = button.closest('[data-video-row]');
        if (!row) return;

        if (button.classList.contains('video-up') && row.previousElementSibling) {
            videoList.insertBefore(row, row.previousElementSibling);
        }

        if (button.classList.contains('video-down') && row.nextElementSibling) {
            videoList.insertBefore(row.nextElementSibling, row);
        }

        [...videoList.querySelectorAll('[data-video-row]')].forEach((videoRow, position) => {
            const order = videoRow.querySelector('input[name$=".SortOrder"]');
            if (order) order.value = position + 1;
        });
    });

    form.addEventListener('submit', sync);
})();
