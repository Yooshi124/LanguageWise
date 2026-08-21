// Renders the service endpoint reference table on the shared home page.
// Kept deliberately small: the frontend container is static HTML, CSS and JS only.
(function () {
    "use strict";

    var SERVICES = [
        { name: "Shared (home)", frontend: 3000, backend: 5000, database: 6000 },
        { name: "Student 1 — Mini Games", frontend: 3001, backend: 5001, database: 6001 },
        { name: "Student 2 — Discussion Forum", frontend: 3002, backend: 5002, database: 6002 },
        { name: "Student 3 — Quizzes & Courses", frontend: 3003, backend: 5003, database: 6003 },
        { name: "Student 4 — Quests & Achievements", frontend: 3004, backend: 5004, database: 6004 },
        { name: "Student 5 — Leaderboard", frontend: 3005, backend: 5005, database: 6005 }
    ];

    function link(port, path) {
        var url = "http://localhost:" + port + (path || "/");
        var anchor = document.createElement("a");
        anchor.href = url;
        anchor.textContent = ":" + port;
        anchor.rel = "noopener";
        return anchor;
    }

    function cell(row, child) {
        var td = document.createElement("td");
        if (typeof child === "string") {
            td.textContent = child;
        } else {
            td.appendChild(child);
        }
        row.appendChild(td);
    }

    document.addEventListener("DOMContentLoaded", function () {
        var body = document.querySelector("#lw-endpoints tbody");
        if (!body) {
            return;
        }

        SERVICES.forEach(function (service) {
            var row = document.createElement("tr");
            cell(row, service.name);
            cell(row, link(service.frontend));
            cell(row, link(service.backend, "/health"));
            cell(row, link(service.database, "/health"));
            body.appendChild(row);
        });
    });
})();
