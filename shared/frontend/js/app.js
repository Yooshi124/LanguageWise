// Renders the service endpoint reference table on the shared home page.
// Kept deliberately small: this frontend container is static HTML, CSS and JS only.
(function () {
    "use strict";

    var SERVICES = [
        { name: "Home", owner: "Team", frontend: 3000, backend: 5000, database: 6000, available: true },
        { name: "Mini Games", owner: "Kyan", frontend: 3001, frontendPath: "/mini-games/", backend: 5001, database: 6005, available: true },
        { name: "Discussion Forum", owner: "Lachlan", frontend: 3002, backend: 5002, database: 6002, available: true },
        { name: "Quizzes & Courses", owner: "Justin", frontend: 3003, backend: 5003, database: 6003, available: true },
        { name: "Quests & Achievements", owner: "Amber", frontend: 3000, frontendPath: "/quests-and-achievements/", backend: 5004, database: 6004, available: true },
        { name: "Leaderboard & Analytics", owner: "Roan", frontend: 3005, backend: 5005, database: 6005, available: false }
    ];

    function link(port, path) {
        var anchor = document.createElement("a");
        anchor.href = "http://localhost:" + port + (path || "/");
        anchor.textContent = ":" + port;
        anchor.rel = "noopener";
        return anchor;
    }

    function endpoint(service, port, path) {
        if (service.available) {
            return link(port, path);
        }

        var unavailable = document.createElement("span");
        unavailable.textContent = "Not available";
        unavailable.setAttribute("aria-disabled", "true");
        return unavailable;
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
        var body = document.getElementById("lw-endpoints");
        if (!body) {
            return;
        }

        body.textContent = "";

        SERVICES.forEach(function (service) {
            var row = document.createElement("tr");
            cell(row, service.name);
            cell(row, service.owner);
            cell(row, endpoint(service, service.frontend, service.frontendPath));
            cell(row, endpoint(service, service.backend, "/health"));
            cell(row, endpoint(service, service.database, "/health"));
            body.appendChild(row);
        });
    });
})();
