(() => {
    const page = document.querySelector('[data-game-page]');
    const form = document.getElementById('gameForm');
    if (!page || !form) return;
    const questions = [...document.querySelectorAll('.engine-question')];
    const progressText = document.getElementById('gameProgress');
    const progressBar = document.getElementById('gameProgressBar');
    let current = 0;

    function syncAnswer(index) {
        const selected = document.querySelector(`input[name="Answers[${index}]"]:checked`);
        const target = document.getElementById(`answer_${index}`);
        if (target) target.value = selected ? selected.value : '';
        return !!selected;
    }

    function show(index) {
        current = index;
        questions.forEach((q, i) => q.hidden = i !== index);
        progressText.textContent = `Question ${index + 1} of ${questions.length}`;
        progressBar.style.width = `${((index + 1) / questions.length) * 100}%`;
    }

    document.querySelectorAll('[data-next-question]').forEach(button => {
        button.addEventListener('click', () => {
            const index = Number(button.dataset.nextQuestion);
            if (!syncAnswer(index)) {
                button.classList.add('shake');
                setTimeout(() => button.classList.remove('shake'), 350);
                return;
            }
            if (index === questions.length - 1) {
                form.submit();
                return;
            }
            show(index + 1);
        });
    });
    show(0);
})();
