(function () {
    "use strict";

    var dashboard = document.getElementById("dashboard");
    var signedOut = document.getElementById("signed-out");
    var serviceError = document.getElementById("service-error");
    var notificationTypes = document.getElementById("notification-types");
    var saveStatus = document.getElementById("save-status");

    document.body.addEventListener("htmx:afterRequest", function (event) {
        var path = event.detail.pathInfo.requestPath;
        if (path === "/api/profile") {
            handleProfile(event.detail.xhr);
        } else if (path === "/api/preferences") {
            handlePreferences(event.detail.xhr);
        }
    });

    document.getElementById("notify-all").addEventListener("change", function (event) {
        notificationTypes.disabled = !event.target.checked;
    });

    function handleProfile(xhr) {
        if (xhr.status === 401) {
            signedOut.hidden = false;
            return;
        }

        if (xhr.status !== 200) {
            serviceError.hidden = false;
            return;
        }

        renderProfile(JSON.parse(xhr.responseText));
        dashboard.hidden = false;
    }

    function renderProfile(profile) {
        var preferences = profile.preferences;
        var completed = profile.achievements.filter(function (achievement) {
            return achievement.progress >= achievement.progressNeeded;
        }).length;

        document.getElementById("profile-name").textContent = profile.username;
        document.getElementById("achievement-summary").textContent = completed + " of " + profile.achievements.length + " achievements complete";
        document.getElementById("completed-count").textContent = completed + " complete";
        document.getElementById("email").value = preferences.email || "";
        document.getElementById("notify-all").checked = preferences.notifyAll;
        setChecked("notifyPostEngagement", preferences.notifyPostEngagement);
        setChecked("notifyCourseCompletion", preferences.notifyCourseCompletion);
        setChecked("notifyQuizResults", preferences.notifyQuizResults);
        setChecked("notifyStreaks", preferences.notifyStreaks);
        setChecked("notifyAchievements", preferences.notifyAchievements);
        notificationTypes.disabled = !preferences.notifyAll;

        var grid = document.getElementById("achievement-grid");
        grid.replaceChildren();
        profile.achievements.forEach(function (achievement) {
            grid.appendChild(createAchievement(achievement));
        });
    }

    function createAchievement(achievement) {
        var fragment = document.getElementById("achievement-template").content.cloneNode(true);
        var article = fragment.querySelector("article");
        var image = fragment.querySelector("img");
        var complete = achievement.progress >= achievement.progressNeeded;

        article.dataset.complete = complete;
        image.src = achievement.image;
        image.addEventListener("error", function () {
            image.src = "/images/achievement.svg";
        }, { once: true });
        fragment.querySelector(".lw-achievement__fallback").textContent = achievement.name.charAt(0);
        fragment.querySelector("h3").textContent = achievement.name;
        fragment.querySelector(".lw-achievement__state").textContent = complete ? "Earned" : "In progress";

        var progress = fragment.querySelector("progress");
        progress.value = achievement.progress;
        progress.max = achievement.progressNeeded;
        progress.setAttribute("aria-label", achievement.name + " progress");
        fragment.querySelector(".lw-achievement__progress").textContent = achievement.progress + " / " + achievement.progressNeeded;
        return fragment;
    }

    function handlePreferences(xhr) {
        saveStatus.dataset.error = xhr.status !== 200;
        if (xhr.status === 200) {
            saveStatus.textContent = JSON.parse(xhr.responseText).message;
        } else if (xhr.status === 400) {
            saveStatus.textContent = "Enter a valid email address.";
        } else {
            saveStatus.textContent = "Preferences could not be saved.";
        }
    }

    function setChecked(name, value) {
        document.querySelector('[name="' + name + '"]').checked = value;
    }
}());