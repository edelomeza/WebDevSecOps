(function () {
    'use strict';

    var sidebar = document.getElementById('sidebar');
    var toggleBtn = document.getElementById('sidebarToggle');

    if (sidebar && toggleBtn) {
        toggleBtn.addEventListener('click', function () {
            if (window.innerWidth <= 767.98) {
                sidebar.classList.toggle('show');
                toggleOverlay(true);
            } else {
                sidebar.classList.toggle('collapsed');
            }
        });
    }

    function toggleOverlay(show) {
        var overlay = document.querySelector('.sidebar-overlay');
        if (show) {
            if (!overlay) {
                overlay = document.createElement('div');
                overlay.className = 'sidebar-overlay';
                overlay.addEventListener('click', function () {
                    sidebar.classList.remove('show');
                    overlay.classList.remove('show');
                });
                document.body.appendChild(overlay);
            }
            overlay.classList.add('show');
        } else {
            var el = document.querySelector('.sidebar-overlay');
            if (el) el.classList.remove('show');
        }
    }

    document.addEventListener('click', function (e) {
        var s = document.getElementById('sidebar');
        var t = document.getElementById('sidebarToggle');
        if (window.innerWidth <= 767.98 &&
            s.classList.contains('show') &&
            !s.contains(e.target) &&
            !t.contains(e.target)) {
            s.classList.remove('show');
            var o = document.querySelector('.sidebar-overlay');
            if (o) o.classList.remove('show');
        }
    });

    // Active link en sidebar basado en URL actual
    var currentPath = window.location.pathname.toLowerCase();
    document.querySelectorAll('.sidebar-nav a').forEach(function (link) {
        var href = link.getAttribute('href').toLowerCase();
        if (currentPath.startsWith(href) && href !== '/') {
            link.classList.add('active');
        } else if (href === '/' && currentPath === '/') {
            link.classList.add('active');
        }
    });

    // Skeleton loader: muestra skeleton 500ms, luego revela la tabla
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[id^="skeleton-"]').forEach(function (skel) {
            var tableId = skel.id.replace('skeleton-', 'table-');
            var table = document.getElementById(tableId);
            setTimeout(function () {
                skel.classList.add('d-none');
                if (table) table.classList.remove('d-none');
            }, 500);
        });
    });
})();
