window.initPopover = function (el, title, content) {
    if (!el) return;
    var existing = bootstrap.Popover.getInstance(el);
    if (existing) existing.dispose();
    new bootstrap.Popover(el, {
        title: title,
        content: content,
        trigger: 'focus',
        placement: 'right',
        html: false
    });
};
